# تشغيل مدار محليًا وإثبات الجاهزية

هذه الوثيقة تخص **Madar** داخل `apps/Madar`. الهدف هو تشغيل المنتج محليًا والتحقق من الجاهزية واختبار SLA بشكل مقصود دون اعتبار ذلك نشرًا إنتاجيًا أو اعتمادًا لقيم SLA في بيئة حقيقية.

## حدود المسار الحالي

المسار التشغيلي المعتمد هو Docker Compose:

```text
Madar.Client + Madar.Api
          ↓
      SQL Server
```

يدعم مدير المستودع الموحد:

```powershell
.\foundationkit.ps1 start  -Target Madar -Mode Docker
.\foundationkit.ps1 status -Target Madar
.\foundationkit.ps1 logs   -Target Madar
.\foundationkit.ps1 stop   -Target Madar
```

ويمكن استخدام المشغّل المتخصص:

```powershell
.\scripts\madar-product.ps1 start
.\scripts\madar-product.ps1 status
.\scripts\madar-product.ps1 logs
.\scripts\madar-product.ps1 stop
```

`Madar` لا يملك مسار Native موحدًا حاليًا. لذلك:

- `-Target Madar -Mode Native` مرفوض؛
- `-Target All -Mode Auto` يشمل Madar إذا كان Docker جاهزًا؛
- `-Target All -Mode Native` يحافظ على Athar/Workbench ويعرض أن Madar تم تجاوزه؛
- `doctor` يفحص `/health/ready` على `8100`.

## الملف المحلي

عند أول تشغيل ينشئ المشغّل:

```text
.local/madar-product.env
```

ويولّد كلمات مرور تطوير عشوائية لـSQL Server وAdministrator وOperator. على Windows يقيّد ACL للملف على حساب Windows الحالي ويرفض الاستمرار إذا تعذر تطبيق الحماية.

الملف يحتوي أيضًا إعدادات SLA المحلية، لكن SLA يكون **معطلاً افتراضيًا**:

```text
MADAR_SLA_ENABLED=false
MADAR_SLA_LOW=01:00:00
MADAR_SLA_MEDIUM=01:00:00
MADAR_SLA_HIGH=01:00:00
MADAR_SLA_CRITICAL=01:00:00
```

قيم الساعة الواحدة هنا **placeholders للتطوير فقط** وليست سياسة إنتاج ولا توصية تجارية. عندما يكون `MADAR_SLA_ENABLED=false` لا تُطبق هذه المدد على الحالات الجديدة.

لا ترفع `.local/` إلى Git.

