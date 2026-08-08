# توجيه الحالات وقوائم انتظار الأقسام في مدار — v0.6

تضيف هذه الشريحة مفهومًا تشغيليًا محدودًا داخل **Madar**: القسم وقائمة الانتظار الخاصة به. الهدف هو نقل الحالة من وضع «جديدة وغير مسندة» إلى قسم مسؤول عنها، ثم السماح لموظف مؤهل داخل ذلك القسم باستلامها.

## التدفق

```text
حالة جديدة
   ↓
Supervisor / Administrator
   ↓
توجيه إلى قسم فعال
   ↓
الحالة تبقى New
   ↓
قائمة انتظار القسم
   ↓
Operator عضو في القسم
   ↓
Claim
   ↓
Assigned to current operator
   ↓
المسار الحالي: In Progress → Resolved → Closed
```

التوجيه لا يمثل حالة جديدة في الـworkflow. دورة الحالة الأساسية ما زالت:

```text
new → assigned → in-progress → resolved → closed
```

`DepartmentId` و`RoutedUtc` يصفان سياق التشغيل فقط.

## النموذج الحالي

Madar يملك داخل المنتج:

```text
Department
├── Id
├── Code
├── Name
├── IsActive
├── CreatedUtc
└── RowVersion

DepartmentMembership
├── DepartmentId
├── UserId
└── JoinedUtc

Case
├── DepartmentId?   ← nullable
└── RoutedUtc?      ← nullable
```

القيم nullable للحالات حتى تبقى الحالات القديمة وغير الموجهة صالحة. كما يظل الإسناد المباشر للحالة غير الموجهة مدعومًا للحفاظ على سلوك v0.1–v0.5.

## لماذا لا توجد Queue مستقلة؟

في v0.6 القسم نفسه هو حدود قائمة الانتظار:

```text
Department Queue =
Cases where DepartmentId == department
AND Status == new
AND AssignedToUserId == null
```

لا يوجد دليل حتى الآن أن المنتج يحتاج أكثر من Queue داخل القسم أو سياسات مستقلة لكل Queue. لذلك لم ننشئ Aggregate عامًا باسم Queue ولم نضف حزمة reusable لذلك.

## الصلاحيات

### التوجيه

`madar.cases.route`

ممنوحة حاليًا لـSupervisor وAdministrator. التوجيه يقبل فقط:

- قسمًا موجودًا وفعالًا؛
- حالة `new`؛
- حالة غير مسندة.

### قراءة قائمة الانتظار

- Operator يرى فقط الأقسام التي يملك عضوية فيها؛
- Supervisor وAdministrator يمكنهما رؤية الأقسام الفعالة وقوائمها عبر صلاحية القراءة العامة الحالية؛
- عدم العضوية يفشل مغلقًا ولا يعيد محتوى الحالات.

### الاستلام

`madar.cases.claim`

المستخدم يجب أن يكون:

1. مسجل الدخول؛
2. بدور Operator مؤهل للإسناد؛
3. عضوًا في القسم الموجهة إليه الحالة؛
4. والحالة ما زالت `new` وغير مسندة.

عند النجاح لا توجد آلة حالات جديدة؛ يستخدم Madar نفس `Case.Assign(...)` الموجودة مسبقًا، ولذلك يصبح الوضع `assigned` للمستخدم الحالي.

## الإسناد المباشر بعد التوجيه

إذا كانت الحالة تحمل `DepartmentId`، فإن الإسناد المباشر من Supervisor/Administrator يقبل فقط Operator عضوًا في القسم نفسه. هذا يمنع تجاوز حدود التوجيه من مسار الإسناد القديم.

أما الحالة غير الموجهة فتبقى قابلة للإسناد مباشرة كما في الإصدارات السابقة.

## التخزين

تضيف migration:

```text
20260808173000_AddDepartmentRouting
```

وتنشئ:

```text
madar.Departments
madar.DepartmentMemberships
```

وتضيف إلى `madar.Cases`:

```text
DepartmentId uniqueidentifier NULL
RoutedUtc   datetimeoffset NULL
```

مع FK وindexes مخصصة لقائمة الانتظار والعضويات.

## Bootstrap المحلي وCI

عندما يكون Madar bootstrap مفعّلًا، ينشئ النظام - إن لم يكن موجودًا - قسم تطويري حتمي:

```text
Code: operations
Name: العمليات
```

ثم يربط مستخدم Operator المزروع بهذا القسم. هذا ليس تعريفًا لهيكل تنظيمي إنتاجي؛ الغرض منه إعطاء الاختبارات المحلية وCI مسارًا حتميًا لإثبات route → queue → claim.

## Audit

التوجيه يسجل:

```text
action = madar.case.routed
attributes:
- departmentId
```

والاستلام يسجل:

```text
action = madar.case.claimed
attributes:
- departmentId
- claimantUserId
```

لا تضاف عناوين بريد أو بيانات اعتماد أو أجسام إشعارات إلى هذه الأحداث.

## حدود FoundationKit

هذه الشريحة **لا تضيف** `FoundationKit.Organization`.

القاعدة المعتمدة:

```text
حاجة ظهرت في Madar فقط
→ تبقى في apps/Madar

دلالة تنظيمية أثبتها أكثر من منتج
وأصبح لها عقد عام مستقل
→ عندها فقط تُقيّم كمرشح FoundationKit
```

وجود Department داخل Madar هو أول دليل منتجي على هذا النوع من الحاجة، وليس دليلًا كافيًا لاستخراج hierarchy عام إلى Core.

## مؤجل عمدًا

- شجرة مؤسسة / فروع / فرق متداخلة؛
- Multi-tenancy؛
- أكثر من Queue لكل قسم؛
- Round-robin أو skill-based routing؛
- presence/capacity/load balancing؛
- auto-assignment؛
- UI لإدارة الهيكل التنظيمي؛
- نقل/إعادة توجيه غني وتاريخ مستقل له؛
- SLA خاص بالقائمة أو business hours؛
- WhatsApp/Email ingestion؛
- الملفات والمرفقات؛
- أي حزمة FoundationKit جديدة.

هذه الوثيقة تصف سلوك المستودع فقط، ولا تمثل سياسة تنظيمية إنتاجية أو اعتمادًا أمنيًا خارجيًا.
