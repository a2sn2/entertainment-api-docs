$ErrorActionPreference = "Stop"

if (-not $env:ATHAR_SQL_PASSWORD) {
    $env:ATHAR_SQL_PASSWORD = "AtharSql!" + [Guid]::NewGuid().ToString("N") + "Aa1"
}

if (-not $env:ATHAR_ADMIN_EMAIL) {
    $env:ATHAR_ADMIN_EMAIL = "admin@athar.local"
}

if (-not $env:ATHAR_ADMIN_PASSWORD) {
    $env:ATHAR_ADMIN_PASSWORD = "AtharAdmin!" + [Guid]::NewGuid().ToString("N") + "Aa1"
}

docker compose -f deploy/athar-compose.yml up --build -d

Write-Host ""
Write-Host "Athar is starting at http://localhost:8090" -ForegroundColor Green
Write-Host "Admin email: $env:ATHAR_ADMIN_EMAIL" -ForegroundColor Cyan
Write-Host "Admin password: $env:ATHAR_ADMIN_PASSWORD" -ForegroundColor Cyan
Write-Host "These credentials are temporary for the current local environment." -ForegroundColor Yellow

Start-Sleep -Seconds 3
Start-Process "http://localhost:8090"
