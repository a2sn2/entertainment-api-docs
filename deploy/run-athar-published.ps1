[CmdletBinding()]
param(
    [string]$ConnectionString = "Server=.;Database=Athar;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True",
    [string]$AdminEmail = "admin@athar.local",
    [string]$AdminDisplayName = "مسؤول منصة أثَر",
    [Parameter(Mandatory = $true)]
    [string]$AdminPassword,
    [string]$Url = "http://localhost:5068"
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$appDirectory = Join-Path $PSScriptRoot "app"
$executable = Join-Path $appDirectory "Athar.Api.exe"

if (-not (Test-Path $executable)) {
    throw "لم أجد Athar.Api.exe داخل مجلد app. تأكد أنك فككت الحزمة كاملة دون تغيير بنيتها."
}

$env:ASPNETCORE_ENVIRONMENT = "Development"
$env:ASPNETCORE_URLS = $Url
$env:ConnectionStrings__Athar = $ConnectionString
$env:AdminSeed__Enabled = "true"
$env:AdminSeed__Email = $AdminEmail
$env:AdminSeed__DisplayName = $AdminDisplayName
$env:AdminSeed__Password = $AdminPassword
$env:DatabaseStartup__MigrationAttempts = "30"
$env:DatabaseStartup__DelaySeconds = "2"

Write-Host "تشغيل أثَر من الحزمة المنشورة..." -ForegroundColor Green
Write-Host "الرابط: $Url" -ForegroundColor Cyan
Write-Host "المسؤول: $AdminEmail" -ForegroundColor Cyan
Write-Host "أوقف التطبيق باستخدام Ctrl+C." -ForegroundColor Yellow

Start-Process $Url
& $executable
