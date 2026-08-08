# Madar v0.3 — التعليقات وسجل التعاون

هذه الوثيقة تصف شريحة **v0.3** الخاصة بالتعليقات داخل Madar. التعليقات هنا بيانات منتج مملوكة لـMadar وليست Capability عامة في FoundationKit.

## الهدف

إتاحة سجل تعاون بسيط وملحق بالحالة، بحيث يستطيع المستخدم المخول بقراءة الحالة إضافة تعليق وقراءة تاريخ التعليقات دون اختراع سياسة تعديل/حذف أو نظام إشعارات قبل وجود متطلب منتج واضح.

## نموذج البيانات

يُخزن التعليق في جدول:

```text
madar.CaseComments
```

ويحتوي:

```text
Id
CaseId
AuthorUserId
Body
CreatedUtc
RowVersion
```

قواعد النص:

- Plain text فقط.
- يُقص الفراغ من البداية والنهاية.
- مطلوب.
- الطول المقبول من 1 إلى 2000 حرف.
- لا يوجد edit أو delete في هذه الشريحة.

الجدول مرتبط بالحالة وبالمستخدم الكاتب، ويستخدم SQL Server `rowversion`. القراءة مرتبة بشكل حتمي حسب:

```text
CreatedUtc ثم Id
```

مع فهرس `(CaseId, CreatedUtc, Id)`.

## الصلاحيات

قائمة التعليقات وإضافة تعليق تعيدان استخدام نفس قاعدة رؤية الحالة الأم. يسمح للمستخدم عندما يكون واحدًا من:

```text
منشئ الحالة
OR المستخدم المسند إليه حاليًا
OR مستخدم لديه madar.cases.read-all
```

إذا كانت الحالة غير موجودة أو غير متاحة للمستخدم، يعاد نفس `NotFound` masking المستخدم في قراءة الحالات، لذلك لا تكشف واجهة التعليقات وجود Case لا يملك المستخدم حق الوصول إليها.

لا يوجد تعليق مجهول.

## API

```text
GET  /api/cases/{caseId}/comments
POST /api/cases/{caseId}/comments
```

المساران يتطلبان Authentication. إضافة التعليق تمر أيضًا عبر:

```text
Anti-CSRF
+
write rate limit
+
case-level authorization
```

## التدقيق والخصوصية

إضافة تعليق تسجل Audit action:

```text
madar.case.comment-added
```

لكن **نص التعليق لا يُنسخ إلى Audit attributes أو logs أو route values أو error messages**.

الـAudit يحتفظ ببيانات محدودة مثل `commentId`، بينما Actor/Correlation تأتي من Audit context المعتاد.

الحد المقصود هو:

```text
Comment Body → madar.CaseComments
Comment ID   → audit metadata
```

وبذلك يمكن إثبات أن تعليقًا أضيف دون تكرار محتواه في سجل التدقيق.

## الواجهة العربية

التعليقات تظهر داخل صفحة الحالة الموجودة أصلًا:

```text
/cases/{CaseId:guid}
```

ولا توجد route جديدة. لوحة «التعليقات والمتابعة» تعرض اسم الكاتب والتوقيت والنص كـPlain Text، وتسمح بإضافة تعليق حتى 2000 حرف.

التعليقات تبقى قابلة للقراءة بعد Resolve/Close لأنها سجل تعاون Append-only وليست Transition في الـWorkflow.

## التحقق الآلي

شريحة v0.3 يجب أن تثبت على الـexact PR head:

```text
Build + Tests
    ↓
SQL migration/startup
    ↓
Create Case
    ↓
Assign Operator
    ↓
Operator POST comment
    ↓
GET comments
    ↓
Verify author/body
    ↓
Resolve + Close
    ↓
Verify madar.case.comment-added
    ↓
Verify comment body absent from audit timeline
    ↓
Verify comment still readable after close
```

كما يجب أن تبقى اختبارات Workbench وAthar والبوابات الأمنية وCodeQL ومخرجات FoundationKit القابلة لإعادة الاستخدام دون Regression.

## خارج نطاق v0.3

- تعديل التعليقات أو حذفها أو Version History.
- Private/Internal note tiers.
- Mentions أو Watchers أو Subscriptions.
- Notifications.
- Attachments/Files.
- Rich Text/HTML.
- Reactions أو Moderation workflow.
- Multi-tenancy/organization hierarchy.
- استخراج `FoundationKit.Comments` أو حزمة Collaboration عامة.

## قاعدة الاستخراج

وجود حاجة واحدة في Madar لا يكفي لاستخراج Capability عامة. أي تعميم داخل FoundationKit يحتاج دليل إعادة استخدام مستقلًا من أكثر من Consumer، وليس مجرد اكتمال Roadmap.
