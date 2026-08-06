#requires -Version 5.1

param(
    [ValidateRange(1, 10)]
    [int]$MaxAttempts = 3
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$BaseUrl = "http://localhost:8090"
$QuickTunnelHost = "api.trycloudflare.com"

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

    & $cloudflared.Source @cloudflaredArgs
    $exitCode = $LASTEXITCODE

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
