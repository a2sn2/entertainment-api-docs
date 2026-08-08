# تشغيل مدار محليًا وإثبات الجاهزية

هذه الوثيقة تخص **Madar** داخل `apps/Madar` بعد اكتمال الشريحة الرأسية الأولى. الهدف هو تشغيل المنتج محليًا والتحقق من الجاهزية دون اعتبار ذلك نشرًا إنتاجيًا.

## حدود المسار الحالي

المسار التشغيلي المعتمد في هذه المرحلة هو Docker Compose:

```text
Madar.Client + Madar.Api
          ↓
      SQL Server
```

ويُدار عبر:

```powershell
.\scripts\madar-product.ps1 start
.\scripts\madar-product.ps1 status
.\scripts\madar-product.ps1 logs
.\scripts\madar-product.ps1 stop
```

المشغّل ينشئ عند أول تشغيل ملفًا محليًا فقط داخل:

```text
.local/madar-product.env
```

ويولّد كلمات مرور تطوير عشوائية بدل تضمين كلمات مرور قابلة لإعادة الاستخدام في المستودع. على Windows يحاول تقييد ACL للملف على حساب Windows الحالي، ويفشل التشغيل إذا تعذر تطبيق الحماية.

لا ترفع `.local/` إلى Git.

## أول تشغيل

من جذر المستودع:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\madar-product.ps1 start
```

بعد نجاح التشغيل:

```text
http://localhost:8100/
http://localhost:8100/swagger
http://localhost:8100/health/live
http://localhost:8100/health/ready
```

الحسابان المحليان الافتراضيان من حيث البريد فقط:

```text
Administrator: admin@madar.local
Operator:      operator@madar.local
```

كلمات المرور المولدة موجودة في `.local/madar-product.env` وليست موثقة هنا عمدًا.

لإنشاء مجموعة أسرار محلية جديدة وحذف المجموعة القديمة:

```powershell
.\scripts\madar-product.ps1 start -Reset
```

استخدم `-Reset` فقط عندما تقصد تغيير بيانات الدخول المحلية. إذا كانت قاعدة بيانات Docker القديمة ما زالت موجودة، فإعادة توليد كلمات المرور لا تعيد كتابة PasswordHash لمستخدم موجود تلقائيًا؛ للحذف الكامل استخدم `docker compose down --volumes` ثم أعد التشغيل.

## الفرق بين Live و Ready

### Live

```text
GET /health/live
```

يعني أن عملية ASP.NET Core تعمل وتستطيع الرد على HTTP.

الاستجابة لا تثبت أن SQL Server أو الـschema جاهزان.

### Ready

```text
GET /health/ready
```

يفحص بصورة محدودة:

1. إمكانية الاتصال بـSQL Server عبر `MadarDbContext`.
2. عدم وجود EF Core migrations معلقة مقارنة بالـmodel الحالي.

عند الجاهزية:

```json
{
  "status": "ready",
  "service": "madar-api"
}
```

وعند عدم الجاهزية يرجع HTTP `503` دون كشف اسم الخادم أو connection string أو تفاصيل بنية SQL.

## سياسة Startup ومهاجرات قاعدة البيانات

الإعدادات الخاصة بمدار:

```text
Madar:DatabaseStartup:ApplyMigrationsOnStartup
Madar:DatabaseStartup:SeedRolesOnStartup
Madar:DatabaseStartup:MigrationAttempts
Madar:DatabaseStartup:DelaySeconds
```

القيم الافتراضية الحالية:

```text
ApplyMigrationsOnStartup = true
SeedRolesOnStartup       = true
MigrationAttempts        = 60
DelaySeconds             = 2
```

الحدود التي يقبلها الـAPI عند startup:

```text
MigrationAttempts: 1..300
DelaySeconds:      0..30
```

في حال وجود خطأ transient أثناء startup، يعيد Madar محاولة مسار قاعدة البيانات ضمن عدد محدود بدل الاعتماد على محاولة واحدة فقط.

إذا كان:

```text
ApplyMigrationsOnStartup = false
```

فلن يعتبر startup ناجحًا إلا إذا:

- قاعدة البيانات قابلة للاتصال؛ و
- لا توجد migrations معلقة.

هذا يسمح ببيئات تتطلب تنفيذ migrations بخطوة نشر مستقلة، دون أن يدّعي التطبيق أنه Ready بينما schema أقدم من الكود.

## الحالة والسجلات والإيقاف

### الحالة

```powershell
.\scripts\madar-product.ps1 status
```

يعرض أحد الأوضاع عمليًا:

```text
STOPPED or unreachable
LIVE but NOT READY
READY
```

### السجلات

```powershell
.\scripts\madar-product.ps1 logs
```

يعرض آخر سجلات Compose. لا تنسخ أسرار `.local/madar-product.env` إلى تقارير الأعطال.

### الإيقاف

```powershell
.\scripts\madar-product.ps1 stop
```

يوقف Compose بدون حذف volume تلقائيًا.

لإزالة بيانات التطوير أيضًا:

```powershell
$env:MADAR_SQL_PASSWORD = '<local value>'
$env:MADAR_ADMIN_EMAIL = 'admin@madar.local'
$env:MADAR_ADMIN_PASSWORD = '<local value>'
$env:MADAR_OPERATOR_EMAIL = 'operator@madar.local'
$env:MADAR_OPERATOR_PASSWORD = '<local value>'
docker compose -f deploy/madar-compose.yml down --volumes --remove-orphans
```

## التحقق الآلي

المستودع يتحقق من Madar في أكثر من مستوى:

```text
Build/Test
    ↓
Publish
    ↓
Container hardening
    ↓
SQL Server + Auth + Case lifecycle + Audit smoke
    ↓
Readiness check
    ↓
Trivy Madar image gate + SARIF
    ↓
CodeQL
```

`scripts/smoke-madar.sh` يتحقق من `/health/ready` ثم ينفذ المسار التشغيلي الحقيقي:

```text
anonymous rejection
→ admin login + CSRF
→ create case
→ assign operator
→ operator login
→ scoped visibility
→ in-progress
→ resolved
→ admin close
→ persisted audit timeline
```

كما أن retry logic الخاص ببداية قاعدة البيانات له اختبارات آلية مستقلة داخل `Madar.Tests`.

## ما الذي لا تثبته هذه الأدلة؟

هذا المسار لا يثبت تلقائيًا:

- Production Approval؛
- استقلالية Segregation of Duties؛
- ISO/IEC 27001 certification؛
- production KMS/Vault؛
- production network topology؛
- RPO/RTO المقبولين فعليًا؛
- penetration/load acceptance؛
- retention/privacy policy قانونية للبيانات.

هذه قرارات وأدلة خاصة ببيئة التشغيل والمنظمة، وليست شيئًا يمكن للمستودع اختراعه.

## قاعدة التطوير التالية

بعد إغلاق جاهزية v0.1.1، يمكن الانتقال إلى عمق المنتج مثل SLA/escalation. عند ظهور حاجة عامة مثل scheduled work يجب تنفيذ الحاجة أولًا كـconsumer حقيقي داخل Madar، ثم تقييم ما إذا كانت هناك أدلة كافية لاستخراج capability عامة إلى FoundationKit بدل إضافة package مسبقًا.
