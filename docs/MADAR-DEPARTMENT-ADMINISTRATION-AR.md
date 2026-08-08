# إدارة الأقسام والعضويات في مدار — v0.7

## الهدف

بعد أن أثبت v0.6 التوجيه إلى الأقسام وقوائم الانتظار والاستلام، يضيف v0.7 طبقة الإدارة اللازمة لتصبح بيانات الأقسام قابلة للتشغيل اليومي بدل الاعتماد على بيانات Bootstrap فقط.

هذه القدرة **مملوكة لمنتج مدار**. لا يضيف هذا الإصدار `FoundationKit.Organization` ولا يفترض أن نموذج الأقسام الحالي هو عقد عام يصلح لكل المنتجات.

## الصلاحية

تُدار الأقسام من خلال الصلاحية:

```text
madar.departments.manage
```

وهي ممنوحة في النموذج الحالي لدور `Administrator` فقط. المصادقة وحدها لا تكفي؛ قرار الصلاحية النهائي يبقى داخل Application layer.

## نموذج القسم

```text
Department
├── Id
├── Code          ثابت بعد الإنشاء
├── Name          قابل للتعديل
├── IsActive
├── CreatedUtc
├── UpdatedUtc
└── RowVersion
```

قواعد الرمز:

- بعد التطبيع إلى lowercase يجب أن يكون طوله بين 2 و60 حرفًا؛
- يقبل الحروف والأرقام و`-` و`_` فقط؛
- يجب أن يكون فريدًا؛
- لا يتغير بعد إنشاء القسم.

اسم القسم بين 2 و120 حرفًا بعد إزالة المسافات الخارجية.

## تعطيل القسم

تعطيل القسم ليس حذفًا. عند الانتقال من `IsActive=true` إلى `false` يفحص التطبيق وجود أي Case غير مغلقة مرتبطة بالقسم.

```text
طلب تعطيل القسم
        ↓
هل توجد Case بحالة != closed ؟
   ├─ نعم → Conflict / رفض
   └─ لا  → تحديث القسم + Audit + Save
```

الهدف هو منع إخفاء قائمة انتظار أو عمل قائم بمجرد تعطيل القسم.

## العضويات

`DepartmentMembership` تبقى علاقة بسيطة بين المستخدم والقسم:

```text
DepartmentMembership
├── DepartmentId
├── UserId
└── JoinedUtc
```

إضافة العضوية تتطلب أن يكون المستخدم موجودًا ويحمل دور `Operator`. إذا كانت العضوية موجودة مسبقًا يرجع التطبيق Conflict واضحًا قبل الاعتماد على خطأ المفتاح المركب في SQL Server.

إزالة العضوية تُرفض إذا كان لدى الموظف Case غير مغلقة مسندة إليه داخل القسم نفسه:

```text
طلب إزالة عضو
        ↓
هل لديه Case في القسم + AssignedToUserId = user + Status != closed ؟
   ├─ نعم → Conflict / رفض
   └─ لا  → Remove membership + Audit + Save
```

هذه الحماية لا تنشئ Transfer أو Reassignment تلقائيًا؛ ذلك مؤجل لشريحة مستقلة حتى تُعرّف قواعد النقل بوضوح.

## API

المسار الإداري:

```text
GET    /api/admin/departments/
POST   /api/admin/departments/
PUT    /api/admin/departments/{departmentId}
GET    /api/admin/departments/{departmentId}/members
POST   /api/admin/departments/{departmentId}/members
DELETE /api/admin/departments/{departmentId}/members/{userId}
```

جميع المسارات تتطلب تسجيل الدخول. عمليات الكتابة تمر عبر Anti-CSRF ومسار `write` rate limiting الحالي. بعد ذلك يطبق Application manager صلاحية `madar.departments.manage` وقواعد العمل.

## واجهة Blazor

المسار:

```text
/admin/departments
```

متاح لدور `Administrator` ويعرض:

- إنشاء قسم جديد؛
- جميع الأقسام النشطة والمعطلة؛
- تعديل الاسم والحالة؛
- أعضاء القسم؛
- قائمة موظفي `Operator` المؤهلين للإضافة؛
- إضافة وإزالة العضوية؛
- رسائل الرفض عند وجود عمل مفتوح يمنع التعطيل أو الإزالة.

## التدقيق

الأحداث الحالية:

```text
madar.department.created
madar.department.updated
madar.department.member-added
madar.department.member-removed
```

البيانات الوصفية محدودة إلى معرفات لازمة مثل `departmentId` و`userId` ورمز القسم وحالة التفعيل. لا يُنسخ البريد أو كلمات المرور أو أوصاف الحالات أو نصوص الإشعارات إلى Audit attributes.

## SQL Server

المهاجرة:

```text
20260808180000_AddDepartmentAdministration
```

تضيف `UpdatedUtc` إلى `madar.Departments`. الأقسام السابقة تُرحّل بأمان عبر تعيين `UpdatedUtc = CreatedUtc` قبل جعل العمود غير قابل لـNULL.

يستمر `RowVersion` الموجود أصلًا في حماية تحديثات القسم من الكتابة المتزامنة المتعارضة.

## مسار التحقق

الاختبار SQL/E2E يثبت المسار التالي على SQL Server حقيقي:

```text
Admin login
   ↓
Create Department
   ↓
Add Operator membership
   ↓
Reject duplicate membership
   ↓
Create + Route Case
   ↓
Reject department deactivation while Case is open
   ↓
Assign Operator
   ↓
Reject membership removal while assignment is open
   ↓
Progress → Resolve → Close
   ↓
Remove membership
   ↓
Rename + Deactivate Department
   ↓
Verify persisted Audit metadata
```

ويعمل بعد مسار v0.6 route → queue → claim في نفس بوابة SQL integration حتى يظل التوافق الخلفي مثبتًا.

## ما لم يُنفذ في v0.7

لا يشمل هذا الإصدار شجرة تنظيمية، فروعًا أو فرقًا، تعدد المستأجرين، إدارة عامة للأدوار، نقل الحالات بين الأقسام، سجل نقل غني، أكثر من Queue للقسم، التوجيه حسب المهارة/السعة/الحضور، round-robin، auto-assignment، تقاويم ساعات العمل، أو استخراج `FoundationKit.Organization`.

## قاعدة الاستخراج إلى FoundationKit

وجود Department في Madar وحده ليس دليلًا كافيًا لبناء حزمة عامة. لا يبدأ استخراج Organization إلى FoundationKit إلا بعد ظهور حاجة مستقلة في منتج آخر واتفاق الدلالات والعقد القابل لإعادة الاستخدام دون تسريب قواعد مدار الخاصة.