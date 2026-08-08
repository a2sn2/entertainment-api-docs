[CmdletBinding()]
param(
    [Parameter(Mandatory = $true, Position = 0)]
    [ValidateSet('start', 'status', 'logs', 'stop')]
    [string]$Action,

    [switch]$Reset,

    [string]$BaseUrl = 'http://localhost:8100'
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$RepoRoot = Split-Path -Parent $PSScriptRoot
$ComposeFile = Join-Path $RepoRoot 'deploy\madar-compose.yml'
$LocalRoot = Join-Path $RepoRoot '.local'
$ConfigPath = Join-Path $LocalRoot 'madar-product.env'

function New-RandomSecret {
    param([int]$Bytes = 24)

    $buffer = New-Object byte[] $Bytes
    $rng = [System.Security.Cryptography.RandomNumberGenerator]::Create()
    try {
        $rng.GetBytes($buffer)
    }
    finally {
        $rng.Dispose()
    }

    return ([Convert]::ToBase64String($buffer) -replace '[+/=]', 'x') + '!Aa1'
}

function Protect-LocalFile {
    param([Parameter(Mandatory = $true)][string]$Path)

    if (-not $IsWindows -and $PSVersionTable.PSVersion.Major -ge 6) {
        return
    }

    if ($env:OS -ne 'Windows_NT') {
        return
    }

    $identity = [System.Security.Principal.WindowsIdentity]::GetCurrent().Name
    & icacls $Path /inheritance:r | Out-Null
    & icacls $Path /grant:r "$identity`:(R,W)" | Out-Null
    if ($LASTEXITCODE -ne 0) {
        throw "Unable to restrict Madar local credential file ACL: $Path"
    }
}

function Get-LocalConfig {
    if (-not (Test-Path $ConfigPath)) {
        return $null
    }

    $values = @{}
    Get-Content -LiteralPath $ConfigPath -Encoding UTF8 | ForEach-Object {
        $line = $_.Trim()
        if (-not $line -or $line.StartsWith('#')) {
            return
        }

        $separator = $line.IndexOf('=')
        if ($separator -le 0) {
            return
        }

        $values[$line.Substring(0, $separator)] = $line.Substring($separator + 1)
    }

    return $values
}

function Initialize-LocalConfig {
    if ($Reset -and (Test-Path $ConfigPath)) {
        Remove-Item -LiteralPath $ConfigPath -Force
    }

    $existing = Get-LocalConfig
    if ($null -ne $existing) {
        return $existing
    }

    New-Item -ItemType Directory -Path $LocalRoot -Force | Out-Null
    $values = [ordered]@{
        MADAR_SQL_PASSWORD      = New-RandomSecret
        MADAR_ADMIN_EMAIL       = 'admin@madar.local'
        MADAR_ADMIN_PASSWORD    = New-RandomSecret
        MADAR_OPERATOR_EMAIL    = 'operator@madar.local'
        MADAR_OPERATOR_PASSWORD = New-RandomSecret
    }

    $content = @(
        '# Local Madar development credentials. Do not commit this file.'
        ($values.GetEnumerator() | ForEach-Object { '{0}={1}' -f $_.Key, $_.Value })
    )
    [System.IO.File]::WriteAllLines($ConfigPath, $content, [System.Text.UTF8Encoding]::new($false))
    Protect-LocalFile -Path $ConfigPath
    return $values
}

function Invoke-WithMadarEnvironment {
    param(
        [Parameter(Mandatory = $true)][hashtable]$Values,
        [Parameter(Mandatory = $true)][scriptblock]$Script
    )

    $original = @{}
    foreach ($name in $Values.Keys) {
        $original[$name] = [Environment]::GetEnvironmentVariable($name, 'Process')
        [Environment]::SetEnvironmentVariable($name, [string]$Values[$name], 'Process')
    }

    try {
        & $Script
    }
    finally {
        foreach ($name in $Values.Keys) {
            [Environment]::SetEnvironmentVariable($name, $original[$name], 'Process')
        }
    }
}

function Test-DockerReady {
    & docker info --format '{{.ServerVersion}}' 2>$null | Out-Null
    return $LASTEXITCODE -eq 0
}

function Get-Health {
    param([string]$Path)

    try {
        return Invoke-RestMethod -Uri ($BaseUrl.TrimEnd('/') + $Path) -TimeoutSec 5
    }
    catch {
        return $null
    }
}

function Show-Status {
    $live = Get-Health -Path '/health/live'
    $ready = Get-Health -Path '/health/ready'

    if ($null -eq $live) {
        Write-Host 'Madar: STOPPED or unreachable' -ForegroundColor Yellow
        return
    }

    if ($null -ne $ready -and $ready.status -eq 'ready') {
        Write-Host 'Madar: READY' -ForegroundColor Green
    }
    else {
        Write-Host 'Madar: LIVE but NOT READY' -ForegroundColor Yellow
    }

    Write-Host "URL: $BaseUrl"
}

if (-not (Test-Path $ComposeFile)) {
    throw "Madar compose file not found: $ComposeFile"
}

switch ($Action) {
    'start' {
        if (-not (Get-Command docker -ErrorAction SilentlyContinue)) {
            throw 'Docker is required for the current Madar operational launcher.'
        }
        if (-not (Test-DockerReady)) {
            throw 'Docker Desktop/Engine is not ready.'
        }

        $config = Initialize-LocalConfig
        Invoke-WithMadarEnvironment -Values $config -Script {
            & docker compose -f $ComposeFile up --build -d
            if ($LASTEXITCODE -ne 0) {
                throw 'Madar Docker startup failed.'
            }
        }

        for ($attempt = 1; $attempt -le 90; $attempt++) {
            $ready = Get-Health -Path '/health/ready'
            if ($null -ne $ready -and $ready.status -eq 'ready') {
                break
            }
            Start-Sleep -Seconds 2
        }

        Show-Status
        Write-Host ''
        Write-Host 'Local development accounts:' -ForegroundColor Cyan
        Write-Host ("  Administrator: {0}" -f $config['MADAR_ADMIN_EMAIL'])
        Write-Host ("  Operator:      {0}" -f $config['MADAR_OPERATOR_EMAIL'])
        Write-Host "Credentials are stored in $ConfigPath with local-only ACLs where supported."
    }

    'status' {
        Show-Status
    }

    'logs' {
        if (-not (Get-Command docker -ErrorAction SilentlyContinue)) {
            throw 'Docker is required to read Madar container logs.'
        }
        & docker compose -f $ComposeFile logs --no-color --tail=250
        if ($LASTEXITCODE -ne 0) {
            throw 'Unable to read Madar Docker logs.'
        }
    }

    'stop' {
        if (-not (Get-Command docker -ErrorAction SilentlyContinue)) {
            throw 'Docker is required to stop the Madar Compose topology.'
        }

        $config = Get-LocalConfig
        if ($null -eq $config) {
            $config = @{
                MADAR_SQL_PASSWORD = 'unused'
                MADAR_ADMIN_EMAIL = 'unused@madar.local'
                MADAR_ADMIN_PASSWORD = 'unused'
                MADAR_OPERATOR_EMAIL = 'unused@madar.local'
                MADAR_OPERATOR_PASSWORD = 'unused'
            }
        }

        Invoke-WithMadarEnvironment -Values $config -Script {
            & docker compose -f $ComposeFile down --remove-orphans
            if ($LASTEXITCODE -ne 0) {
                throw 'Madar Docker stop failed.'
            }
        }

        Write-Host 'Madar stopped.' -ForegroundColor Green
    }
}
