# تشغيل FoundationKit وWorkbench ومنصة أثَر على Visual Studio 2026

هذا الدليل مخصص لتشغيل الحل كاملًا على Windows مع SQL Server محلي. لا تحتاج إلى تشغيل مشروع Blazor منفصل؛ مشروع الـAPI يستضيف ملفات Blazor WebAssembly ويعملان من عنوان واحد.

## 1. المتطلبات

ثبّت أو تأكد من وجود:

- Visual Studio 2026.
- Workload: **ASP.NET and web development**.
- .NET 8 SDK، وفق `global.json` داخل المستودع.
- SQL Server 2025 أو SQL Server Express.
- SQL Server Management Studio لفحص قواعد البيانات.
- Git.

على جهاز يستخدم الـDefault Instance مثل `MSSQLSERVER` يكفي غالبًا:

```text
Server=.
```

أما SQL Express فيستخدم عادة:

```text
Server=.\SQLEXPRESS
```

## 2. تنزيل المستودع

من Terminal أو PowerShell:

```powershell
git clone https://github.com/a2sn2/foundationkit-dotnet.git
cd foundationkit-dotnet
git pull origin main
```

ثم افتح:

```text
FoundationKit.sln
```

انتظر حتى ينتهي Visual Studio من استعادة NuGet packages. إذا لم يبدأ تلقائيًا:

```text
Solution Explorer → Right click Solution → Restore NuGet Packages
```

## 3. تأكد من SQL Server

من `SQL Server Configuration Manager` أو `Services` تأكد أن الخدمة المناسبة تعمل:

```text
SQL Server (MSSQLSERVER)   Running
```

أو:

```text
SQL Server (SQLEXPRESS)    Running
```

اختبر الاتصال من SSMS باستخدام Windows Authentication قبل تشغيل الحل.

---

# تشغيل Workbench

Workbench هو المثال المعماري الذي يشرح مسار المستخدم ومسار الإدارة.

## 4. إعداد Connection String لـWorkbench

من Solution Explorer:

```text
samples
  → FoundationKit.Workbench.Api
  → Right click
  → Manage User Secrets
```

ضع:

```json
{
  "ConnectionStrings": {
    "Workbench": "Server=.;Database=FoundationKitWorkbench;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True"
  }
}
```

لـSQL Express استخدم:

```json
{
  "ConnectionStrings": {
    "Workbench": "Server=.\\SQLEXPRESS;Database=FoundationKitWorkbench;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True"
  }
}
```

لا تضع كلمات مرور أو بيانات اتصال حساسة داخل `appsettings.json` في Git.

## 5. اختيار Startup Project

```text
Right click FoundationKit.Workbench.Api
→ Set as Startup Project
```

ثم:

```text
Build → Rebuild Solution
```

بعد نجاح البناء اضغط:

```text
F5
```

يفتح المتصفح على:

```text
http://localhost:5057
```

## 6. اختبار Workbench

افتح بالتسلسل:

```text
http://localhost:5057/
http://localhost:5057/user
http://localhost:5057/admin
http://localhost:5057/swagger
http://localhost:5057/api/health
```

السيناريو الكامل:

1. افتح `/user`.
2. أدخل بيانات مشروع تجريبي.
3. اختر قدرات من FoundationKit.
4. أرسل الطلب.
5. افتح `/admin`.
6. اختر الطلب.
7. اكتب اسم المراجع وملاحظاته.
8. اعتمد أو ارفض.
9. ارجع إلى `/user` واضغط تحديث الحالة.

أول تشغيل يطبق EF Core migrations تلقائيًا وينشئ:

```text
Database: FoundationKitWorkbench
Tables: BuildBriefs, AdminReviews, __EFMigrationsHistory
```

---

# تشغيل منصة أثَر

أثَر هو المنتج العربي الكامل: Identity، مستخدم، إدارة، CSRF، تدقيق، ومبادرات.

## 7. إعداد User Secrets لمنصة أثَر

من Solution Explorer:

```text
examples
  → Athar
  → Athar.Api
  → Right click
  → Manage User Secrets
```

ضع إعدادات محلية مثل:

```json
{
  "ConnectionStrings": {
    "Athar": "Server=.;Database=Athar;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True"
  },
  "AdminSeed": {
    "Enabled": true,
    "Email": "admin@athar.local",
    "DisplayName": "مسؤول منصة أثر",
    "Password": "AtharLocal!2026Aa"
  }
}
```

كلمة المرور السابقة مثال محلي فقط. غيّرها ولا تستخدمها في أي بيئة حقيقية.

لـSQL Express:

```json
{
  "ConnectionStrings": {
    "Athar": "Server=.\\SQLEXPRESS;Database=Athar;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True"
  },
  "AdminSeed": {
    "Enabled": true,
    "Email": "admin@athar.local",
    "DisplayName": "مسؤول منصة أثر",
    "Password": "AtharLocal!2026Aa"
  }
}
```

## 8. اختيار Startup Project

