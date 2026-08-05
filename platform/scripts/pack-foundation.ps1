param(
    [string]$Configuration = "Release",
    [string]$OutputDirectory = "artifacts/foundation"
)

$ErrorActionPreference = "Stop"
$platformRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$outputPath = Join-Path $platformRoot $OutputDirectory

New-Item -ItemType Directory -Path $outputPath -Force | Out-Null

$projects = Get-ChildItem -Path (Join-Path $platformRoot "core") -Filter "FoundationKit.*.csproj" -Recurse |
    Sort-Object FullName

if ($projects.Count -eq 0) {
    throw "No FoundationKit projects were found."
}

foreach ($project in $projects) {
    Write-Host "Packing $($project.Name)..."
    & dotnet pack $project.FullName --configuration $Configuration --output $outputPath
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet pack failed for $($project.FullName)."
    }
}

Write-Host "FoundationKit packages created in: $outputPath"
Get-ChildItem -Path $outputPath -Filter "*.nupkg" | Select-Object Name, Length
