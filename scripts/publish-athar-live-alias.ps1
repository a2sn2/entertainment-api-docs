#requires -Version 5.1

[CmdletBinding()]
param(
    [string]$TunnelUrl,
    [switch]$Offline
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$RepositoryRoot = Split-Path -Parent $PSScriptRoot
$BranchName = "athar-live-link"
$RemoteName = "origin"
$TargetRelativePath = "site/athar-live/target.json"
$WorktreePath = Join-Path $RepositoryRoot ".local/athar-live-link-worktree"
$StableUrl = "https://a2sn2.github.io/foundationkit-dotnet/athar-live/"
$AllowedTunnelPattern = '^https://[a-z0-9-]+\.trycloudflare\.com/?$'

function Invoke-Git {
    param(
        [Parameter(Mandatory)][string[]]$Arguments,
        [string]$WorkingDirectory = $RepositoryRoot,
        [switch]$IgnoreExitCode
    )

    & git -C $WorkingDirectory @Arguments
    $exitCode = $LASTEXITCODE
    if (-not $IgnoreExitCode -and $exitCode -ne 0) {
        throw "Git command failed with exit code $exitCode: git $($Arguments -join ' ')"
    }

    return $exitCode
}

if ($Offline) {
    $status = "offline"
    $url = ""
    $message = "Athar is currently offline. Start the local product and run the expose command."
}
else {
    if ([string]::IsNullOrWhiteSpace($TunnelUrl) -or $TunnelUrl -notmatch $AllowedTunnelPattern) {
        throw "TunnelUrl must be a valid https://*.trycloudflare.com URL."
    }

    $status = "online"
    $url = $TunnelUrl.TrimEnd('/')
    $message = "Athar is online through the current temporary Cloudflare tunnel."
}

if (-not (Get-Command git -ErrorAction SilentlyContinue)) {
    throw "Git is required to publish the fixed free alias."
}

New-Item -ItemType Directory -Force -Path (Split-Path -Parent $WorktreePath) | Out-Null

try {
    Invoke-Git -Arguments @("fetch", $RemoteName, $BranchName) | Out-Null

    Invoke-Git -Arguments @("worktree", "remove", "--force", $WorktreePath) -IgnoreExitCode | Out-Null
    if (Test-Path $WorktreePath) {
        Remove-Item $WorktreePath -Recurse -Force
    }

    Invoke-Git -Arguments @("worktree", "add", "--detach", $WorktreePath, "$RemoteName/$BranchName") | Out-Null

    $targetPath = Join-Path $WorktreePath $TargetRelativePath
    New-Item -ItemType Directory -Force -Path (Split-Path -Parent $targetPath) | Out-Null

    $payload = [ordered]@{
        schemaVersion = 1
        status = $status
        url = $url
        updatedAt = [DateTime]::UtcNow.ToString("o")
        message = $message
    }

    $json = $payload | ConvertTo-Json -Depth 4
    [System.IO.File]::WriteAllText(
        $targetPath,
        $json + [Environment]::NewLine,
        [System.Text.UTF8Encoding]::new($false))

    $userName = (& git -C $WorktreePath config user.name 2>$null)
    if ([string]::IsNullOrWhiteSpace(($userName -join ""))) {
        Invoke-Git -WorkingDirectory $WorktreePath -Arguments @("config", "user.name", "Athar Live Link") | Out-Null
    }

    $userEmail = (& git -C $WorktreePath config user.email 2>$null)
    if ([string]::IsNullOrWhiteSpace(($userEmail -join ""))) {
        Invoke-Git -WorkingDirectory $WorktreePath -Arguments @("config", "user.email", "athar-live@users.noreply.github.com") | Out-Null
    }

    Invoke-Git -WorkingDirectory $WorktreePath -Arguments @("add", $TargetRelativePath) | Out-Null
    $diffExit = Invoke-Git -WorkingDirectory $WorktreePath -Arguments @("diff", "--cached", "--quiet") -IgnoreExitCode

    if ($diffExit -eq 0) {
        Write-Host "The fixed alias already has the requested state." -ForegroundColor Yellow
        Write-Host "Stable link: $StableUrl" -ForegroundColor Green
        exit 0
    }

    $commitMessage = if ($Offline) { "Mark Athar live alias offline" } else { "Point Athar live alias to current tunnel" }
    Invoke-Git -WorkingDirectory $WorktreePath -Arguments @("commit", "-m", $commitMessage) | Out-Null
    Invoke-Git -WorkingDirectory $WorktreePath -Arguments @("push", $RemoteName, "HEAD:refs/heads/$BranchName") | Out-Null

    Write-Host "Fixed free alias updated." -ForegroundColor Green
    Write-Host "Stable link: $StableUrl" -ForegroundColor Green
}
finally {
    Invoke-Git -Arguments @("worktree", "remove", "--force", $WorktreePath) -IgnoreExitCode | Out-Null
    if (Test-Path $WorktreePath) {
        Remove-Item $WorktreePath -Recurse -Force -ErrorAction SilentlyContinue
    }
}