## أول تشغيل

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\foundationkit.ps1 start -Target Madar -Mode Docker
```

بعد النجاح:

```text
http://localhost:8100/
http://localhost:8100/login
http://localhost:8100/cases
http://localhost:8100/swagger
http://localhost:8100/health/live
http://localhost:8100/health/ready
```

البريدان المحليان الافتراضيان:

```text
Administrator: admin@madar.local
Operator:      operator@madar.local
```

كلمات المرور المولدة موجودة فقط في `.local/madar-product.env`.

لإنشاء مجموعة أسرار محلية جديدة:

```powershell
.\scripts\madar-product.ps1 start -Reset
```

`-Reset` يعيد إنشاء ملف الإعدادات، لكنه لا يعيد تلقائيًا كتابة PasswordHash لمستخدم موجود في volume قديم. عند الحاجة إلى بيئة جديدة بالكامل يجب حذف volume عمدًا بعد التأكد أن البيانات تجريبية.

## تفعيل SLA للتطوير

عدّل `.local/madar-product.env` قبل تشغيل/إعادة تشغيل Madar، مثلًا:

```text
MADAR_SLA_ENABLED=true
MADAR_SLA_LOW=04:00:00
MADAR_SLA_MEDIUM=02:00:00
MADAR_SLA_HIGH=01:00:00
MADAR_SLA_CRITICAL=00:30:00
```

هذه الأرقام مجرد مثال تطويري. السياسة الحقيقية يجب أن تأتي من صاحب المنتج/الإجراءات التشغيلية.

ثم:

```powershell
.\foundationkit.ps1 stop -Target Madar
.\foundationkit.ps1 start -Target Madar -Mode Docker
```

إعدادات ASP.NET Core المقابلة هي:

```text
Madar:Sla:Enabled
Madar:Sla:Low
Madar:Sla:Medium
Madar:Sla:High
Madar:Sla:Critical
```

عند `Enabled=true` يجب أن تكون المدد الأربع موجودة، أكبر من صفر، ولا تتجاوز 365 يومًا. فشل هذا التحقق يمنع startup بدل تشغيل سياسة SLA ناقصة أو غامضة.

## معنى SLA في v0.2

في الشريحة الحالية:

```text
SlaTargetUtc = CreatedUtc + duration snapshot
```

أي أن مدة الأولوية تُقرأ عند إنشاء الحالة ثم يُحفظ الموعد النهائي داخل الحالة نفسها. تغيير configuration لاحقًا لا يغير هدف الحالات القديمة.

الحالات:

```text
not-applicable  السياسة كانت معطلة عند إنشاء الحالة
active          الحالة غير محلولة والوقت <= الهدف
met             تم الحل في أو قبل الهدف
breached        تجاوز الوقت الهدف أو تم الحل بعده
```

قاعدة الحد الزمني دقيقة: عند `now == SlaTargetUtc` ما زالت الحالة ضمن المهلة. يبدأ الخرق فقط عندما يصبح الوقت **أكبر** من الهدف. والحل بالضبط عند الهدف يعد `met`.

عند أول خرق يُحفظ:

```text
SlaBreachedUtc = SlaTargetUtc
EscalatedUtc   = وقت أول اكتشاف/تقييم للخرق
```

الأول يمثل لحظة تجاوز العقد زمنيًا، والثاني يمثل وقت تسجيل Madar للتصعيد. إعادة التقييم لا تغيّر هذين الحقلين ولا تنشئ audit event آخر لنفس الخرق.

## تقييم SLA يدويًا/تشغيليًا

المسار الحالي:

```text
POST /api/cases/sla/evaluate
```

Body:

```json
{
  "limit": 50
}
```

الحد المقبول `1..100`.

هذا المسار:

- يتطلب تسجيل الدخول؛
- متاح حاليًا لـSupervisor وAdministrator؛
- يمر عبر anti-CSRF وwrite rate limit؛
- يعالج الحالات غير المحلولة والمتأخرة التي لم يسجل لها خرق مسبقًا؛
- يعيد `evaluatedCount`, `breachedCount`, `hasMore`؛
- يسجل `madar.case.sla-breached` مرة واحدة لكل حالة.

هذا **ليس scheduler**. هو command واضح يمكن لمجدول مستقبلي استدعاؤه. لا يوجد في v0.2 اختيار Hangfire/Quartz/HostedService/Cloud Scheduler، ولا توجد حزمة `FoundationKit.Jobs`. اختيار التنفيذ الدوري مؤجل حتى وجود قرار تشغيل حقيقي.

كذلك لا توجد business-hours/holidays/pause-resume semantics في هذه المرحلة؛ القياس الحالي elapsed UTC من وقت إنشاء الحالة.

## Late resolution

إذا تم حل الحالة بعد الهدف قبل تشغيل evaluator، يكتشف `CaseManager` الخرق أثناء عملية `resolve` نفسها ويسجل breach/escalation/audit. هذا يمنع اعتماد صحة SLA على توقيت scheduler غير الموجود حاليًا.

## Live و Ready

### Live

```text
GET /health/live
```

يثبت أن ASP.NET Core يعمل فقط.

### Ready

```text
GET /health/ready
```

يتحقق من:

1. إمكانية الاتصال بـSQL Server.
2. عدم وجود EF Core migrations معلقة.

عند الجاهزية:

```json
{
  "status": "ready",
  "service": "madar-api"
}
```

وعند عدم الجاهزية يرجع HTTP `503` دون كشف connection string أو تفاصيل SQL.

## Startup ومهاجرات قاعدة البيانات

```text
Madar:DatabaseStartup:ApplyMigrationsOnStartup
Madar:DatabaseStartup:SeedRolesOnStartup
Madar:DatabaseStartup:MigrationAttempts
Madar:DatabaseStartup:DelaySeconds
```

القيم الافتراضية:

```text
ApplyMigrationsOnStartup = true
SeedRolesOnStartup       = true
MigrationAttempts        = 60
DelaySeconds             = 2
```

الحدود:

```text
MigrationAttempts: 1..300
DelaySeconds:      0..30
```

إذا كان `ApplyMigrationsOnStartup=false`، فلا يصبح startup ناجحًا إلا إذا كانت قاعدة البيانات قابلة للاتصال ولا توجد migrations معلقة.

v0.2 يضيف migration خاصة بـSLA:

```text
20260808110000_AddMadarSla.cs
```

وتضيف `SlaTargetUtc`, `SlaBreachedUtc`, `EscalatedUtc` وفهرس الاستعلام عن الحالات المستحقة. EF migrations تبقى schema source of truth.

## الحالة والسجلات والإيقاف

```powershell
.\foundationkit.ps1 status -Target Madar
.\foundationkit.ps1 logs -Target Madar
.\foundationkit.ps1 stop -Target Madar
```

الحالة تعرض:

```text
STOPPED or unreachable
LIVE but NOT READY
READY
```

الإيقاف لا يحذف volume. اسم Compose المحلي:

```text
madar-product
```

الحذف اليدوي المتعمد فقط:

```powershell
docker compose --project-name madar-product -f deploy/madar-compose.yml down --volumes --remove-orphans
```

قد يحتاج Compose قيم البيئة المطلوبة عند تفسير الملف؛ المسار العادي هو استخدام المشغّل. `reset -Target Madar` غير معروض عمدًا في المدير الموحد.

## التحقق الآلي

التحقق الحالي يشمل:

```text
Build/Test
    ↓
