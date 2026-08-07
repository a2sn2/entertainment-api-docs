$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

if (-not $env:ATHAR_SQL_PASSWORD) {
    $env:ATHAR_SQL_PASSWORD = "AtharSql!" + [Guid]::NewGuid().ToString("N") + "Aa1"
}

if (-not $env:ATHAR_ADMIN_EMAIL) {
    $env:ATHAR_ADMIN_EMAIL = "admin@athar.local"
}

if (-not $env:ATHAR_ADMIN_PASSWORD) {
    $env:ATHAR_ADMIN_PASSWORD = "AtharAdmin!" + [Guid]::NewGuid().ToString("N") + "Aa1"
}

$localDirectory = Join-Path (Get-Location) ".local"
$credentialFile = Join-Path $localDirectory "athar-bootstrap-admin.env"
New-Item -ItemType Directory -Force -Path $localDirectory | Out-Null
[System.IO.File]::WriteAllLines(
    $credentialFile,
    @(
        "ATHAR_ADMIN_EMAIL=$env:ATHAR_ADMIN_EMAIL"
        "ATHAR_ADMIN_PASSWORD=$env:ATHAR_ADMIN_PASSWORD"
    ),
    [System.Text.UTF8Encoding]::new($false))

$identity = [System.Security.Principal.WindowsIdentity]::GetCurrent()
$sid = $identity.User.Value
& icacls $credentialFile /inheritance:r /grant:r "*$sid`:(F)" *> $null
if ($LASTEXITCODE -ne 0) {
    Remove-Item $credentialFile -Force -ErrorAction SilentlyContinue
    throw "Could not restrict ACLs on the local bootstrap credential file."
}

docker compose -f deploy/athar-compose.yml up --build -d
if ($LASTEXITCODE -ne 0) {
    throw "Docker Compose failed."
}

Write-Host ""
Write-Host "Athar is starting at http://localhost:8090" -ForegroundColor Green
Write-Host "Admin email: $env:ATHAR_ADMIN_EMAIL" -ForegroundColor Cyan
Write-Host "Bootstrap credentials are stored in .local/athar-bootstrap-admin.env with an ACL restricted to the current account." -ForegroundColor Yellow
Write-Host "Do not share, commit, or use these development credentials in production." -ForegroundColor Yellow

Start-Sleep -Seconds 3
Start-Process "http://localhost:8090"
