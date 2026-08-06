# إضافة مشروع جديد بجانب FoundationKit

## القاعدة

كل منتج جديد يكون مستقلًا داخل مجلده، ويستهلك حزم FoundationKit دون تعديلها لتناسب منتجًا واحدًا.

```text
examples/<ProjectName>/     أمثلة مكتملة
apps/<ProjectName>/         منتجات فعلية عند اعتماد هذا المسار
```

يمكن إضافة مجلد `apps/` مستقبلًا دون نقل مشروع أثَر.

## الهيكل القياسي

```text
<Project>.Domain
<Project>.Application
<Project>.Infrastructure
<Project>.Contracts
<Project>.Api
<Project>.Client
tests/<Project>.Tests
postman/<Project>.Api.postman_collection.json
deploy/<project>-compose.yml
```

## اتجاه الاعتماد

```text
Domain
  ↑
Application ← Contracts
  ↑
Infrastructure
  ↑
Api ← Client hosting

Client → Contracts + FoundationKit.Blazor
```

الممنوع:

- مرجع من Domain إلى EF Core أو ASP.NET Core.
- مرجع من Client إلى Infrastructure أو DbContext.
- وضع Migrations داخل `src/FoundationKit.*`.
- إعادة Generic CRUD لمجرد تقليل عدد الملفات.
- إرجاع Entity من API مباشرة.
- خلط عقود المستخدم مع عقود الإدارة دون سبب واضح.

## دور الطبقات

### Domain

Entities وAggregates وقواعد الأعمال والأحداث فقط.

### Application

Managers وUse Cases وPorts والتنسيق بين Domain وPersistence.

### Infrastructure

EF Core وSQL Server وIdentity وQueries وIntegrations.

### Contracts

DTOs وRequests وResponses وRoute constants.

### Api

Authentication وAuthorization وEndpoint groups وProblem Details وSwagger.

### Client

Typed API Client وViewModels وRazor Components وMudBlazor وUI/UX.

## Managers وServices

استخدم Manager عندما تجمع العملية أكثر من مسؤولية تطبيقية:

```text
validate current user
load aggregate
apply domain transition
write audit
commit unit of work
return DTO
```

استخدم Service لحد واضح مثل:

```text
IInitiativeQueryService
IAuditWriter
IEmailSender
IFileStorage
```

لا تنشئ `GenericManager<TEntity>` ينفذ CRUD لجميع الكيانات؛ هذا يلغي معنى قواعد الأعمال.

## MVVM مع Blazor

FoundationKit لا يقلد WPF حرفيًا. النموذج المعتمد:

```text
Component subscribes to ViewModel.StateChanged
ViewModel owns loading/error/data state
ViewModel calls typed client
Typed client uses shared contracts
```

استخدم:

```text
ViewModelBase
ListViewModel<T>
AsyncState<T>
ApiClientBase
```

## خطوات الإضافة

1. انسخ هيكل مشروع أثَر مع تغيير الاسم.
2. اكتب Domain قبل الواجهة.
3. عرّف Contracts حسب الجمهور.
4. اكتب Manager لكل Use Case حقيقي.
5. نفذ Infrastructure ومهاجرة مستقلة.
6. أضف Authentication/Authorization حسب المنتج.
7. اكتب Typed Client وViewModels.
8. ابنِ UI/UX.
9. أضف tests وPostman وDocker وCI smoke.
10. حدّث README وProduction Readiness.
