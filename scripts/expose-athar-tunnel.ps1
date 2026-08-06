#requires -Version 5.1

param(
    [ValidateRange(1, 10)]
    [int]$MaxAttempts = 3
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$RepositoryRoot = Split-Path -Parent $PSScriptRoot
$BaseUrl = "http://localhost:8090"
$QuickTunnelHost = "api.trycloudflare.com"
$AliasPublisher = Join-Path $PSScriptRoot "publish-athar-live-alias.ps1"
$StableAlias = "https://a2sn2.github.io/foundationkit-dotnet/athar-live/"
$TunnelLogDirectory = Join-Path $RepositoryRoot ".local/logs"
$AllowedTunnelPattern = 'https://[a-z0-9-]+\.trycloudflare\.com/?'

function Test-QuickTunnelNetwork {
    Write-Host "Checking DNS and outbound HTTPS access..." -ForegroundColor Cyan

    $dnsOk = $false
    try {
        [void][System.Net.Dns]::GetHostAddresses($QuickTunnelHost)
        $dnsOk = $true
        Write-Host "  DNS: OK ($QuickTunnelHost)" -ForegroundColor Green
    }
    catch {
        Write-Host "  DNS: FAILED ($QuickTunnelHost)" -ForegroundColor Red
    }

    $tcpOk = $false
    try {
        $tcpOk = Test-NetConnection -ComputerName $QuickTunnelHost -Port 443 -InformationLevel Quiet -WarningAction SilentlyContinue
    }
    catch {
        $tcpOk = $false
    }

    if ($tcpOk) {
        Write-Host "  TCP 443: OK" -ForegroundColor Green
    }
    else {
        Write-Host "  TCP 443: FAILED" -ForegroundColor Red
    }

    return ($dnsOk -and $tcpOk)
}

function Show-NewLogLines {
    param(
        [Parameter(Mandatory)][string]$Path,
        [Parameter(Mandatory)][ref]$SeenCount,
        [Parameter(Mandatory)][ref]$DetectedUrl
    )

    if (-not (Test-Path $Path)) {
        return
    }

    $lines = @(Get-Content $Path -ErrorAction SilentlyContinue)
    if ($lines.Count -le $SeenCount.Value) {
        return
    }

    for ($index = $SeenCount.Value; $index -lt $lines.Count; $index++) {
        $line = [string]$lines[$index]
        Write-Host $line

        if ([string]::IsNullOrWhiteSpace($DetectedUrl.Value)) {
            $match = [regex]::Match($line, $AllowedTunnelPattern, [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
            if ($match.Success) {
                $DetectedUrl.Value = $match.Value.TrimEnd('/')
            }
        }
    }

    $SeenCount.Value = $lines.Count
}

function Publish-FixedAlias {
    param([Parameter(Mandatory)][string]$TunnelUrl)

    if (-not (Test-Path $AliasPublisher)) {
        Write-Host "Fixed alias publisher is missing. Share the temporary URL shown above." -ForegroundColor Yellow
        return $false
    }

    try {
        & powershell.exe -NoProfile -ExecutionPolicy Bypass -File $AliasPublisher -TunnelUrl $TunnelUrl
        if ($LASTEXITCODE -ne 0) {
            Write-Host "The tunnel works, but the fixed alias could not be updated." -ForegroundColor Yellow
            Write-Host "Share this temporary URL: $TunnelUrl" -ForegroundColor Yellow
            return $false
        }

        Write-Host ""
        Write-Host "Permanent free link:" -ForegroundColor Green
        Write-Host "  $StableAlias" -ForegroundColor White
        Write-Host "Share the permanent link instead of the random tunnel URL." -ForegroundColor Green
        return $true
    }
    catch {
        Write-Host "The tunnel works, but the fixed alias update failed: $($_.Exception.Message)" -ForegroundColor Yellow
        Write-Host "Share this temporary URL: $TunnelUrl" -ForegroundColor Yellow
        return $false
    }
}

function Mark-FixedAliasOffline {
    if (-not (Test-Path $AliasPublisher)) {
        return
    }

    try {
        & powershell.exe -NoProfile -ExecutionPolicy Bypass -File $AliasPublisher -Offline
        if ($LASTEXITCODE -ne 0) {
            Write-Host "Could not mark the fixed alias offline. It may still point to the expired tunnel for a while." -ForegroundColor Yellow
        }
    }
    catch {
        Write-Host "Could not mark the fixed alias offline: $($_.Exception.Message)" -ForegroundColor Yellow
    }
}

try {
    Invoke-RestMethod -Uri "$BaseUrl/health/ready" -TimeoutSec 3 | Out-Null
}
catch {
    throw "Athar is not ready at $BaseUrl. Start it first with START-ATHAR.cmd."
}

$cloudflared = Get-Command "cloudflared" -ErrorAction SilentlyContinue
if (-not $cloudflared) {
    Write-Host "cloudflared is not installed or is not available in PATH." -ForegroundColor Yellow
    Write-Host "Install it with:" -ForegroundColor Cyan
    Write-Host "  winget install --id Cloudflare.cloudflared --exact --source winget" -ForegroundColor White
    Write-Host "Then close and reopen PowerShell and run EXPOSE-ATHAR.cmd." -ForegroundColor Yellow
    exit 1
}

Write-Host "Creating a temporary HTTPS tunnel to $BaseUrl" -ForegroundColor Cyan
Write-Host "Keep this window open while other people use the link." -ForegroundColor Yellow
Write-Host "Press Ctrl+C to stop the public link." -ForegroundColor Yellow
Write-Host "Do not use real or sensitive data in this experimental public demo." -ForegroundColor Red
Write-Host ""

$networkReady = Test-QuickTunnelNetwork
if (-not $networkReady) {
    Write-Host ""
    Write-Host "Cloudflare Quick Tunnel is not reachable from this network." -ForegroundColor Yellow
    Write-Host "Try a mobile hotspot, another network, or an allowed VPN, then run EXPOSE-ATHAR.cmd again." -ForegroundColor Yellow
    exit 2
}

New-Item -ItemType Directory -Force -Path $TunnelLogDirectory | Out-Null

$cloudflaredArgs = @(
    "tunnel",
    "--url", $BaseUrl,
    "--protocol", "http2",
    "--edge-ip-version", "4",
    "--no-autoupdate"
)

for ($attempt = 1; $attempt -le $MaxAttempts; $attempt++) {
    Write-Host ""
    Write-Host "Quick Tunnel attempt $attempt of $MaxAttempts..." -ForegroundColor Cyan

    $stdoutLog = Join-Path $TunnelLogDirectory "cloudflared-attempt-$attempt.out.log"
    $stderrLog = Join-Path $TunnelLogDirectory "cloudflared-attempt-$attempt.err.log"
    Remove-Item $stdoutLog, $stderrLog -Force -ErrorAction SilentlyContinue

    $process = $null
    $detectedUrl = ""
    $aliasPublished = $false
    $seenOut = 0
    $seenErr = 0

    try {
        $process = Start-Process `
            -FilePath $cloudflared.Source `
            -ArgumentList $cloudflaredArgs `
            -PassThru `
            -NoNewWindow `
            -RedirectStandardOutput $stdoutLog `
            -RedirectStandardError $stderrLog

        while (-not $process.HasExited) {
            Show-NewLogLines -Path $stdoutLog -SeenCount ([ref]$seenOut) -DetectedUrl ([ref]$detectedUrl)
            Show-NewLogLines -Path $stderrLog -SeenCount ([ref]$seenErr) -DetectedUrl ([ref]$detectedUrl)

            if (-not $aliasPublished -and -not [string]::IsNullOrWhiteSpace($detectedUrl)) {
                $aliasPublished = Publish-FixedAlias -TunnelUrl $detectedUrl
                if (-not $aliasPublished) {
                    $aliasPublished = $true
                }
            }

            Start-Sleep -Milliseconds 700
            $process.Refresh()
        }

        $process.WaitForExit()
        Show-NewLogLines -Path $stdoutLog -SeenCount ([ref]$seenOut) -DetectedUrl ([ref]$detectedUrl)
        Show-NewLogLines -Path $stderrLog -SeenCount ([ref]$seenErr) -DetectedUrl ([ref]$detectedUrl)

        if (-not $aliasPublished -and -not [string]::IsNullOrWhiteSpace($detectedUrl)) {
            $aliasPublished = Publish-FixedAlias -TunnelUrl $detectedUrl
            if (-not $aliasPublished) {
                $aliasPublished = $true
            }
        }

        $exitCode = $process.ExitCode
    }
    finally {
        if ($null -ne $process -and -not $process.HasExited) {
            Stop-Process -Id $process.Id -Force -ErrorAction SilentlyContinue
        }

        if (-not [string]::IsNullOrWhiteSpace($detectedUrl)) {
            Mark-FixedAliasOffline
        }
    }

    if ($exitCode -eq 0) {
        exit 0
    }

    if ($attempt -lt $MaxAttempts) {
        $delaySeconds = 10 * $attempt
        Write-Host "cloudflared exited with code $exitCode. Retrying in $delaySeconds seconds..." -ForegroundColor Yellow
        Start-Sleep -Seconds $delaySeconds
    }
}

Write-Host ""
Write-Host "Cloudflare did not create a Quick Tunnel after $MaxAttempts attempts." -ForegroundColor Red
Write-Host "The local Athar product is still running at $BaseUrl." -ForegroundColor Yellow
Write-Host "Try again later or switch to a mobile hotspot/another network." -ForegroundColor Yellow
exit 3
