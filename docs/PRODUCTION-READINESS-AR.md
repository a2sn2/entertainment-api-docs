# جاهزية FoundationKit للإنتاج

## التعريف الصحيح

لا توجد مكتبة يمكن وصفها بأنها «جاهزة لأي Production» دون معرفة بيئة النشر والتهديدات والامتثال وحجم الحمل.

الاعتماد الصحيح في هذا المستودع هو:

> FoundationKit يوفّر Production Baseline قابلًا لإعادة الاستخدام، ومشروع أثَر يثبت دمجه من قاعدة البيانات حتى UI/UX. يصبح المنتج جاهزًا للإطلاق بعد اجتياز بوابة البيئة والتشغيل الخاصة به.

## ما يقدمه المستودع فعليًا

### الكور

- Entity وAggregateRoot وValue Objects وDomain Events.
- Result وتصنيف الأخطاء.
- Commands وQueries وUse Cases.
- Specifications وRepositories وUnit of Work.
- EF Core provider-neutral infrastructure.
- Problem Details وCorrelation ID وSecurity Headers.
- Typed Blazor API client وتصنيف أخطاء الشبكة.
- AsyncState وViewModelBase المناسبان لـBlazor.
- Pagination وGeneric Entity DTOs.
- Architecture tests وحدود تمنع تسرب SQL Server إلى الكور.

### مشروع أثَر

- ASP.NET Core Identity.
- Cookie Authentication آمنة وHttpOnly.
- Roles وسياسة Administrator.
- Password Policy وقفل الحساب.
- Anti-CSRF لكل POST.
- Rate Limiting على الدخول والكتابة.
- Validation في DTO وDomain.
- Idempotency عبر ClientRequestId فريد لكل مستخدم.
- Optimistic Concurrency عبر RowVersion.
- Audit Trail.
- SQL Server migrations.
- Startup migration retry.
- Live وReady health endpoints.
- Swagger وPostman.
- Docker topology.
- Unit tests وSQL-backed smoke workflow.
- أسرار عبر Configuration/User Secrets/Environment فقط.

## بوابة الإطلاق الفعلي

يجب ألا يتم إطلاق أي منتج حتى تكون البنود التالية مكتملة في بيئته:

### الأمن

- HTTPS إلزامي وشهادة موثوقة.
- تخزين الأسرار في Secret Vault.
- تغيير حساب المسؤول الأولي بعد التهيئة أو تعطيل Seed.
- تفعيل Email Confirmation وPassword Reset بمزود بريد حقيقي.
- MFA للحسابات الإدارية عند الحاجة.
- مراجعة CORS وCookie Domain وSameSite حسب النطاقات الفعلية.
- فحص SAST وDependency Scanning وSecret Scanning.
- اختبار اختراق وفق نطاق المنتج.

### البيانات

- SQL Server مُدار أو خطة نسخ احتياطي واختبار استعادة.
- تشفير البيانات أثناء النقل وفي التخزين.
- صلاحيات حساب قاعدة البيانات بالحد الأدنى.
- خطة Retention وحذف وتصدير للبيانات.
- تشغيل migrations كخطوة نشر مضبوطة بدل التشغيل التلقائي عندما تتطلب البيئة ذلك.

### التشغيل

- Logs مركزية مع Correlation ID.
- Metrics وTracing إلى مزود مراقبة.
- Alerts للصحة والأخطاء وزمن الاستجابة.
- تحديد SLO وSLA.
- Runbook للحوادث والتراجع.
- Load test بحمل واقعي.
- WAF/Reverse Proxy وسياسة Rate Limiting على الحافة.
- Deployment slots أو Blue/Green عند الحاجة.

### المنتج والامتثال

- سياسة خصوصية وشروط استخدام.
- تعريف الصلاحيات والأدوار بموافقة مالك المنتج.
- Threat Model.
- متطلبات KYC/AML/PCI أو غيرها حسب المجال.
- مراجعة سهولة الوصول واللغة والأجهزة المستهدفة.

## قرار الجاهزية

```text
Core tests pass
    +
Example end-to-end tests pass
    +
Security gate pass
    +
Data recovery test pass
    +
Observability gate pass
    +
Product acceptance pass
    =
Approved for production
```

أي فشل في بوابة البيئة لا يعني أن الكور ناقص؛ يعني أن المنتج لم يُجهّز بعد لبيئته الفعلية.
