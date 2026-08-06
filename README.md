# FoundationKit for .NET

[![FoundationKit CI](https://github.com/a2sn2/foundationkit-dotnet/actions/workflows/ci.yml/badge.svg)](https://github.com/a2sn2/foundationkit-dotnet/actions/workflows/ci.yml)
[![FoundationKit Atlas](https://github.com/a2sn2/foundationkit-dotnet/actions/workflows/pages.yml/badge.svg)](https://a2sn2.github.io/foundationkit-dotnet/)
[![Windows Launcher](https://github.com/a2sn2/foundationkit-dotnet/actions/workflows/windows-launcher-check.yml/badge.svg)](https://github.com/a2sn2/foundationkit-dotnet/actions/workflows/windows-launcher-check.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)
[![Target: .NET 8](https://img.shields.io/badge/.NET-8.0-512BD4)](https://dotnet.microsoft.com/)

**FoundationKit** هو أساس هندسي قابل لإعادة الاستخدام لبناء تطبيقات .NET بنمط Clean Architecture وDomain-Driven Design، مع مشروع معماري توضيحي، ومنتج عربي كامل، واختبارات، وتوثيق، وتشغيل محلي وDocker، وبوابة GitHub Pages.

المستودع لا يقدّم قالبًا نظريًا فقط؛ بل يثبت دورة كاملة:

```text
Reusable Core
    ↓
Architecture Workbench
    ↓
Complete Product Reference
    ↓
Automated Tests and CI
    ↓
Local / Docker / Browser Demo
    ↓
Production Readiness Gate
```

الإصدار الحالي للحزم القابلة لإعادة الاستخدام: `0.1.0`.

---

## المحتويات

1. [ما هو هذا المستودع؟](#ما-هو-هذا-المستودع)
2. [كيف تطوّر المشروع؟](#كيف-تطوّر-المشروع)
3. [الصورة المعمارية الكاملة](#الصورة-المعمارية-الكاملة)
4. [مكوّنات المستودع](#مكوّنات-المستودع)
5. [FoundationKit Core](#1--foundationkit-core)
6. [Workbench](#2--workbench)
7. [منصة أثَر](#3--منصة-أثر)
8. [Atlas وGitHub Pages](#4--atlas-وgithub-pages)
9. [التشغيل بسكربت واحد](#التشغيل-بسكربت-واحد)
10. [التشغيل المحلي وDocker](#أوضاع-التشغيل)
11. [الإتاحة داخل الشبكة والإنترنت](#مشاركة-المنتج)
12. [الأسرار والسجلات والنسخ الاحتياطي](#الأسرار-والسجلات-والبيانات)
13. [Visual Studio 2026](#visual-studio-2026)
14. [Build وTest وVerify وPack](#بناء-واختبار-المستودع)
15. [GitHub Actions](#github-actions)
16. [الانتقال إلى Production](#من-المرجع-إلى-production)
17. [استكشاف الأخطاء](#استكشاف-الأخطاء)
18. [الروابط المهمة](#الروابط-المهمة)

---

# ما هو هذا المستودع؟

المستودع مقسّم عمدًا إلى خمس طبقات مختلفة من الإثبات:

```text
src/FoundationKit.*          حزم عامة قابلة لإعادة الاستخدام
samples/                     أمثلة معمارية وتعليمية
examples/Athar/              منتج عربي Full Stack كامل
site/                        بوابة ثابتة وتفاعلية على GitHub Pages
apps/                        المكان المحجوز للمنتجات الفعلية القادمة
```

الفرق بينها مهم:

| الجزء | الغرض | هل يملك قواعد منتج؟ | هل يحتاج قاعدة بيانات؟ |
|---|---|---:|---:|
| `src/FoundationKit.*` | كور عام | لا | لا |
| `samples/Workbench` | شرح المسارات المعمارية | قواعد تجريبية | نعم |
| `examples/Athar` | منتج مرجعي كامل | نعم | نعم |
| `site/` | توثيق ومعاينة ثابتة | لا | لا |
| `apps/` | منتجات مستقبلية حقيقية | حسب المنتج | حسب المنتج |

> FoundationKit هو **Production Baseline** وليس وعدًا بأن أي تطبيق يصبح Production بمجرد نسخ الكود. جاهزية الإنتاج تعتمد أيضًا على البيئة، والأمن، والنسخ الاحتياطي، والمراقبة، والامتثال، واختبارات القبول.

---

# كيف تطوّر المشروع؟

## المرحلة الأولى — بناء الكور العام

بدأ المشروع بفصل العناصر المشتركة التي يحتاجها أكثر من منتج:

- الكيانات والجذور التجميعية وكائنات القيمة.
- أحداث النطاق.
- الأوامر والاستعلامات وحالات الاستخدام.
- النتائج المصنفة والتحقق والترقيم.
- المستودعات والمواصفات ووحدة العمل.
- تكامل EF Core العام.
- Problem Details وCorrelation ID ورؤوس الأمان.
- عميل Blazor typed وحالة الواجهة وViewModels.

الهدف من هذه المرحلة كان منع تكرار نفس البنية في كل مشروع جديد، من دون إدخال قواعد نشاط محددة داخل الكور.

## المرحلة الثانية — Workbench

أُضيف Workbench لإثبات أن الكور لا يعمل كمكتبات منفصلة فقط، بل يربط مسارين كاملين:

```text
مستخدم يرسل طلبًا
        ↓
الطلب يُحفظ في SQL Server
        ↓
الإدارة تراجع الطلب
        ↓
اعتماد أو رفض
        ↓
المستخدم يرى الحالة الجديدة
```

Workbench هو مختبر معماري وتعليمي، وليس المنتج النهائي.

## المرحلة الثالثة — منصة أثَر

أُنشئت منصة **أثَر** كمنتج عربي متكامل يضيف ما لا ينبغي أن يملكه الكور العام:

- قواعد المبادرات المجتمعية.
- ASP.NET Core Identity.
- المستخدم والإدارة والأدوار.
- Cookie Authentication.
- Anti-CSRF.
- Rate Limiting.
- Audit Trail.
- SQL Server migrations.
- واجهة عربية RTL.
- دورة مراجعة واعتماد ورفض.

## المرحلة الرابعة — Atlas وGitHub Pages

أُنشئت بوابة عربية ثابتة تشرح كل أجزاء المستودع، وتربط الصفحات الفعلية بمصادرها، وتعرض نسخة Browser Demo من أثَر.

## المرحلة الخامسة — المنتج التجريبي المجاني

أُضيفت طريقتان مجانيتان للعرض:

1. تشغيل Full Stack الحقيقي على جهاز المطوّر.
2. مشاركة Browser Demo ثابتة من GitHub Pages.

ثم أُضيف Cloudflare Quick Tunnel لإعطاء رابط HTTPS مؤقت يصل إلى التطبيق الحقيقي الذي يعمل على الجهاز.

## المرحلة السادسة — مدير موحّد للمستودع

أصبح الملف التالي هو نقطة التحكم الرئيسية:

```text
foundationkit.ps1
```

وهو يدير:

- أثَر.
- Workbench.
- Native وDocker.
- التشغيل والإيقاف وإعادة التشغيل.
- الحالة والسجلات والشبكة والنفق.
- Build وTest وVerify وPack.
- فحص الـProduction Baseline.

---

# الصورة المعمارية الكاملة

```text
                                  FOUNDATIONKIT CORE

     Domain         Application       Infrastructure       WebApi       Blazor
       │                 │                   │                │             │
       └─────────────────┴───────────────────┴────────────────┴─────────────┘
                                         │
                   ┌─────────────────────┴─────────────────────┐
                   │                                           │
           WORKBENCH REFERENCE                         ATHAR PRODUCT
       User + Admin vertical slices            Arabic product with Identity
                   │                                           │
           SQL-backed workflow                 User UI ↔ API ↔ Admin UI
                   │                                           │
                   └─────────────────────┬─────────────────────┘
                                         │
                                  SQL Server / EF Core
                                         │
                       Tests · CI · Docker · Pages · Operations
```

## اتجاه الاعتماد

```text
Domain
  ↑
Application
  ↑
Infrastructure / API / UI
```

القواعد الأساسية:

- `Domain` لا يعرف SQL Server أو HTTP أو Blazor.
- `Application` يعرف حالات الاستخدام والعقود، ولا يعرف تفاصيل الاستضافة.
- `Infrastructure` ينفّذ التخزين والتكاملات.
- `Api` يربط HTTP والمصادقة والتهيئة.
- `Client` يقدّم تجربة المستخدم ويتواصل مع API عبر عقود واضحة.

---

# مكوّنات المستودع

```text
foundationkit-dotnet/
├── src/                          FoundationKit reusable packages
├── samples/                      Workbench architecture sample
├── examples/Athar/               Complete Arabic reference product
├── apps/                         Reserved for real products
├── tests/                        Core, Workbench, and Athar tests
├── tools/                        Catalog generator and repository tooling
├── catalog/                      Canonical capability catalog
├── docs/                         Architecture and operations documentation
├── deploy/                       Docker Compose and published launchers
├── postman/                      Executable API collections
├── scripts/                      Internal verification and runtime scripts
├── site/                         GitHub Pages Atlas and Athar browser demo
├── .github/workflows/            CI, Pages, package, and Windows checks
├── foundationkit.ps1             Unified repository manager
└── FoundationKit.sln             Visual Studio solution
```

## مشاريع الـSolution

```text
FoundationKit.Domain
FoundationKit.Application
FoundationKit.Infrastructure
FoundationKit.WebApi
FoundationKit.Blazor
FoundationKit.Tests

FoundationKit.Workbench.Api
FoundationKit.Workbench.Contracts
FoundationKit.Workbench.Client
FoundationKit.Workbench.Tests

Athar.Domain
Athar.Application
Athar.Infrastructure
Athar.Contracts
Athar.Api
Athar.Client
Athar.Tests

FoundationKit.CatalogGenerator
```

---

# 1 — FoundationKit Core

## `FoundationKit.Domain`

مسؤول عن قلب النطاق:

- `Entity<TId>`.
- `AggregateRoot<TId>`.
- Value Objects.
- Domain Events.
- المساواة والهوية وقواعد النطاق العامة.

لا يعتمد على قاعدة بيانات أو API أو UI.

## `FoundationKit.Application`

مسؤول عن حالات الاستخدام:

- Commands وQueries.
- Use Cases.
- `Result<T>` وتصنيف الأخطاء.
- Validation.
- Pagination.
- Repository وUnit of Work abstractions.
- DTO bases مثل `EntityDto<TId>` و`AuditedEntityDto<TId>`.

## `FoundationKit.Infrastructure`

مسؤول عن التنفيذ التقني العام:

- EF Core repository adapter.
- Specification evaluator.
- Unit of Work.
- Domain event dispatching.
- Interceptors.

الكور لا يفرض مزود قاعدة بيانات على المنتج؛ المنتج نفسه يختار SQL Server ويملك migrations.

## `FoundationKit.WebApi`

مسؤول عن السلوك المشترك في HTTP:

- تحويل Result إلى HTTP response.
- RFC 7807 Problem Details.
- Correlation ID.
- Security headers.
- Pipeline extensions.

## `FoundationKit.Blazor`

مسؤول عن العناصر المشتركة في Blazor:

- Typed API client base.
- `ApiResult<T>`.
- تحليل أخطاء API والشبكة.
- `AsyncState<T>`.
- `ViewModelBase`.
- `ListViewModel<T>`.

## ما الذي لا يملكه الكور؟

الكور لا يملك:

- قاعدة بيانات منتج.
- migrations خاصة بمنتج.
- الأدوار والصلاحيات الفعلية.
- تصميم الواجهة.
- قواعد المبادرات أو الطلبات.
- إعدادات الاستضافة.
- أسرار البيئة.

---

# 2 — Workbench

Workbench يوضّح مسارين رأسيين متصلين:

```text
User Full Stack
    ↓ submitted request
Admin Full Stack
    ↓ approved / rejected
User reads updated status
```

## المشاريع

```text
samples/FoundationKit.Workbench/
samples/FoundationKit.Workbench.Contracts/
samples/FoundationKit.Workbench.Client/
tests/FoundationKit.Workbench.Tests/
```

## المسارات

```text
/                    الخريطة الرئيسية
/user                بوابة المستخدم
/admin               لوحة الإدارة
/swagger             Swagger
/api/health          فحص الصحة
```

## المنافذ

| الوضع | الرابط |
|---|---|
| Native عبر السكربت | `http://localhost:5057` |
| Docker | `http://localhost:8080` |
| Visual Studio launch profile | `http://localhost:5057` |

## قاعدة البيانات

```text
FoundationKitWorkbench
```

Native يستخدم SQL Server المحلي وWindows Authentication افتراضيًا.

Docker يستخدم SQL Server داخل Container مستقل.

## وثائق Workbench

- [Dual Full-Stack Architecture](docs/DUAL-FULL-STACK.md)
- [Workbench Operations](docs/WORKBENCH.md)
- [Technical Architecture](docs/ARCHITECTURE.md)

---

# 3 — منصة أثَر

**أثَر** منصة عربية لإدارة المبادرات المجتمعية.

## دورة المستخدم

```text
تسجيل حساب / تسجيل دخول
        ↓
إنشاء مبادرة
        ↓
التحقق من البيانات
        ↓
حفظ المبادرة في SQL Server
        ↓
ظهورها في قائمة المستخدم والإدارة
        ↓
متابعة القرار
```

## دورة الإدارة

```text
دخول المسؤول
        ↓
قائمة المبادرات
        ↓
فتح التفاصيل
        ↓
إضافة ملاحظة القرار
        ↓
اعتماد أو رفض
        ↓
تغيير حالة المبادرة
        ↓
إنشاء InitiativeReview وAuditEntry
        ↓
ظهور القرار للمستخدم
```

## المشاريع

```text
examples/Athar/
├── Athar.Domain
├── Athar.Application
├── Athar.Infrastructure
├── Athar.Contracts
├── Athar.Api
└── Athar.Client

tests/Athar.Tests
postman/Athar.Api.postman_collection.json
deploy/athar-compose.yml
```

## User Full Stack

```text
Arabic Blazor UI
    ↓
InitiativesViewModel
    ↓
AtharApiClient
    ↓
CreateInitiativeRequest
    ↓
POST /api/v1/initiatives
    ↓
InitiativeManager
    ↓
Initiative Aggregate
    ↓
EF Core + SQL Server
```

## Admin Full Stack

```text
Arabic Admin Dashboard
    ↓
AdminViewModel
    ↓
AtharApiClient
    ↓
GET /api/v1/admin/initiatives
POST /api/v1/admin/initiatives/{id}/review
    ↓
InitiativeManager
    ↓
InitiativeReview + AuditEntry + status transition
    ↓
SQL Server
```

## الأمن والتشغيل الموجودان في المرجع

- ASP.NET Core Identity.
- Cookie Authentication وHttpOnly.
- User وAdministrator roles.
- Password policy وقفل المحاولات.
- Anti-CSRF لكل عملية كتابة.
- Rate Limiting للدخول والكتابة.
- DTO وDomain validation.
- Idempotency عبر `ClientRequestId`.
- Optimistic Concurrency عبر `RowVersion`.
- Audit Trail.
- SQL Server migrations.
- Startup migration retry.
- Live وReady health endpoints.
- Swagger وPostman.
- Docker topology.
- Unit tests وSQL-backed smoke tests.
- Configuration وUser Secrets وEnvironment Variables للأسرار.

## مسارات أثَر

```text
/                      الرئيسية
/account               الحساب والتسجيل والدخول
/initiatives           مبادرات المستخدم
/admin                 لوحة الإدارة
/swagger               Swagger UI
/health/live           فحص حياة العملية
/health/ready          فحص الجاهزية وقاعدة البيانات
```

## المنافذ

| الوضع | الرابط |
|---|---|
| السكربت الموحد Native أو Docker | `http://localhost:8090` |
| Visual Studio | `http://localhost:5068` |

---

# 4 — Atlas وGitHub Pages

## FoundationKit Atlas

**https://a2sn2.github.io/foundationkit-dotnet/**

بوابة عربية ثابتة تشرح:

- الحزم العامة.
- Workbench.
- أثَر.
- الصفحات والمسارات.
- API وSwagger وPostman.
- الاختبارات وCI.
- Docker والتشغيل.
- الوثائق وبوابات Production.

تتم مطابقة المسارات الموجودة في Atlas مع Razor source عبر:

```text
scripts/verify-pages.py
```

إضافة صفحة `@page` جديدة من دون توثيقها تؤدي إلى فشل CI.

## Athar Browser Demo

**https://a2sn2.github.io/foundationkit-dotnet/athar-demo/**

هذه نسخة تفاعلية ثابتة تعمل بالكامل داخل المتصفح باستخدام `localStorage`.

تحتوي على:

- إنشاء مبادرة.
- لوحة إدارة.
- اعتماد ورفض.
- ملاحظات القرار.
- إحصاءات.
- إعادة ضبط التجربة.

ولا تحتوي على:

- ASP.NET Core API حقيقي.
- SQL Server.
- Identity حقيقي.
- Cookies أو Audit على الخادم.
- بيانات مشتركة بين الزوار.

> GitHub Pages للشرح والمعاينة فقط، وليس Backend دائمًا.

---

# التشغيل بسكربت واحد

المدير الرسمي للمستودع هو:

```text
foundationkit.ps1
```

بسبب سياسة Windows PowerShell، الصيغة الأكثر ثباتًا هي:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\foundationkit.ps1 help
```

هذا التجاوز ينطبق على العملية الحالية فقط ولا يغيّر Execution Policy للنظام.

عندما تسمح سياسة جهازك بتشغيل الملفات مباشرة يمكن استخدام:

```powershell
.\foundationkit.ps1 help
```

## البداية السريعة

```powershell
# فحص الجهاز والمستودع
powershell -NoProfile -ExecutionPolicy Bypass -File .\foundationkit.ps1 doctor

# تشغيل أثَر تلقائيًا
powershell -NoProfile -ExecutionPolicy Bypass -File .\foundationkit.ps1 start -Target Athar -Mode Auto

# معرفة الحالة
powershell -NoProfile -ExecutionPolicy Bypass -File .\foundationkit.ps1 status -Target Athar

# إيقاف أثَر مع الاحتفاظ بالبيانات
powershell -NoProfile -ExecutionPolicy Bypass -File .\foundationkit.ps1 stop -Target Athar
```

الملفات التالية ما زالت موجودة للتوافق والضغط المزدوج، لكنها تحوّل التنفيذ إلى السكربت الموحد:

```text
START-ATHAR.cmd
STOP-ATHAR.cmd
EXPOSE-ATHAR.cmd
```

---

# مرجع أوامر السكربت

## أوامر دورة حياة المنتجات

| الأمر | الوظيفة |
|---|---|
| `start` | تشغيل Athar أو Workbench أو الاثنين |
| `stop` | الإيقاف مع الاحتفاظ بالبيانات |
| `restart` | إيقاف ثم تشغيل |
| `status` | عرض Mode والعملية والحاويات وHealth |
| `open` | فتح التطبيق في المتصفح |
| `logs` | عرض آخر سجلات Native أو Docker |
| `lan` | إظهار روابط الأجهزة داخل نفس الشبكة |
| `expose` | إنشاء رابط HTTPS مؤقت لأثَر |
| `credentials` | عرض حساب مسؤول أثَر المحلي |
| `backup` | إنشاء نسخة احتياطية لقاعدة أثَر |
| `reset` | تنظيف البيئة؛ يحتاج `-Force` |

## أوامر المستودع

| الأمر | الوظيفة |
|---|---|
| `doctor` | فحص Git و.NET والأدوات والحالة |
| `restore` | NuGet restore للحل كاملًا |
| `build` | Restore ثم Build |
| `test` | Restore ثم Build ثم Test |
| `verify` | Test + Catalog + Pages + JavaScript + Git checks |
| `pack` | إنشاء حزم NuGet الخمس |
| `production-check` | فحص الـBaseline الآلي وعرض البوابات الخارجية |

## Targets

```text
-Target Athar
-Target Workbench
-Target All
-Target Repository
```

`Repository` مخصص للأوامر مثل Build وVerify؛ أوامر المستودع لا تحتاج Target عمليًا.

## Modes

```text
-Mode Auto
-Mode Native
-Mode Docker
```

### Auto

```text
Docker جاهز؟
    نعم → Docker
    لا  → Native باستخدام .NET وSQL Server المحلي
```

### Native

يتطلب:

- .NET 8 SDK.
- SQL Server محلي.
- Windows Authentication الافتراضي.

### Docker

يتطلب:

- Docker Desktop.
- Docker Compose.

ويشغّل التطبيق وSQL Server داخل Containers منفصلة.

---

# أمثلة تشغيل مرنة

## تشغيل أثَر

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\foundationkit.ps1 start -Target Athar -Mode Auto
```

## إجبار Native

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\foundationkit.ps1 start -Target Athar -Mode Native
```

## إجبار Docker

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\foundationkit.ps1 start -Target Athar -Mode Docker
```

## تشغيل Workbench

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\foundationkit.ps1 start -Target Workbench -Mode Auto
```

## تشغيل الاثنين

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\foundationkit.ps1 start -Target All -Mode Native
```

## حالة الاثنين

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\foundationkit.ps1 status -Target All
```

## إعادة التشغيل

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\foundationkit.ps1 restart -Target Athar
```

## عرض السجلات

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\foundationkit.ps1 logs -Target Athar
```

## الإيقاف الكامل

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\foundationkit.ps1 stop -Target All
```

> `stop` يحافظ على قواعد البيانات. استخدم `reset -Force` فقط عندما تقصد تنظيف البيئة.

---

# أوضاع التشغيل

## Native على Windows

في Native يقوم السكربت بـ:

1. التحقق من .NET SDK.
2. إنشاء إعدادات محلية داخل `.local/`.
3. تنفيذ `dotnet publish -c Release`.
4. تشغيل التطبيق كعملية مستقلة.
5. تمرير Connection String وبيانات Seed عبر Environment Variables.
6. تطبيق migrations عند بدء التطبيق المرجعي.
7. انتظار Health endpoint.
8. حفظ PID والسجلات.
9. فتح المتصفح.

### قواعد البيانات الافتراضية

```text
Athar
FoundationKitWorkbench
```

### Connection Strings الافتراضية

```text
Server=.;Database=Athar;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True

Server=.;Database=FoundationKitWorkbench;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True
```

يمكن تعديلها في الملفات المحلية داخل `.local/` من دون رفع الأسرار إلى Git.

## Docker

### أثَر

```text
deploy/athar-compose.yml
```

المنافذ:

```text
Athar          8090
SQL Server     14334
```

### Workbench

```text
deploy/docker-compose.yml
```

المنافذ:

```text
Workbench      8080
SQL Server     14333
```

Docker Volumes تحافظ على البيانات عند `stop` أو `down` العادي.

---

# مشاركة المنتج

## داخل نفس Wi-Fi أو LAN

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\foundationkit.ps1 lan -Target Athar
```

سيظهر رابط مشابه:

```text
http://192.168.1.20:8090
```

قد تحتاج إلى السماح للمنفذ في Windows Firewall.

## رابط HTTPS مؤقت للتطبيق الحقيقي

بعد تشغيل أثَر:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\foundationkit.ps1 expose -Target Athar
```

أو:

```text
EXPOSE-ATHAR.cmd
```

المتطلب:

```powershell
winget install --id Cloudflare.cloudflared --exact --source winget
```

سيظهر رابط مؤقت مثل:

```text
https://random-name.trycloudflare.com
```

شروط استمرار الرابط:

- الجهاز شغال.
- أثَر شغال.
- الإنترنت متصل.
- نافذة النفق مفتوحة.

إغلاق النافذة أو `Ctrl+C` يوقف الرابط، والرابط يتغير في التشغيل التالي.

> Quick Tunnel مناسب للعرض التجريبي فقط ولا يملك ضمان استمرارية أو SLA.

## رابط ثابت للـBrowser Demo

```text
https://a2sn2.github.io/foundationkit-dotnet/athar-demo/
```

هذا الرابط لا يحتاج تشغيل الجهاز، لكنه Demo ثابت وليس النظام الحقيقي.

---

# الأسرار والسجلات والبيانات

## الملفات المحلية

```text
.local/athar-product.env
.local/workbench-product.env
.local/athar-product.mode
.local/workbench-product.mode
.local/athar-native.pid
.local/workbench-native.pid
.local/logs/
.local/backups/
```

`.local/` مستبعد من Git.

## حساب مسؤول أثَر المحلي

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\foundationkit.ps1 credentials -Target Athar
```

لا تشارك حساب المسؤول مع المستخدمين التجريبيين.

## السجلات

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\foundationkit.ps1 logs -Target All
```

Native logs:

```text
.local/logs/athar-native.out.log
.local/logs/athar-native.err.log
.local/logs/workbench-native.out.log
.local/logs/workbench-native.err.log
```

Docker logs تُقرأ من Docker Compose مباشرة.

## النسخ الاحتياطي

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\foundationkit.ps1 backup -Target Athar
```

المسار:

```text
.local/backups/
```

في Native قد تحتاج إلى `sqlcmd`، وفي Docker يتم تنفيذ النسخ من داخل SQL Server Container.

## Reset

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\foundationkit.ps1 reset -Target Athar -Force
```

قبل Reset:

1. أوقف الرابط العام.
2. خذ Backup.
3. تأكد أنك لا تحتاج البيانات.
4. استخدم `-Force` فقط بقصد.

في Workbench Native ينظف السكربت ملفات التشغيل ويحافظ على قاعدة SQL المحلية، بينما Docker Reset يزيل Volume الخاص به.

---

# Visual Studio 2026

افتح:

```text
FoundationKit.sln
```

Startup projects:

```text
FoundationKit.Workbench.Api   http://localhost:5057
Athar.Api                     http://localhost:5068
```

كل API يستضيف Blazor WebAssembly Client الخاص به. لا تشغّل Client منفصلًا عندما تريد اختبار المسار الكامل API + UI + SQL Server.

## User Secrets لأثَر

مثال محلي:

```json
{
  "ConnectionStrings": {
    "Athar": "Server=.;Database=Athar;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True"
  },
  "AdminSeed": {
    "Enabled": true,
    "Email": "admin@athar.local",
    "DisplayName": "مسؤول منصة أثر",
    "Password": "A-strong-local-password"
  }
}
```

لا ترفع User Secrets أو كلمات المرور إلى Git.

الدليل الكامل:

**[تشغيل FoundationKit وWorkbench وأثَر على Visual Studio 2026](docs/VISUAL-STUDIO-2026-AR.md)**

---

# بناء واختبار المستودع

## عبر السكربت الموحد

```powershell
# Restore
powershell -NoProfile -ExecutionPolicy Bypass -File .\foundationkit.ps1 restore

# Build
powershell -NoProfile -ExecutionPolicy Bypass -File .\foundationkit.ps1 build -Configuration Release

# Test
powershell -NoProfile -ExecutionPolicy Bypass -File .\foundationkit.ps1 test -Configuration Release

# Full verification
powershell -NoProfile -ExecutionPolicy Bypass -File .\foundationkit.ps1 verify -Configuration Release

# NuGet packages
powershell -NoProfile -ExecutionPolicy Bypass -File .\foundationkit.ps1 pack -Configuration Release
```

## يدويًا

```bash
dotnet restore FoundationKit.sln
dotnet build FoundationKit.sln --configuration Release --no-restore
dotnet test FoundationKit.sln --configuration Release --no-build
```

التحقق الإضافي يشمل:

- Architecture and unit tests.
- Workbench tests.
- Athar tests.
- Generated capability catalog.
- Git whitespace checks.
- GitHub Pages route manifest.
- JavaScript syntax.
- Docker SQL-backed smoke workflows في CI.

## Pack output

```text
artifacts/packages/
```

الحزم:

```text
FoundationKit.Domain
FoundationKit.Application
FoundationKit.Infrastructure
FoundationKit.WebApi
FoundationKit.Blazor
```

---

# GitHub Actions

## `FoundationKit CI`

ينفذ:

- Repository boundary validation.
- JSON وPages validation.
- Restore وBuild وTest.
- Catalog check.
- Publish Workbench وAthar.
- Pack للحزم.
- SQL Server integration smoke tests.
- التحقق من MudBlazor static assets وSwagger.

## `FoundationKit Pages Portal`

ينفذ:

- فحص Atlas.
- فحص Browser Demo.
- بناء artifact ثابت.
- نشر GitHub Pages.

## `Athar Experimental Product Package`

ينفذ:

- Build وTest لأثَر.
- Windows x64 publish.
- ZIP artifact.
- Docker image تجريبية.
- رفع الصورة إلى GitHub Container Registry.

## `Athar Windows Launcher Check`

ينفذ على Windows PowerShell 5.1:

- التحقق أن launchers بصيغة ASCII آمنة.
- Parser validation.
- Smoke checks للأوامر المحلية.

---

# من المرجع إلى Production

## التعريف الصحيح

```text
FoundationKit = Production Baseline
Athar          = Production Reference
Your App       = Production only after environment approval
```

لا يصبح المنتج Production بمجرد أن يعمل على `localhost` أو Quick Tunnel.

## المسار المقترح

```text
1. تثبيت نطاق المنتج وقواعده
2. إنشاء منتج مستقل داخل apps/<ProductName>
3. إعادة استخدام FoundationKit packages
4. نقل قواعد المنتج من المرجع أو كتابتها حسب الحاجة
5. إعداد Production configuration خارج Git
6. تشغيل Build وTest وVerify
7. تنفيذ Security وData وObservability gates
8. نشر Staging
9. تنفيذ Acceptance وLoad وRecovery tests
10. اعتماد Release ثم نشر Production
```

يفضل إبقاء `examples/Athar` كمرجع مستقر، وإنشاء المنتج الحقيقي داخل:

```text
apps/<ProductName>/
```

## معمارية Production نموذجية

```text
Internet
   ↓
Trusted DNS + HTTPS Certificate
   ↓
Reverse Proxy / WAF / Edge Rate Limiting
   ↓
Athar ASP.NET Core Host
   ├── Blazor static files
   ├── API
   ├── Identity
   └── Health endpoints
   ↓
SQL Server with least-privilege account
   ├── Encrypted transport
   ├── Automated backups
   └── Tested restore procedure

Application
   ├── Central logs
   ├── Metrics
   ├── Tracing
   └── Alerts
```

## Build قبل النشر

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\foundationkit.ps1 production-check
```

هذا الأمر:

- يبني الحل.
- يشغّل الاختبارات.
- ينفذ Verify.
- يتأكد أن Admin Seed معطّل في الإعدادات المرفوعة.
- يتأكد أن Connection String غير مرفوع في `appsettings.json`.
- يعرض البوابات الخارجية المتبقية.

لا يمنح موافقة Production آلية؛ بل يفحص الجزء الذي يمكن فحصه من داخل المستودع.

## نشر ملفات Release

```bash
dotnet publish examples/Athar/Athar.Api/Athar.Api.csproj \
  --configuration Release \
  --output artifacts/athar-production
```

الناتج يستضيف:

- ASP.NET Core API.
- Blazor WebAssembly Client.
- Static assets.
- Swagger حسب إعدادات التطبيق الحالية.

## بناء Container

```bash
docker build \
  --file examples/Athar/Athar.Api/Dockerfile \
  --tag athar:1.0.0 \
  .
```

## Environment Variables الأساسية

```text
ASPNETCORE_ENVIRONMENT=Production
ConnectionStrings__Athar=<secret connection string>
AdminSeed__Enabled=false
```

عند التهيئة الأولى فقط يمكن استخدام Seed مضبوط في بيئة آمنة، ثم يجب تعطيله وتغيير بيانات الحساب الأولي.

## Migrations

المرجع يطبّق migrations عند startup مع retry. في Production عالي الحساسية يفضّل:

1. Backup قبل migration.
2. تشغيل migration كخطوة نشر مضبوطة.
3. فحص schema.
4. تشغيل التطبيق.
5. Smoke test.
6. Rollback plan.

## بوابة الأمن

- HTTPS إلزامي وشهادة موثوقة.
- Secret Vault أو Environment secrets آمنة.
- تعطيل أو تغيير Admin Seed.
- Email Confirmation وPassword Reset حقيقيان.
- MFA للحسابات الإدارية عند الحاجة.
- مراجعة Cookie وSameSite وDomain وCORS.
- SAST وDependency وSecret scanning.
- Penetration test حسب نطاق المنتج.

## بوابة البيانات

- SQL Server مُدار أو خطة تشغيل واضحة.
- Least-privilege database account.
- تشفير أثناء النقل وفي التخزين.
- Backup schedule.
- Restore drill موثق وناجح.
- Retention وExport وDeletion policy.

## بوابة التشغيل

- Centralized logging مع Correlation ID.
- Metrics وTracing.
- Alerts للصحة والأخطاء وزمن الاستجابة.
- SLO وSLA.
- Incident وRollback runbooks.
- Load test بحمل واقعي.
- Reverse Proxy أو WAF.
- Deployment strategy مثل Blue/Green عند الحاجة.

## بوابة المنتج والامتثال

- Privacy Policy.
- Terms of Use.
- Role and permission approval.
- Threat Model.
- Accessibility review.
- Device and language testing.
- متطلبات المجال مثل KYC/AML/PCI عندما تنطبق.

## قرار الجاهزية

```text
Core tests pass
    +
End-to-end tests pass
    +
Security gate pass
    +
Data recovery test pass
    +
Observability gate pass
    +
Performance gate pass
    +
Product acceptance pass
    =
Approved for production
```

الدليل الرسمي:

**[جاهزية FoundationKit للإنتاج](docs/PRODUCTION-READINESS-AR.md)**

---

# استكشاف الأخطاء

## تشغيل ملفات PowerShell ممنوع

استخدم:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\foundationkit.ps1 doctor
```

## ظهور رموز مثل `Ø§Ù`

ملفات التشغيل الأساسية يجب أن تبقى ASCII لتوافق Windows PowerShell 5.1. شغّل آخر نسخة من `main`:

```powershell
git switch main
git pull origin main
```

## Docker غير موجود

استخدم:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\foundationkit.ps1 start -Target Athar -Mode Native
```

أو استخدم `Auto` ليختار Native تلقائيًا.

## SQL Server غير متاح

تحقق من:

- تشغيل خدمة SQL Server.
- صحة اسم الـInstance.
- صلاحية Windows Authentication.
- Connection String في `.local/athar-product.env`.

ثم:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\foundationkit.ps1 logs -Target Athar
```

## المنفذ مستخدم

افحص:

```powershell
Get-NetTCPConnection -LocalPort 8090 -ErrorAction SilentlyContinue
Get-NetTCPConnection -LocalPort 5057 -ErrorAction SilentlyContinue
Get-NetTCPConnection -LocalPort 8080 -ErrorAction SilentlyContinue
```

ثم أوقف النسخة السابقة:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\foundationkit.ps1 stop -Target All
```

## Quick Tunnel لا يعمل

السكربت يفحص:

- DNS.
- TCP 443.
- Cloudflare API.
- HTTP/2 connectivity.
- إعادة المحاولة.

جرّب شبكة أخرى أو Mobile Hotspot عندما تمنع الشبكة الحالية الخدمة.

## معرفة الوضع الحالي

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\foundationkit.ps1 doctor
powershell -NoProfile -ExecutionPolicy Bypass -File .\foundationkit.ps1 status -Target All
```

---

# الإيقاف الآمن

## إيقاف الرابط العام

داخل نافذة Cloudflare:

```text
Ctrl+C
```

## إيقاف التطبيقات مع حفظ البيانات

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\foundationkit.ps1 stop -Target All
```

## التحقق

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\foundationkit.ps1 status -Target All
```

## تنظيف كامل عند الحاجة فقط

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\foundationkit.ps1 reset -Target All -Force
```

خذ نسخة احتياطية قبل أي Reset.

---

# الروابط المهمة

## المشروع

- Repository: **https://github.com/a2sn2/foundationkit-dotnet**
- Atlas: **https://a2sn2.github.io/foundationkit-dotnet/**
- Athar Browser Demo: **https://a2sn2.github.io/foundationkit-dotnet/athar-demo/**
- GitHub Actions: **https://github.com/a2sn2/foundationkit-dotnet/actions**

## الوثائق

- [Architecture](docs/ARCHITECTURE.md)
- [Dual Full-Stack Architecture](docs/DUAL-FULL-STACK.md)
- [Workbench](docs/WORKBENCH.md)
- [منصة أثَر](examples/Athar/README.md)
- [تشغيل Visual Studio 2026](docs/VISUAL-STUDIO-2026-AR.md)
- [تشغيل Native على Windows](docs/NATIVE-WINDOWS-RUN-AR.md)
- [المنتج التجريبي المجاني](docs/EXPERIMENTAL-PRODUCT-AR.md)
- [جاهزية الإنتاج](docs/PRODUCTION-READINESS-AR.md)
- [إضافة مشروع جديد](docs/ADDING-A-PROJECT-AR.md)

---

# الترخيص

المستودع تحت ترخيص [MIT](LICENSE).

استخدم FoundationKit كأساس، وأبقِ قواعد المنتج الفعلية داخل المنتج، ولا تعتبر أي نشر Production قبل اجتياز بوابة البيئة كاملة.
