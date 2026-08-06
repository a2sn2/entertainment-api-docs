#requires -Version 5.1

[CmdletBinding()]
param(
    [ValidateSet("Start", "Stop", "Status", "Open", "Lan", "Backup", "Reset")]
    [string]$Action = "Start",

    [switch]$Force
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$RepositoryRoot = Split-Path -Parent $PSScriptRoot
$ComposeFile = Join-Path $RepositoryRoot "deploy/athar-compose.yml"
$LocalDirectory = Join-Path $RepositoryRoot ".local"
$EnvironmentFile = Join-Path $LocalDirectory "athar-product.env"
$BackupDirectory = Join-Path $LocalDirectory "backups"
$ProjectName = "athar-product"
$BaseUrl = "http://localhost:8090"

function Assert-Command {
    param([Parameter(Mandatory)][string]$Name)

    if (-not (Get-Command $Name -ErrorAction SilentlyContinue)) {
        throw "Required command '$Name' is not installed or is not available in PATH."
    }
}

function Assert-Docker {
    Assert-Command "docker"

    docker info *> $null
    if ($LASTEXITCODE -ne 0) {
        throw "Docker Desktop is not ready. Start Docker Desktop, wait until it is running, and try again."
    }

    docker compose version *> $null
    if ($LASTEXITCODE -ne 0) {
        throw "Docker Compose is not available through Docker Desktop."
    }
}

function New-StrongPassword {
    param([string]$Prefix)

    return $Prefix + [Guid]::NewGuid().ToString("N") + "Aa1!"
}

function Initialize-EnvironmentFile {
    New-Item -ItemType Directory -Force -Path $LocalDirectory | Out-Null

    if (Test-Path $EnvironmentFile) {
        return
    }

    $sqlPassword = New-StrongPassword "AtharSql!"
    $adminPassword = New-StrongPassword "AtharAdmin!"
    $lines = @(
        "ATHAR_SQL_PASSWORD=$sqlPassword"
        "ATHAR_ADMIN_EMAIL=admin@athar.local"
        "ATHAR_ADMIN_PASSWORD=$adminPassword"
    )

    [System.IO.File]::WriteAllLines(
        $EnvironmentFile,
        $lines,
        [System.Text.UTF8Encoding]::new($false))

    Write-Host "Created local settings at .local/athar-product.env" -ForegroundColor Green
    Write-Host "This file is ignored by Git and will not be committed." -ForegroundColor DarkYellow
}

function Get-EnvironmentValues {
    Initialize-EnvironmentFile
    $values = @{}

    foreach ($line in Get-Content $EnvironmentFile) {
        if ([string]::IsNullOrWhiteSpace($line) -or $line.TrimStart().StartsWith("#")) {
            continue
        }

        $parts = $line -split "=", 2
        if ($parts.Count -eq 2) {
            $values[$parts[0].Trim()] = $parts[1]
        }
    }

    return $values
}

function Invoke-Compose {
    param(
        [Parameter(Mandatory)]
        [string[]]$ComposeArguments
    )

    & docker compose `
        --project-name $ProjectName `
        --env-file $EnvironmentFile `
        -f $ComposeFile `
        @ComposeArguments

    if ($LASTEXITCODE -ne 0) {
        throw "Docker Compose failed. Review the messages above."
    }
}

function Wait-UntilReady {
    param([int]$Attempts = 120)

    Write-Host "Waiting for the API and SQL Server..." -ForegroundColor Cyan

    for ($attempt = 1; $attempt -le $Attempts; $attempt++) {
        try {
            $response = Invoke-RestMethod -Uri "$BaseUrl/health/ready" -TimeoutSec 3
            if ($null -ne $response) {
                Write-Host "Athar is ready." -ForegroundColor Green
                return
            }
        }
        catch {
            Start-Sleep -Seconds 2
        }
    }

    Invoke-Compose -ComposeArguments @("logs", "--tail", "150", "athar-api")
    throw "Athar did not become ready before the timeout."
}

function Show-AccessInformation {
    $values = Get-EnvironmentValues

    Write-Host ""
    Write-Host "Athar experimental product" -ForegroundColor Green
    Write-Host "  Home:          $BaseUrl"
    Write-Host "  Account:       $BaseUrl/account"
    Write-Host "  Initiatives:   $BaseUrl/initiatives"
    Write-Host "  Admin:         $BaseUrl/admin"
    Write-Host "  Swagger:       $BaseUrl/swagger"
    Write-Host "  Readiness:     $BaseUrl/health/ready"
    Write-Host ""
    Write-Host "Local administrator account" -ForegroundColor Cyan
    Write-Host "  Email:         $($values['ATHAR_ADMIN_EMAIL'])"
    Write-Host "  Password:      $($values['ATHAR_ADMIN_PASSWORD'])"
    Write-Host ""
}

function Show-LanUrls {
    $addresses = Get-NetIPAddress -AddressFamily IPv4 -ErrorAction SilentlyContinue |
        Where-Object {
            $_.IPAddress -notlike "127.*" -and
            $_.IPAddress -notlike "169.254.*" -and
            $_.AddressState -eq "Preferred"
        } |
        Select-Object -ExpandProperty IPAddress -Unique

    if (-not $addresses) {
        Write-Host "No suitable IPv4 address was found. Run ipconfig and check the active adapter." -ForegroundColor Yellow
        return
    }

    Write-Host "Possible URLs for devices on the same Wi-Fi or LAN:" -ForegroundColor Cyan
    foreach ($address in $addresses) {
        Write-Host "  http://${address}:8090"
    }

    Write-Host ""
    Write-Host "If another device cannot connect, allow TCP port 8090 through Windows Firewall." -ForegroundColor Yellow
}

function Backup-Database {
    Assert-Docker
    Initialize-EnvironmentFile
    New-Item -ItemType Directory -Force -Path $BackupDirectory | Out-Null

    $stamp = Get-Date -Format "yyyyMMdd-HHmmss"
    $fileName = "AtharDb-$stamp.bak"
    $containerPath = "/var/opt/mssql/backup/$fileName"

    $backupCommand = @'
set -e
mkdir -p /var/opt/mssql/backup
if [ -x /opt/mssql-tools18/bin/sqlcmd ]; then
  SQLCMD=/opt/mssql-tools18/bin/sqlcmd
else
  SQLCMD=/opt/mssql-tools/bin/sqlcmd
fi
"$SQLCMD" -S localhost -U sa -P "$MSSQL_SA_PASSWORD" -C -Q "BACKUP DATABASE [AtharDb] TO DISK = N'__BACKUP_PATH__' WITH INIT, CHECKSUM"
'@.Replace("__BACKUP_PATH__", $containerPath)

    Invoke-Compose -ComposeArguments @("exec", "-T", "athar-sqlserver", "bash", "-lc", $backupCommand)
    Invoke-Compose -ComposeArguments @("cp", "athar-sqlserver:$containerPath", (Join-Path $BackupDirectory $fileName))

    Write-Host "Database backup created:" -ForegroundColor Green
    Write-Host "  $(Join-Path $BackupDirectory $fileName)"
}

switch ($Action) {
    "Start" {
        Assert-Docker
        Initialize-EnvironmentFile
        Invoke-Compose -ComposeArguments @("up", "--build", "-d")
        Wait-UntilReady
        Show-AccessInformation
        Start-Process $BaseUrl
    }
    "Stop" {
        Assert-Docker
        Initialize-EnvironmentFile
        Invoke-Compose -ComposeArguments @("down", "--remove-orphans")
        Write-Host "Athar stopped. SQL Server data was preserved." -ForegroundColor Green
    }
    "Status" {
        Assert-Docker
        Initialize-EnvironmentFile
        Invoke-Compose -ComposeArguments @("ps")
        try {
            Invoke-RestMethod -Uri "$BaseUrl/health/ready" -TimeoutSec 3 | ConvertTo-Json -Depth 5
        }
        catch {
            Write-Host "The readiness endpoint is not available." -ForegroundColor Yellow
        }
    }
    "Open" {
        Start-Process $BaseUrl
        Show-AccessInformation
    }
    "Lan" {
        Show-LanUrls
    }
    "Backup" {
        Backup-Database
    }
    "Reset" {
        if (-not $Force) {
            throw "Reset deletes the local database. Run the command again with -Force to confirm."
        }

        Assert-Docker
        Initialize-EnvironmentFile
        Invoke-Compose -ComposeArguments @("down", "--volumes", "--remove-orphans")
        Remove-Item $EnvironmentFile -Force -ErrorAction SilentlyContinue
        Write-Host "Removed containers, local data, and experimental settings." -ForegroundColor Green
    }
}
