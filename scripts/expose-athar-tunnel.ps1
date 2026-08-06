$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$BaseUrl = "http://localhost:8090"

try {
    Invoke-RestMethod -Uri "$BaseUrl/health/ready" -TimeoutSec 3 | Out-Null
}
catch {
    throw "أثَر غير جاهز على $BaseUrl. شغله أولًا عبر START-ATHAR.cmd أو scripts/athar-product.ps1 -Action Start."
}

$cloudflared = Get-Command "cloudflared" -ErrorAction SilentlyContinue
if (-not $cloudflared) {
    Write-Host "تعذر تشغيل النفق لأن cloudflared غير مثبت على الجهاز." -ForegroundColor Yellow
    Write-Host "بعد تثبيته وإتاحته في PATH أعد تشغيل هذا السكربت." -ForegroundColor Yellow
    Write-Host "لن يغير السكربت إعدادات الراوتر أو الجدار الناري." -ForegroundColor DarkYellow
    exit 1
}

Write-Host "سيتم إنشاء رابط HTTPS مؤقت يوجه إلى $BaseUrl" -ForegroundColor Cyan
Write-Host "اترك هذه النافذة مفتوحة طوال مدة العرض، وأوقفها بـ Ctrl+C." -ForegroundColor Yellow
Write-Host "لا تستخدم بيانات حقيقية أو حساسة في الرابط التجريبي." -ForegroundColor Red

& cloudflared tunnel --url $BaseUrl
