# Madar v0.4 — اعتماد الحالات الحساسة

هذه الوثيقة تصف شريحة **v0.4** الخاصة ببوابة الاعتماد قبل حل بعض حالات Madar. الهدف هو استخدام Capability الاعتمادات الموجودة في FoundationKit داخل منتج حقيقي ثانٍ، مع إبقاء سياسة المنتج والبيانات والواجهة وقاعدة البيانات داخل Madar.

## قاعدة المنتج

نوعا الحالة التاليان يحتاجان اعتمادًا قبل الانتقال من `in-progress` إلى `resolved`:

```text
access-request
compliance-case
```

أما بقية أنواع الحالات الحالية فتحتفظ بمسار الحل المباشر المعتاد.

الاعتماد **لا يحل الحالة تلقائيًا** ولا يغلقها. هو فقط يفتح بوابة الانتقال إلى `resolved` بعد الموافقة.

## نموذج البيانات

تُخزن الاعتمادات في:

```text
madar.CaseApprovals
```

ويحتوي كل سجل على:

```text
Id
CaseId
RequestedByUserId
RequestedUtc
Status
ReviewedByUserId?
DecidedUtc?
DecisionNotes?
RowVersion
```

الحالات المسموحة:

```text
pending
approved
rejected
```

ملاحظات القرار Plain Text اختيارية ومحدودة إلى 1000 حرف. السجل يستخدم SQL Server `rowversion`، وتاريخ الاعتمادات يُقرأ بترتيب حتمي حسب `RequestedUtc` ثم `Id`.

## Maker-Checker

عند اتخاذ القرار يعاد استخدام `FoundationKit.Approvals` كما هو، دون إضافة API جديدة إليه.

الترتيب الأمني المقصود:

```text
هل يملك المستخدم madar.cases.approve؟
        ↓
نعم
        ↓
هل هو نفس طالب الاعتماد؟
        ↓
لا
        ↓
هل القرار approve أو reject صالح للحالة الحالية؟
        ↓
تنفيذ القرار
```

فحص الصلاحية يسبق كشف مخالفة Maker-Checker. هذا يمنع مستخدمًا غير مخول من استنتاج معلومات إضافية عن طالب الاعتماد.

ويحتفظ `CaseApproval` نفسه بدفاع Domain إضافي يمنع صاحب الطلب من اتخاذ القرار على طلبه حتى لو استُدعي مباشرة خارج Application orchestration.

## الصلاحيات

الصلاحية الجديدة:

```text
madar.cases.approve
```

في نموذج الأدوار الحالي تُمنح لـ:

```text
Supervisor
Administrator
```

طلب الاعتماد نفسه متاح فقط عندما:

- الحالة من نوع حساس؛
- الحالة `in-progress`؛
- المستخدم هو الموظف المسند إليه، أو لديه `madar.cases.progress-any`؛
- لا يوجد طلب `pending` حالي؛
- آخر طلب ليس `approved` بالفعل.

إذا رُفض آخر طلب يمكن إنشاء طلب جديد بعد استكمال المعالجة.

## API

```text
GET  /api/cases/{caseId}/approvals
POST /api/cases/{caseId}/approvals
POST /api/cases/{caseId}/approvals/{approvalId}/decision
```

جميع المسارات تتطلب Authentication. مسارات الكتابة تمر كذلك عبر:

```text
Anti-CSRF
+
write rate limit
+
product authorization
```

قراءة سجل الاعتماد تعيد استخدام Case visibility masking. الحالة غير الموجودة أو غير المتاحة للمستخدم لا تتحول إلى قناة تكشف وجودها عبر approval API.

## بوابة تسجيل الحل

عند محاولة:

```text
in-progress → resolved
```

لحالة حساسة، يقرأ Madar أحدث Approval للحالة. إذا لم يوجد، أو كان `pending`، أو كان `rejected`، يرجع Conflict ولا يغيّر حالة الـCase.

فقط عندما تكون أحدث حالة:

```text
approved
```

يستمر مسار `resolve` المعتاد.

## التدقيق والخصوصية

طلب الاعتماد يسجل:

```text
madar.case.approval-requested
```

واتخاذ القرار يسجل:

```text
madar.case.approval-decided
```

Audit metadata محدود إلى بيانات مثل:

```text
approvalId
decision
```

**DecisionNotes لا تُنسخ إلى Audit attributes أو logs أو route values أو error messages.**

الحد المقصود:

```text
Decision Notes → madar.CaseApprovals
Decision Code  → audit metadata
```

## الواجهة العربية

الاعتمادات تظهر داخل صفحة الحالة الموجودة:

```text
/cases/{CaseId:guid}
```

ولا توجد صفحة مستقلة جديدة. لوحة «اعتماد الحالة» تعرض:

- حالة آخر طلب؛
- طالب الاعتماد؛
- المراجع؛
- وقت الطلب والقرار؛
- ملاحظات القرار؛
- زر إنشاء طلب عندما يكون ذلك مسموحًا؛
- أزرار الموافقة/الرفض للمخولين؛
- تنبيه واضح عند منع الاعتماد الذاتي.

## التحقق الآلي

SQL smoke المستهدف يثبت:

```text
Create access-request
    ↓
Assign Operator
    ↓
Start Progress
    ↓
Resolve before approval → 409 blocked
    ↓
Operator requests approval
    ↓
Administrator approves
    ↓
Approval history persists
    ↓
Operator resolves successfully
    ↓
Administrator closes
    ↓
Audit contains request + decision
    ↓
Decision-notes marker absent from audit timeline
    ↓
Approval history remains readable after close
```

كما يجب أن تبقى Workbench وAthar وMadar السابقة والبوابات الأمنية وCodeQL ومخرجات الحزم القابلة لإعادة الاستخدام دون Regression.

## حدود FoundationKit.Approvals

Madar هو Consumer منتج مستقل إضافي لـ`FoundationKit.Approvals` بعد Athar، لكنه لا يبرر وحده توسيع API العامة أو ادعاء نضج ProductionReady. في v0.4 نعيد استخدام السطح الموجود فقط:

- `ApprovalPolicy`؛
- `ApprovalEligibility`؛
- `ApprovalDecisions`؛
- تكامل WorkflowDefinition.

أما Persistence وPermissions وUI وCase policy وAudit actions فهي ملك Madar.

## خارج نطاق v0.4

- Multi-stage approvals؛
- Parallel/Quorum approvals؛
- Dynamic approver discovery؛
- Delegation/Substitution؛
- Approval notifications؛
- Approval SLA أو background scheduler؛
- Files/Attachments؛
- تعديل/حذف/Versioning لسجلات الاعتماد؛
- Multi-tenancy/organization hierarchy؛
- Production Approval أو اعتماد تنظيمي/شهادة خارجية.
