# منصة أثَر — المشروع العربي المرجعي

**أثَر** مشروع Full-Stack احترافي يوضح استخدام FoundationKit داخل منتج حقيقي مستقل عن الـWorkbench.

فكرة المشروع: إدارة المبادرات المجتمعية من لحظة إنشاء الحساب وتقديم المبادرة، مرورًا بقائمة مراجعة الإدارة، حتى الاعتماد أو الرفض وظهور القرار للمستخدم.

## لماذا هذا المثال موجود؟

الـWorkbench يشرح بنية الكور وحدوده. أمّا أثَر فيثبت أن الكور يمكن ربطه بمنتج كامل يملك:

- قاعدة بيانات ومهاجرات مستقلة؛
- Domain وEntities وقواعد أعمال؛
- Application Managers وServices؛
- DTOs وعقود API مصنفة؛
- مصادقة وأدوار وصلاحيات؛
- Blazor WebAssembly وMudBlazor؛
- ViewModels بأسلوب MVVM مناسب لـBlazor؛
- واجهة مستخدم ولوحة إدارة عربية؛
- حماية CSRF وRate Limiting وقفل الحساب؛
- Idempotency لمنع إنشاء الطلب مرتين؛
- Optimistic Concurrency عبر RowVersion؛
- Audit Trail؛
- Health Checks وSwagger وPostman؛
- Docker وCI واختبارات.

## خريطة المشاريع

```text
examples/Athar/
├── Athar.Domain          Entities, aggregates, events, invariants
├── Athar.Application     managers, use cases, ports, orchestration
├── Athar.Infrastructure  EF Core, SQL Server, Identity, queries, audit, migrations
├── Athar.Contracts       DTOs, routes, requests, responses
├── Athar.Api             ASP.NET Core host, security, endpoints, Swagger
└── Athar.Client          Blazor WebAssembly, MudBlazor, ViewModels, Arabic UI/UX
```

الاختبارات:

```text
tests/Athar.Tests
```

## التدفق الكامل

```text
المستخدم يسجل حسابًا
        ↓
Cookie Authentication + CSRF
        ↓
ينشئ مبادرة
        ↓
InitiativeManager
        ↓
Initiative Aggregate
        ↓
SQL Server: athar.Initiatives
        ↓
تظهر في لوحة الإدارة
        ↓
الإدارة تعتمد أو ترفض
        ↓
InitiativeReview + AuditEntry + Status transition
        ↓
المستخدم يرى القرار
```

## Generic Base وDTO وEntity

FoundationKit يوفّر القواعد العامة:

```text
Entity<TId>
AggregateRoot<TId>
EntityDto<TId>
AuditedEntityDto<TId>
PageRequest
PagedResult<T>
IRepository<TEntity, TId>
IUnitOfWork
Result<T>
ViewModelBase
ListViewModel<T>
```

المشروع لا يستخدم Generic CRUD Manager لأن ذلك يسرّب قواعد المنتج. بدلًا منه يستخدم:

```text
IInitiativeManager
InitiativeManager
IInitiativeQueryService
AuditWriter
AtharApiClient
AccountViewModel
InitiativesViewModel
AdminViewModel
```

هذا هو تطبيق MVVM المناسب لـBlazor:

```text
Razor Page
    ↓ binds and observes
ViewModelBase
    ↓ calls
Typed Api Client
    ↓
Contracts + API
```

## التشغيل المحلي لاحقًا

سيتم تنفيذ التجربة المحلية بعد اكتمال واعتماد جميع التعديلات.

المشروع الافتراضي:

```text
Athar.Api
```

الرابط:

```text
http://localhost:5068
```

أهم المسارات:

```text
/               الصفحة العامة
/account        التسجيل والدخول
/initiatives    مساحة المستخدم
/admin          لوحة الإدارة
/swagger        توثيق API
/health/live    فحص العملية
/health/ready   فحص SQL Server
```

## User Secrets

```json
{
  "ConnectionStrings": {
    "Athar": "Server=.;Database=AtharDb;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True"
  },
  "AdminSeed": {
    "Enabled": true,
    "Email": "admin@athar.local",
    "DisplayName": "مسؤول منصة أثر",
    "Password": "<strong-local-password>"
  }
}
```

لا تُحفظ كلمات المرور داخل المستودع.

## Postman

```text
postman/Athar.Api.postman_collection.json
```

المجموعة تمشي بالتسلسل من إنشاء المستخدم والمبادرة حتى مراجعة الإدارة ورجوع الحالة الجديدة.

## قاعدة البيانات

Schemas:

```text
identity.Users
identity.Roles
identity.UserRoles
athar.Initiatives
athar.InitiativeReviews
athar.AuditEntries
```

## حدود الإنتاج

المشروع يطبق Production Baseline داخل الكود، لكنه لا يستطيع اختيار بنية الاستضافة نيابة عن المنتج. قبل الإطلاق الفعلي يجب اجتياز بوابة النشر الموضحة في:

```text
docs/PRODUCTION-READINESS-AR.md
```