```text
Right click Athar.Api
→ Set as Startup Project
```

ثم:

```text
Build → Rebuild Solution
F5
```

يفتح:

```text
http://localhost:5068
```

مشروع `Athar.Api` يستضيف `Athar.Client` تلقائيًا؛ لا تجعل `Athar.Client` Startup Project مستقلًا عند تشغيل المسار الكامل.

## 9. اختبار منصة أثَر كمستخدم

1. افتح:

```text
http://localhost:5068/account
```

2. أنشئ مستخدمًا جديدًا ببريد مختلف عن بريد الإدارة.
3. بعد الدخول افتح:

```text
http://localhost:5068/initiatives
```

4. أنشئ مبادرة.
5. تأكد أنها ظهرت بحالة **قيد المراجعة**.

## 10. اختبار منصة أثَر كمسؤول

1. سجّل خروج المستخدم.
2. سجّل الدخول بـ:

```text
Email: admin@athar.local
Password: القيمة الموجودة في User Secrets
```

3. افتح:

```text
http://localhost:5068/admin
```

4. اختر المبادرة.
5. راجع الوصف والميزانية والمستفيدين.
6. أضف ملاحظات.
7. اختر اعتماد أو رفض.
8. سجّل الخروج.
9. ادخل بحساب المستخدم.
10. افتح `/initiatives` وتأكد من ظهور القرار وملاحظات الإدارة.

## 11. المسارات المهمة في أثَر

```text
/                     الصفحة الرئيسية
/account              التسجيل والدخول
/initiatives          مساحة المستخدم ومبادراته
/admin                مركز قرار الإدارة
/swagger              توثيق API
/health/live           هل عملية التطبيق حية؟
/health/ready          هل التطبيق وقاعدة البيانات جاهزان؟
```

أول تشغيل ينشئ قاعدة:

```text
Database: Athar
```

وتشمل جداول Identity وجداول المنتج مثل المبادرات والمراجعات وسجل التدقيق.

---

# تشغيل المشروعين معًا

يمكن تشغيل Workbench وأثَر في وقت واحد لأن المنافذ مختلفة:

```text
Workbench: http://localhost:5057
Athar:     http://localhost:5068
```

من Visual Studio:

```text
Right click Solution
→ Configure Startup Projects
→ Multiple startup projects
```

اضبط:

```text
FoundationKit.Workbench.Api   Start
Athar.Api                     Start
```

ولا تضبط مشاريع Client على Start؛ كل API يستضيف عميله.

---

# فحص قاعدة البيانات في SSMS

بعد التشغيل:

1. افتح SSMS.
2. اتصل بنفس Server المستخدم في User Secrets.
3. اضغط Refresh على Databases.
4. ستجد:

```text
FoundationKitWorkbench
Athar
```

5. افحص:

```text
Databases
→ <Database>
→ Tables
```

لا تنشئ الجداول يدويًا؛ المهاجرات هي المصدر الرسمي لبنية قاعدة البيانات.

---

# أوامر التحقق من Terminal

من جذر المستودع:

```powershell
dotnet restore FoundationKit.sln
dotnet build FoundationKit.sln --configuration Release --no-restore
dotnet test FoundationKit.sln --configuration Release --no-build
```

تشغيل Workbench:

```powershell
dotnet run --project .\samples\FoundationKit.Workbench\FoundationKit.Workbench.Api.csproj
```

تشغيل أثَر:

```powershell
dotnet run --project .\examples\Athar\Athar.Api\Athar.Api.csproj
```

## مشاكل شائعة

### Login failed أو Server not found

- تأكد أن SQL Server service تعمل.
- جرّب `Server=.` للـDefault Instance.
- جرّب `Server=.\SQLEXPRESS` للـExpress.
- تأكد أن اسم السيرفر نفسه يعمل في SSMS.

### قاعدة البيانات لم تظهر

- راجع Output في Visual Studio.
- تأكد أن Connection String ليست فارغة.
- تأكد أن حساب Windows لديه صلاحية إنشاء قاعدة محلية.
- أوقف التطبيق، صحح User Secrets، ثم شغله مرة أخرى.

### المتصفح يعرض 401 أو 403

- `401`: المستخدم غير مسجل الدخول.
- `403`: المستخدم دخل لكنه لا يملك دور Administrator.
- تأكد أن AdminSeed مفعّل في أول تشغيل وأن بريد المسؤول صحيح.

### صفحة Blazor لا تحمل

- اجعل مشروع الـAPI هو Startup Project.
- نفذ Rebuild Solution.
- امسح `bin` و`obj` فقط عند الحاجة ثم Restore وRebuild.
- لا تشغّل Client وحده عند اختبار API وSQL Server.

### تغيير User Secrets لم ينعكس

- أوقف التطبيق بالكامل.
- تأكد أنك فتحت Manage User Secrets للمشروع الصحيح.
- شغّل من جديد؛ User Secrets تُقرأ في Development فقط.
