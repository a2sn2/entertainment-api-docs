# تشغيل مدار محليًا وإثبات الجاهزية

هذه الوثيقة تخص **Madar** داخل `apps/Madar` بعد اكتمال الشريحة الرأسية الأولى. الهدف هو تشغيل المنتج محليًا والتحقق من الجاهزية دون اعتبار ذلك نشرًا إنتاجيًا.

## حدود المسار الحالي

المسار التشغيلي المعتمد في هذه المرحلة هو Docker Compose:

```text
Madar.Client + Madar.Api
          ↓
      SQL Server
```

يدعم مدير المستودع الموحد أوامر مدار الأساسية:

```powershell
.\foundationkit.ps1 start  -Target Madar -Mode Docker
.\foundationkit.ps1 status -Target Madar
.\foundationkit.ps1 logs   -Target Madar
.\foundationkit.ps1 stop   -Target Madar
```

ويمكن استخدام المشغّل المتخصص مباشرةً عند الحاجة:

```powershell
.\scripts\madar-product.ps1 start
.\scripts\madar-product.ps1 status
.\scripts\madar-product.ps1 logs
.\scripts\madar-product.ps1 stop
```

`Madar` لا يملك مسار Native موحدًا في هذه المرحلة. لذلك:

- `-Target Madar -Mode Native` مرفوض بوضوح؛
- `-Target All -Mode Auto` يشمل Madar إذا كان Docker جاهزًا؛
- `-Target All -Mode Native` يحافظ على مسار Athar/Workbench الأصلي ويعرض تنبيهًا بأن Madar تم تجاوزه لأنه Docker-only؛
- `doctor` يفحص `/health/ready` على المنفذ `8100` ويعرض حالة Madar مع بقية التطبيقات.

المشغّل ينشئ عند أول تشغيل ملفًا محليًا فقط داخل:

```text
.local/madar-product.env
```

ويولّد كلمات مرور تطوير عشوائية بدل تضمين كلمات مرور قابلة لإعادة الاستخدام في المستودع. على Windows يقيّد ACL للملف على حساب Windows الحالي، ويرفض الاستمرار إذا تعذر تطبيق الحماية.

لا ترفع `.local/` إلى Git.

## أول تشغيل

من جذر المستودع:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\foundationkit.ps1 start -Target Madar -Mode Docker
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

استخدم `-Reset` فقط عندما تقصد تغيير بيانات الدخول المحلية. إذا كانت قاعدة بيانات Docker القديمة ما زالت موجودة، فإعادة توليد كلمات المرور لا تعيد كتابة PasswordHash لمستخدم موجود تلقائيًا؛ عند الحاجة إلى حذف بيانات التطوير أيضًا أوقف stack ثم احذف volume صراحةً وأعد التشغيل.

## الفرق بين Live و Ready

### Live

```text
GET /health/live
```

يعني أن عملية ASP.NET Core تعمل وتستطيع الرد على HTTP. هذه الاستجابة لا تثبت أن SQL Server أو الـschema جاهزان.

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

فلن يعتبر startup ناجحًا إلا إذا كانت قاعدة البيانات قابلة للاتصال ولا توجد migrations معلقة. هذا يسمح ببيئات تتطلب تنفيذ migrations بخطوة نشر مستقلة، دون أن يدّعي التطبيق أنه Ready بينما schema أقدم من الكود.

## الحالة والسجلات والإيقاف

### الحالة

```powershell
.\foundationkit.ps1 status -Target Madar
```

يعرض عمليًا أحد الأوضاع:

```text
STOPPED or unreachable
LIVE but NOT READY
READY
```

### السجلات

```powershell
.\foundationkit.ps1 logs -Target Madar
```

يعرض آخر سجلات Compose. لا تنسخ أسرار `.local/madar-product.env` إلى تقارير الأعطال.

### الإيقاف

```powershell
.\foundationkit.ps1 stop -Target Madar
```

يوقف Compose بدون حذف volume تلقائيًا. المشروع المحلي يستخدم اسم Compose ثابتًا:

```text
madar-product
```

ولحذف بيانات التطوير يدويًا، نفّذ ذلك فقط عندما يكون فقد البيانات مقصودًا:

```powershell
docker compose --project-name madar-product -f deploy/madar-compose.yml down --volumes --remove-orphans
```

قد يحتاج Compose إلى قيم المتغيرات المطلوبة عند قراءة الملف؛ المسار الطبيعي الموصى به هو استخدام المشغّل ثم تنفيذ الحذف المقصود ضمن جلسة تطوير مضبوطة.

مدير المستودع لا يعرّض `reset -Target Madar` عمدًا في v0.1.1 حتى لا يتحول حذف قاعدة البيانات إلى إجراء غير مقصود ضمن `-Target All`.

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

كما أن retry logic الخاص ببداية قاعدة البيانات له اختبارات آلية مستقلة داخل `Madar.Tests`، وWindows Launcher Check يتحقق من أن `foundationkit.ps1` ما زال صالحًا على Windows PowerShell 5.1 بعد إضافة Madar.

## Atlas / Pages

Atlas يحتوي قسمًا مستقلًا لـMadar ويستخرج مسارات Blazor الحقيقية من:

```text
apps/Madar/Madar.Client/Pages
```

ويتحقق آليًا من المسارات الحالية:

```text
/
/login
/cases
/cases/{CaseId:guid}
```

كما يوثق Swagger وCSRF وLive/Ready ودليل التشغيل الحالي. هذا يمنع بقاء صفحة أو route حقيقي خارج خريطة المستودع دون اكتشافه في CI.

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