EF migration + SQL startup
    ↓
Readiness
    ↓
Auth + CSRF
    ↓
Critical CI-only SLA case
    ↓
Target snapshot
    ↓
Wait beyond short test target
    ↓
Authorized bounded evaluation
    ↓
Persist breach + escalation + audit exactly once
    ↓
Second evaluation proves idempotency
    ↓
Normal case assignment/lifecycle proves SLA met
    ↓
Container/Trivy/SARIF/CodeQL
```

في CI فقط تُمرر سياسة قصيرة جدًا لإثبات السلوك بسرعة، منها Critical = ثانيتان. هذه **قيمة اختبار آلي فقط** وليست قيمة المنتج.

الاختبارات البرمجية تغطي أيضًا:

- عدم وجود SLA عندما تكون السياسة معطلة؛
- snapshot للهدف؛
- رفض target غير مستقبلي؛
- exact-target boundary؛
- late resolve breach؛
- idempotency؛
- صلاحية Supervisor/Administrator ومنع Operator من global evaluation؛
- bounded batch + `hasMore`.

## Atlas / Pages

Atlas يقرأ مسارات Madar الحقيقية من:

```text
apps/Madar/Madar.Client/Pages
```

والمسارات الحالية تبقى:

```text
/
/login
/cases
/cases/{CaseId:guid}
```

SLA أضيف داخل الصفحات الحالية، لذلك لا توجد route UI جديدة. Swagger يوثق `EvaluateMadarCaseSla` ضمن API الحالي.

## ما الذي لا تثبته هذه الأدلة؟

لا تثبت تلقائيًا:

- Production Approval؛
- Segregation-of-Duties مستقل؛
- ISO/IEC 27001 certification؛
- أن مدد SLA التجريبية مناسبة للأعمال؛
- production scheduler/provider؛
- production KMS/Vault أو network topology؛
- RPO/RTO؛
- penetration/load acceptance؛
- retention/privacy policy قانونية.

هذه قرارات وأدلة تشغيلية وتنظيمية خارج ما يستطيع المستودع إثباته وحده.

## قاعدة التطوير التالية

وجود command يحتاج تشغيلًا دوريًا لا يعني تلقائيًا إنشاء `FoundationKit.Jobs`. بعد إثبات SLA business command داخل Madar، يأتي قرار منفصل عن scheduler. وإذا ظهرت لاحقًا حاجة عامة حقيقية من أكثر من consumer، عندها فقط يُقيّم استخراج capability قابلة لإعادة الاستخدام.
