#requires -Version 5.1

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$BaseUrl = "http://localhost:8090"

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

& cloudflared tunnel --url $BaseUrl

if ($LASTEXITCODE -ne 0) {
    throw "cloudflared exited with code $LASTEXITCODE."
}
