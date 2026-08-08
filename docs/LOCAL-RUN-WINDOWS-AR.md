# تشغيل FoundationKit محليًا على Windows

هذا هو الدليل الكانوني لأول تشغيل محلي على Windows. الهدف أن نختبر المستودع تدريجيًا ونفصل مشاكل الأدوات أو SQL Server أو المنافذ عن مشاكل التطبيق نفسه.

> لا تستخدم بيانات حقيقية أو حساسة أثناء الاختبار المحلي. إعدادات `.local/` وUser Secrets محلية فقط ولا تُرفع إلى Git.

## 1. ما الذي تحتاجه

الحد الأدنى لمسار Native:

- Git.
- PowerShell 5.1 أو أحدث.
- .NET 8 SDK؛ `global.json` يطلب خط .NET 8 ويقبل أحدث feature band متوافق.
- SQL Server محلي يعمل، مثل Default Instance (`MSSQLSERVER`) أو SQL Express.

اختياري:

- Visual Studio 2026 مع workload **ASP.NET and web development**.
- SSMS لفحص قاعدة البيانات.
- Docker Desktop إذا أردت تشغيل بيئة Docker بدل SQL Server المحلي.
- Python وNode.js لتشغيل كل فحوصات التحقق المحلية الإضافية.
- `sqlcmd` لعمليات النسخ الاحتياطي Native الخاصة بـAthar.

## 2. تنزيل نسخة نظيفة

```powershell
git clone https://github.com/a2sn2/foundationkit-dotnet.git
cd foundationkit-dotnet
git switch main
git pull --ff-only origin main
```

تأكد أن نسخة العمل نظيفة:

```powershell
git status --short
```

المخرجات الطبيعية لنسخة جديدة: لا شيء.

## 3. الفحص الأول قبل التشغيل

