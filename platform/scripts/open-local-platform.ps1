[CmdletBinding()]
param(
    [ValidateRange(1, 300)]
    [int]$TimeoutSeconds = 60
)

$ErrorActionPreference = 'Stop'

$targets = @(
    [pscustomobject]@{
        Name = 'API Swagger'
        ProbeUrl = 'http://localhost:5080/health'
        OpenUrl = 'http://localhost:5080/swagger'
    },
    [pscustomobject]@{
        Name = 'Client'
        ProbeUrl = 'http://localhost:5081'
        OpenUrl = 'http://localhost:5081'
    },
    [pscustomobject]@{
        Name = 'Admin'
        ProbeUrl = 'http://localhost:5082'
        OpenUrl = 'http://localhost:5082/login'
    }
)

function Test-LocalUrl {
    param([Parameter(Mandatory)][string]$Url)

    try {
        $response = Invoke-WebRequest -Uri $Url -UseBasicParsing -TimeoutSec 2
        return $response.StatusCode -ge 200 -and $response.StatusCode -lt 500
    }
    catch {
        return $false
    }
}

Write-Host 'Waiting for Entertainment Docs local services...' -ForegroundColor Cyan
$deadline = (Get-Date).AddSeconds($TimeoutSeconds)
$ready = @{}

while ((Get-Date) -lt $deadline) {
    foreach ($target in $targets) {
        if (-not $ready.ContainsKey($target.Name) -and (Test-LocalUrl -Url $target.ProbeUrl)) {
            $ready[$target.Name] = $true
            Write-Host "Ready: $($target.Name)" -ForegroundColor Green
        }
    }

    if ($ready.Count -eq $targets.Count) {
        break
    }

    Start-Sleep -Seconds 1
}

$opened = 0
foreach ($target in $targets) {
    if ($ready.ContainsKey($target.Name)) {
        Start-Process $target.OpenUrl
        $opened++
    }
    else {
        Write-Warning "$($target.Name) is not reachable at $($target.ProbeUrl)."
    }
}

if ($opened -eq 0) {
    throw 'No local Entertainment Docs service became reachable. Start the API, Client, and Admin projects first.'
}

Write-Host "Opened $opened local page(s)." -ForegroundColor Green
