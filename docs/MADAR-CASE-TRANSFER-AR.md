# مدار — نقل الحالات وإعادة الإسناد

يوثق هذا الملف نطاق **Madar v0.8** الخاص بالنقل التشغيلي للحالات وإعادة الإسناد. هذه السلوكيات مملوكة لمنتج مدار ولا تمثل Capability عامة في FoundationKit.

## 1. الهدف

بعد إضافة الأقسام وقوائم الانتظار وإدارتها، أصبح من الضروري معالجة الحالات التي تحتاج إلى تغيير القسم أو الموظف المسؤول دون حذف السجل أو الالتفاف على ضوابط العضوية والصلاحيات.

يدعم الإصدار مسارين مستقلين:

```text
Reassignment
Case remains in same department
Assigned / InProgress
        ↓
Different eligible Operator
        ↓
Lifecycle state preserved
SLA preserved

Transfer
Case already routed
New / Assigned / InProgress
        ↓
Different active department
        ↓
Assignee cleared
Status = New
Target department queue
SLA preserved
```

## 2. الصلاحيات

- `madar.cases.reassign`: إعادة إسناد حالة نشطة إلى Operator آخر.
- `madar.cases.transfer`: نقل حالة نشطة إلى قسم فعال آخر.

الصلاحيتان ممنوحتان حاليًا لـ`Supervisor` و`Administrator` فقط. دور `Operator` لا يحصل عليهما.

## 3. إعادة الإسناد

تقبل إعادة الإسناد الحالات `assigned` و`in-progress` فقط. يجب أن يكون الموظف الجديد مستخدمًا صالحًا ويحمل دور `Operator`، ويجب أن يختلف عن الموظف الحالي.

إذا كانت الحالة موجهة إلى قسم، يتحقق التطبيق من أن القسم ما زال فعالًا وأن الموظف الجديد عضو فيه. إعادة الإسناد لا تغير `Status` ولا `DepartmentId` ولا `RoutedUtc` ولا قيم SLA.

بعد نجاح الحفظ في SQL Server يسجل النظام التدقيق أولًا ضمن نفس معاملة العمل، ثم يحاول إشعار الموظف الجديد باستخدام منسق الإشعارات القائم. فشل أو عدم تهيئة وسيلة الإشعار لا يعيد معاملة إعادة الإسناد إلى الوراء.

سجل التدقيق:

```text
madar.case.reassigned
├── previousAssigneeUserId
├── assigneeUserId
├── status
└── departmentId      (عند وجود قسم)
```

لا يتم نسخ البريد الإلكتروني أو محتوى الحالة أو نص الإشعار إلى خصائص التدقيق.

## 4. نقل الحالة بين الأقسام

النقل يتطلب أن تكون الحالة موجهة مسبقًا إلى قسم، وأن يكون القسم الجديد فعالًا ومختلفًا عن القسم الحالي. يسمح بالنقل عندما تكون الحالة `new` أو `assigned` أو `in-progress`، ويمنع بعد `resolved` أو `closed`.

عند النجاح:

- يتغير `DepartmentId` إلى القسم الجديد.
- يتحدث `RoutedUtc` و`UpdatedUtc` إلى وقت النقل.
- يزال `AssignedToUserId`.
- تصبح الحالة `new` لتظهر في قائمة انتظار القسم الجديد.
- يبقى المنشئ والعنوان والوصف والنوع والأولوية محفوظين.
- تبقى أهداف وأدلة SLA محفوظة.
- تبقى التعليقات والاعتمادات والسجل السابق محفوظة.

سجل التدقيق:

```text
madar.case.transferred
├── fromDepartmentId
├── toDepartmentId
├── previousStatus
└── previousAssigneeUserId   (عند وجود إسناد سابق)
```

إذا كان النقل يعيد حالة `assigned` أو `in-progress` إلى `new`، يسجل الـDomain كذلك `CaseStatusChanged` كدليل دورة حياة داخلي، بينما يبقى الحدث التشغيلي الواضح في Timeline هو `madar.case.transferred`.

## 5. API

```http
POST /api/cases/{caseId}/reassignment
POST /api/cases/{caseId}/transfer
```

مثال إعادة الإسناد:

```json
{
  "assigneeUserId": "00000000-0000-0000-0000-000000000000"
}
```

مثال النقل:

```json
{
  "departmentId": "00000000-0000-0000-0000-000000000000"
}
```

كلا المسارين يتطلب تسجيل الدخول، ويمران عبر Application authorization، وAnti-CSRF، وسياسة `write` rate limiting الحالية. تعارضات الحالة التشغيلية ترجع كـ`409`، وفشل الصلاحية كـ`403`، والقسم غير الموجود/غير الفعال كـ`404` وفق عقود الأخطاء الحالية.

## 6. واجهة Blazor

صفحة تفاصيل الحالة العربية تعرض للمشرف/المدير، عندما تسمح حالة العمل:

- اختيار قسم فعال آخر ثم تنفيذ النقل؛
- اختيار Operator آخر ثم تنفيذ إعادة الإسناد؛
- رسالة توضيحية بأن النقل يعيد الحالة إلى قائمة انتظار القسم الجديد؛
- رسالة توضيحية بأن إعادة الإسناد تحافظ على حالة المعالجة وSLA؛
- تحديث تفاصيل الحالة وTimeline بعد نجاح العملية.

الخادم يبقى مصدر الحقيقة لكل قواعد العضوية والصلاحيات حتى لو عرضت الواجهة خيارات أوسع.

## 7. التحقق الآلي

تغطي اختبارات الوحدة:

- نقل حالة قيد المعالجة وإزالة الإسناد مع حفظ SLA؛
- منع النقل إلى القسم نفسه؛
- منع نقل الحالة المحلولة؛
- الحفاظ على حالة `in-progress` عند إعادة الإسناد؛
- منع إعادة الإسناد إلى الموظف نفسه؛
- صلاحيات Supervisor/Administrator مقابل Operator؛
- عضوية الموظف الجديد في القسم؛
- bounded audit metadata؛
- حدوث إشعار إعادة الإسناد بعد `SaveChanges`.

ويغطي SQL/E2E المسار:

```text
route
  ↓
assign Operator A
  ↓
start-progress
  ↓
reassign Operator B
  ↓
transfer to Department B
  ↓
target queue
  ↓
claim
  ↓
SQL persistence + audit timeline
```

يستخدم اختبار التكامل قاعدة بيانات وحاويات مؤقتة، ويضيف للمستخدم الإداري دور `Operator` داخل fixture المؤقت فقط ليكون Assignee ثانٍ مؤهلًا دون إضافة API أو إعداد Bootstrap مخصص للاختبارات إلى المنتج.

## 8. الحدود المؤجلة

لا يشمل v0.8:

- Round-robin أو التوجيه الآلي؛
- Presence أو Capacity أو Skills؛
- Bulk reassignment؛
- Transfer approval؛
- شجرة تنظيمية أو Multi-tenancy؛
- جدول Routing History مستقل عن Audit Timeline الحالي؛
- Queue-specific SLA أو business hours؛
- ملفات/مرفقات؛
- قنوات WhatsApp أو البريد الوارد؛
- استخراج `FoundationKit.Organization` أو حزمة Routing عامة.

قرار إعادة الاستخدام سيأتي فقط بعد ظهور احتياج مستقل من منتج آخر وعقد عام واضح، وليس لأن مدار احتاج هذه الوظيفة وحده.