من جذر المستودع:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\foundationkit.ps1 doctor
```

ثم:

```powershell
dotnet --info
```

المهم في `doctor`:

- `git` = PASS.
- `dotnet` = PASS.
- `powershell` = PASS.
- وجود .NET 8 SDK.
- Git working tree = clean.
- Docker قد يكون موجودًا أو غير موجود؛ هو اختياري لمسار Native.

إذا فشل `doctor`، أصلح أول FAIL قبل تشغيل التطبيقات.

## 4. تحقق من SQL Server قبل التطبيق

اختبر نفس الـinstance من SSMS باستخدام Windows Authentication.

Default Instance:

```text
Server=.
```

SQL Express:

```text
Server=.\SQLEXPRESS
```

لا تنشئ الجداول يدويًا. Workbench وAthar يملكان EF Core migrations خاصة بهما، وهي مصدر الحقيقة لبنية قواعد البيانات.

## 5. أول اختبار: Workbench فقط — Native

ابدأ بـWorkbench لأنه أبسط من Athar ويختبر .NET + SQL Server + migrations + API + Blazor في مسار واحد.

```powershell
.\foundationkit.ps1 start -Target Workbench -Mode Native
```

المسار Native للمدير الموحد يستخدم افتراضيًا:

```text
Server=.;Database=FoundationKitWorkbench;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True
```

العنوان:

```text
http://localhost:5057
```

بعد التشغيل:

```powershell
.\foundationkit.ps1 status -Target Workbench -Mode Native
```

ثم جرّب:

```text
http://localhost:5057/
http://localhost:5057/user
http://localhost:5057/admin
http://localhost:5057/swagger
http://localhost:5057/api/health
```

إذا كنت تستخدم SQL Express أو instance مختلفًا، عدّل الملف المحلي الذي ينشئه المدير:

```text
.local/workbench-product.env
```

وغيّر فقط:

```text
WORKBENCH_NATIVE_CONNECTION_STRING=Server=.\SQLEXPRESS;Database=FoundationKitWorkbench;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True
```

الملف `.local/workbench-product.env` محمي محليًا ومهمل من Git. لا ترفعه للمستودع.

بعد التعديل:

```powershell
.\foundationkit.ps1 stop -Target Workbench -Mode Native
.\foundationkit.ps1 start -Target Workbench -Mode Native
```

## 6. ثاني اختبار: Athar فقط — Native

بعد نجاح Workbench:

```powershell
.\foundationkit.ps1 start -Target Athar -Mode Native
```

المدير الموحد يستخدم افتراضيًا:

```text
Server=.;Database=Athar;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True
```

العنوان في مسار المدير الموحد:

```text
http://localhost:8090
```

احصل على حساب الإدارة المحلي عند الحاجة فقط:

```powershell
.\foundationkit.ps1 credentials -Target Athar
```

ثم:

```powershell
.\foundationkit.ps1 status -Target Athar -Mode Native
```

المسارات المهمة:

```text
http://localhost:8090/
http://localhost:8090/account
http://localhost:8090/initiatives
http://localhost:8090/admin
http://localhost:8090/swagger
http://localhost:8090/health/live
http://localhost:8090/health/ready
```

إذا كان SQL Server على SQL Express، عدّل:

```text
.local/athar-product.env
```

إلى:

```text
ATHAR_NATIVE_CONNECTION_STRING=Server=.\SQLEXPRESS;Database=Athar;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True
```

ثم أوقف وأعد التشغيل.

## 7. فرق المنافذ بين المدير الموحد وVisual Studio

هناك مساران صحيحان، لكن لا تخلط بين منافذهما:

| المسار | Workbench | Athar |
|---|---:|---:|
| `foundationkit.ps1` Native | `5057` | `8090` |
| Visual Studio / `dotnet run` launch profile | `5057` | `5068` |
| Docker | `8080` | `8090` |

لذلك ظهور Athar على `8090` عند استخدام المدير الموحد ليس خطأ، وظهوره على `5068` من Visual Studio ليس خطأ أيضًا.

## 8. تشغيل المشروعين معًا — Native

بعد نجاح كل واحد منفردًا:

```powershell
.\foundationkit.ps1 start -Target All -Mode Native
```

ثم:

```powershell
.\foundationkit.ps1 status -Target All -Mode Native
```

الإيقاف مع الحفاظ على البيانات:

```powershell
.\foundationkit.ps1 stop -Target All -Mode Native
```

## 9. تشغيل Docker بدل SQL Server المحلي

إذا كان Docker Desktop جاهزًا:

```powershell
.\foundationkit.ps1 start -Target Workbench -Mode Docker
.\foundationkit.ps1 start -Target Athar -Mode Docker
```

أو:

```powershell
.\foundationkit.ps1 start -Target All -Mode Docker
```

المنافذ:

```text
Workbench: http://localhost:8080
Athar:     http://localhost:8090
```

Docker ينشئ SQL Server containers خاصة بالتطوير ولا يستخدم Windows Authentication الخاص بالـinstance المحلي.

## 10. تشغيل Visual Studio 2026

افتح:

```text
FoundationKit.sln
```

لـWorkbench اجعل:

```text
FoundationKit.Workbench.Api
```

هو Startup Project.

لـAthar اجعل:

```text
Athar.Api
```

هو Startup Project.

لا تشغّل مشاريع Blazor Client وحدها عند اختبار المسار الكامل؛ الـAPI host يقدم ملفات العميل.

الدليل التفصيلي لـVisual Studio موجود في:

```text
docs/VISUAL-STUDIO-2026-AR.md
```

## 11. فحص المستودع قبل تشخيص Runtime

قبل أن تعتبر المشكلة من التطبيق، شغّل:

```powershell
.\foundationkit.ps1 restore
.\foundationkit.ps1 build
.\foundationkit.ps1 test
.\foundationkit.ps1 verify
```

`verify` يشغل البناء والاختبارات وفحوصات الكتالوج والـPages المتاحة محليًا. CI على GitHub يبقى أوسع لأنه يشغل أيضًا Linux containers وSQL integration وSecurity Scan وCodeQL.

## 12. أوامر التشخيص عند الفشل

Workbench:

```powershell
.\foundationkit.ps1 logs -Target Workbench -Mode Native
```

Athar:

```powershell
.\foundationkit.ps1 logs -Target Athar -Mode Native
```

حالة Git:

```powershell
git status --short
git rev-parse HEAD
```

حالة .NET:

```powershell
dotnet --info
dotnet --list-sdks
```

اختبر SQL Server من SSMS بنفس اسم السيرفر الموجود في connection string.

## 13. ماذا ترسل عند ظهور مشكلة

أرسل المعلومات التالية بدون كلمات مرور أو أسرار:

1. الأمر الذي شغلته حرفيًا.
2. أول رسالة خطأ كاملة، وليس آخر سطر فقط.
3. ناتج `foundationkit.ps1 doctor`.
4. ناتج `dotnet --info` عند وجود مشكلة SDK/build.
5. اسم SQL Server المستخدم فقط، مثل `.` أو `.\SQLEXPRESS`.
6. هل الاتصال بنفس الاسم ينجح من SSMS أم لا.
7. ناتج `foundationkit.ps1 logs -Target <Athar|Workbench> -Mode Native` عند مشكلة Runtime.
8. ناتج `git rev-parse HEAD` حتى نتأكد أننا نشخّص نفس النسخة.

لا ترسل محتوى `.local/*.env` ولا User Secrets ولا كلمات المرور.

## 14. تنظيف Runtime المحلي عند الحاجة

الإيقاف العادي يحافظ على البيانات:

```powershell
.\foundationkit.ps1 stop -Target All -Mode Native
```

`reset -Force` مخصص لإعادة ملفات تشغيل المدير المحلي. في Native لا يحذف قاعدة SQL Server تلقائيًا؛ هذا متعمد لحماية البيانات المحلية.

```powershell
.\foundationkit.ps1 reset -Target Workbench -Mode Native -Force
.\foundationkit.ps1 reset -Target Athar -Mode Native -Force
```

إذا احتجت حذف قواعد البيانات نفسها، افعل ذلك يدويًا وبوعي من SSMS بعد التأكد أنها قواعد تطوير محلية فقط.
