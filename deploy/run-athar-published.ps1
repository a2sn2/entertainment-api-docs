[CmdletBinding()]
param(
    [string]$ConnectionString = "Server=.;Database=Athar;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True",
    [string]$AdminEmail = "admin@athar.local",
    [string]$AdminDisplayName = "مسؤول منصة أثَر",
    [string]$AdminPassword,
    [string]$Url = "http://localhost:5068"
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$appDirectory = Join-Path $PSScriptRoot "app"
$executable = Join-Path $appDirectory "Athar.Api.exe"
$readyUrl = $Url.TrimEnd("/") + "/health/ready"

if (-not (Test-Path $executable)) {
    throw "لم أجد Athar.Api.exe داخل مجلد app. تأكد أنك فككت الحزمة كاملة دون تغيير بنيتها."
}

if ([string]::IsNullOrWhiteSpace($AdminPassword)) {
    $securePassword = Read-Host "أدخل كلمة مرور مؤقتة للمسؤول" -AsSecureString
    $passwordPointer = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($securePassword)
    try {
        $AdminPassword = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($passwordPointer)
    }
    finally {
        [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($passwordPointer)
    }
}

if ($AdminPassword.Length -lt 12) {
    throw "كلمة مرور المسؤول يجب أن تكون 12 حرفًا على الأقل."
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

$process = Start-Process `
    -FilePath $executable `
    -WorkingDirectory $appDirectory `
    -NoNewWindow `
    -PassThru

$ready = $false
for ($attempt = 1; $attempt -le 60; $attempt++) {
    if ($process.HasExited) {
        throw "توقف Athar.Api قبل الوصول إلى حالة الجاهزية. Exit code: $($process.ExitCode)"
    }

    try {
        Invoke-RestMethod -Uri $readyUrl -TimeoutSec 2 | Out-Null
        $ready = $true
        break
    }
    catch {
        Start-Sleep -Seconds 1
    }
}

if (-not $ready) {
    Stop-Process -Id $process.Id -Force -ErrorAction SilentlyContinue
    throw "لم يصل أثَر إلى حالة الجاهزية خلال 60 ثانية."
}

Write-Host "أثَر جاهز." -ForegroundColor Green
Start-Process $Url
Wait-Process -Id $process.Id
