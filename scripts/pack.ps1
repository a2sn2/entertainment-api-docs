[CmdletBinding()]
param(
    [string]$Configuration = "Release",
    [string]$Output = "artifacts/packages"
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
Push-Location $root

try {
    if (Test-Path $Output) {
        Remove-Item $Output -Recurse -Force
    }

    New-Item $Output -ItemType Directory -Force | Out-Null

    dotnet restore FoundationKit.sln
    if ($LASTEXITCODE -ne 0) { throw "Restore failed." }

    dotnet build FoundationKit.sln --configuration $Configuration --no-restore
    if ($LASTEXITCODE -ne 0) { throw "Build failed." }

    $projects = Get-ChildItem "src/FoundationKit.*" -Filter "FoundationKit.*.csproj" |
        Sort-Object FullName

    if ($projects.Count -ne 11) {
        throw "Expected exactly eleven FoundationKit package projects, found $($projects.Count)."
    }

    foreach ($project in $projects) {
        dotnet pack $project.FullName `
            --configuration $Configuration `
            --no-build `
            --output $Output

        if ($LASTEXITCODE -ne 0) {
            throw "Packing $($project.Name) failed."
        }
    }

    $packages = @(Get-ChildItem $Output -Filter "*.nupkg" |
        Where-Object { $_.Name -notlike "*.symbols.nupkg" })
    $symbols = @(Get-ChildItem $Output -Filter "*.snupkg")

    if ($packages.Count -ne 11 -or $symbols.Count -ne 11) {
        throw "Expected eleven packages and eleven symbol packages."
    }

    Write-Host "Created 11 packages and 11 symbol packages in $Output"
}
finally {
    Pop-Location
}
