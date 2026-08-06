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
        throw "الأداة '$Name' غير مثبتة أو غير موجودة في PATH."
    }
}

function Assert-Docker {
    Assert-Command "docker"
    docker info *> $null
    if ($LASTEXITCODE -ne 0) {
        throw "Docker Desktop غير شغال. افتحه وانتظر حتى تصبح الخدمة جاهزة ثم أعد المحاولة."
    }

    docker compose version *> $null
    if ($LASTEXITCODE -ne 0) {
        throw "Docker Compose غير متاح ضمن Docker Desktop."
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

    @(
        "ATHAR_SQL_PASSWORD=$sqlPassword"
        "ATHAR_ADMIN_EMAIL=admin@athar.local"
        "ATHAR_ADMIN_PASSWORD=$adminPassword"
    ) | Set-Content -Path $EnvironmentFile -Encoding utf8

    Write-Host "تم إنشاء إعدادات التجربة المحلية في .local/athar-product.env" -ForegroundColor Green
    Write-Host "هذا الملف مستبعد من Git ولا يتم رفع الأسرار إلى المستودع." -ForegroundColor DarkYellow
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
    param([Parameter(ValueFromRemainingArguments)][string[]]$Arguments)

    & docker compose `
        --project-name $ProjectName `
        --env-file $EnvironmentFile `
        -f $ComposeFile `
        @Arguments

    if ($LASTEXITCODE -ne 0) {
        throw "فشل أمر Docker Compose. راجع الرسائل السابقة."
    }
}

function Wait-UntilReady {
    param([int]$Attempts = 120)

    Write-Host "بانتظار API وقاعدة البيانات..." -ForegroundColor Cyan

    for ($attempt = 1; $attempt -le $Attempts; $attempt++) {
        try {
            $response = Invoke-RestMethod -Uri "$BaseUrl/health/ready" -TimeoutSec 3
            if ($null -ne $response) {
                Write-Host "أثَر جاهز للعمل." -ForegroundColor Green
                return
            }
        }
        catch {
            Start-Sleep -Seconds 2
        }
    }

    Invoke-Compose logs --tail 150 athar-api
    throw "لم يصل أثَر إلى حالة الجاهزية خلال المهلة المحددة."
}

function Show-AccessInformation {
    $values = Get-EnvironmentValues

    Write-Host ""
    Write-Host "روابط المنتج التجريبي" -ForegroundColor Green
    Write-Host "  الرئيسية:     $BaseUrl"
    Write-Host "  الحساب:       $BaseUrl/account"
    Write-Host "  مبادراتي:     $BaseUrl/initiatives"
    Write-Host "  الإدارة:      $BaseUrl/admin"
    Write-Host "  Swagger:      $BaseUrl/swagger"
    Write-Host "  الجاهزية:     $BaseUrl/health/ready"
    Write-Host ""
    Write-Host "حساب الإدارة المحلي" -ForegroundColor Cyan
    Write-Host "  البريد:       $($values['ATHAR_ADMIN_EMAIL'])"
    Write-Host "  كلمة المرور:  $($values['ATHAR_ADMIN_PASSWORD'])"
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
        Write-Host "لم أجد عنوان IPv4 مناسبًا. افتح ipconfig وتحقق من عنوان الشبكة." -ForegroundColor Yellow
        return
    }

    Write-Host "روابط محتملة داخل نفس شبكة Wi-Fi/LAN:" -ForegroundColor Cyan
    foreach ($address in $addresses) {
        Write-Host "  http://${address}:8090"
    }

    Write-Host ""
    Write-Host "إذا لم يفتح الرابط من جهاز آخر، اسمح للمنفذ 8090 في Windows Firewall يدويًا." -ForegroundColor Yellow
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

    Invoke-Compose exec -T athar-sqlserver bash -lc $backupCommand
    Invoke-Compose cp "athar-sqlserver:$containerPath" (Join-Path $BackupDirectory $fileName)

    Write-Host "تم إنشاء النسخة الاحتياطية:" -ForegroundColor Green
    Write-Host "  $(Join-Path $BackupDirectory $fileName)"
}

switch ($Action) {
    "Start" {
        Assert-Docker
        Initialize-EnvironmentFile
        Invoke-Compose up --build -d
        Wait-UntilReady
        Show-AccessInformation
        Start-Process $BaseUrl
    }
    "Stop" {
        Assert-Docker
        Initialize-EnvironmentFile
        Invoke-Compose down --remove-orphans
        Write-Host "تم إيقاف أثَر مع الاحتفاظ ببيانات SQL Server." -ForegroundColor Green
    }
    "Status" {
        Assert-Docker
        Initialize-EnvironmentFile
        Invoke-Compose ps
        try {
            Invoke-RestMethod -Uri "$BaseUrl/health/ready" -TimeoutSec 3 | ConvertTo-Json -Depth 5
        }
        catch {
            Write-Host "واجهة الجاهزية غير متاحة الآن." -ForegroundColor Yellow
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
            throw "Reset يحذف قاعدة البيانات المحلية. أعد الأمر مع -Force للتأكيد."
        }

        Assert-Docker
        Initialize-EnvironmentFile
        Invoke-Compose down --volumes --remove-orphans
        Remove-Item $EnvironmentFile -Force -ErrorAction SilentlyContinue
        Write-Host "تم حذف الحاويات والبيانات المحلية وإعدادات التجربة." -ForegroundColor Green
    }
}
