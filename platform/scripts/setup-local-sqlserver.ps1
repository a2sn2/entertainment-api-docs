[CmdletBinding()]
param(
    [string]$Server = $env:COMPUTERNAME,
    [string]$Database = "EntertainmentDocs_Dev",
    [switch]$StartApi
)

$ErrorActionPreference = "Stop"
$platformRoot = Split-Path -Parent $PSScriptRoot
$solution = Join-Path $platformRoot "EntertainmentDocs.sln"
$apiProject = Join-Path $platformRoot "src\EntertainmentDocs.Api\EntertainmentDocs.Api.csproj"
$infrastructureProject = Join-Path $platformRoot "src\EntertainmentDocs.Infrastructure\EntertainmentDocs.Infrastructure.csproj"

if ([string]::IsNullOrWhiteSpace($Server)) {
    throw "SQL Server name is required. Pass -Server 'SERVER\INSTANCE' when using a named instance."
}

$connectionString = "Server=$Server;Database=$Database;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True"

Write-Host "Entertainment Docs - SQL Server local setup" -ForegroundColor Cyan
Write-Host "Server   : $Server"
Write-Host "Database : $Database"
Write-Host "Auth     : Windows Authentication"
Write-Host ""

$env:ASPNETCORE_ENVIRONMENT = "Development"
$env:ConnectionStrings__SqlServer = $connectionString
$env:ENTERTAINMENTDOCS_SQLSERVER = $connectionString

Push-Location $platformRoot
try {
    Write-Host "[1/4] Restoring local .NET tools..." -ForegroundColor Yellow
    dotnet tool restore
    if ($LASTEXITCODE -ne 0) { throw "dotnet tool restore failed." }

    Write-Host "[2/4] Restoring solution packages..." -ForegroundColor Yellow
    dotnet restore $solution
    if ($LASTEXITCODE -ne 0) { throw "dotnet restore failed." }

    Write-Host "[3/4] Building the solution..." -ForegroundColor Yellow
    dotnet build $solution --configuration Debug --no-restore
    if ($LASTEXITCODE -ne 0) { throw "dotnet build failed." }

    Write-Host "[4/4] Applying EF Core migrations to SQL Server..." -ForegroundColor Yellow
    dotnet ef database update `
        --project $infrastructureProject `
        --startup-project $apiProject `
        --context AppDbContext
    if ($LASTEXITCODE -ne 0) {
        throw "Database migration failed. Verify that SQL Server is running and that '$Server' accepts Windows Authentication."
    }

    Write-Host ""
    Write-Host "SQL Server setup completed successfully." -ForegroundColor Green
    Write-Host "Database created/updated: $Database"
    Write-Host "Local administrator: admin@local.test"
    Write-Host "Local password     : LocalAdmin!2026"
    Write-Host ""

    if ($StartApi) {
        Write-Host "Starting API on http://localhost:5080 ..." -ForegroundColor Cyan
        dotnet run --project $apiProject --launch-profile http
    }
    else {
        Write-Host "Next: set EntertainmentDocs.Api as Startup Project and press F5, or run:" -ForegroundColor Cyan
        $runCommand = 'dotnet run --project "{0}" --launch-profile http' -f $apiProject
        Write-Host $runCommand
    }
}
finally {
    Pop-Location
}
