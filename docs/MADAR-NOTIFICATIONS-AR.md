# إشعارات مدار التشغيلية — v0.5

تضيف هذه الشريحة إشعارات بريد إلكتروني محدودة للأحداث التشغيلية المهمة في Madar، مع إعادة استخدام `FoundationKit.Notifications` و`FoundationKit.Notifications.Smtp` كما هما.

## الأحداث الحالية

```text
إسناد حالة
    ↓
إشعار الموظف المستلم

قرار اعتماد
    ↓
إشعار طالب الاعتماد

حل الحالة بواسطة مستخدم آخر
    ↓
إشعار منشئ الحالة
```

لا يوجد في v0.5 inbox داخل النظام، ولا SMS/WhatsApp/Push، ولا قوالب ديناميكية، ولا retries أو background jobs.

## ترتيب الحفظ والإرسال

القاعدة المهمة:

```text
Business change
      ↓
SQL commit
      ↓
Notification attempt
      ↓
Bounded delivery audit
```

لذلك `NotConfigured` أو `Failed` من مزود SMTP لا يعيدان إلغاء assignment/approval/resolution الذي تم حفظه بالفعل.

## إعداد SMTP

القسم المستخدم في ASP.NET Core configuration:

```text
Madar:Notifications:Smtp:Host
Madar:Notifications:Smtp:Port
Madar:Notifications:Smtp:EnableSsl
Madar:Notifications:Smtp:Username
Madar:Notifications:Smtp:Password
Madar:Notifications:Smtp:FromAddress
```

وفي `deploy/madar-compose.yml` تقابلها المتغيرات الاختيارية:

```text
MADAR_SMTP_HOST
MADAR_SMTP_PORT
MADAR_SMTP_ENABLE_SSL
MADAR_SMTP_USERNAME
MADAR_SMTP_PASSWORD
MADAR_SMTP_FROM_ADDRESS
```

القيمة الافتراضية للمنفذ هي `587` و`EnableSsl=true`، بينما Host وFromAddress يبقيان فارغين افتراضيًا في بيئة التطوير/CI حتى لا يجري اتصال SMTP خارجي.

إذا كان `Host` أو `FromAddress` فارغًا، يعتبر المزود غير مهيأ ويرجع:

```text
NotificationDeliveryStatus.NotConfigured
```

بدل محاولة اتصال خارجي.

الأسرار مثل SMTP password يجب أن تأتي من environment variables / secret store المناسب للبيئة، ولا تُكتب في Git أو logs أو audit timeline.

## الخصوصية وAudit

`NotificationMessage.Destination` و`NotificationMessage.Body` بيانات تشغيلية حساسة ولا يجب تسجيلها.

Madar يسجل فقط:

```text
action = madar.case.notification-delivery

attributes:
- purpose
- targetUserId
- deliveryStatus
```

ولا يسجل:

```text
email address
message body
SMTP password
provider exception text
```

## حدود النضج

هذه الشريحة تثبت Consumer ثانٍ مستقلًا لـ`FoundationKit.Notifications` بعد Athar، لكنها لا ترفع capability عن `ReferenceOnly`.

السبب أن كلا المستهلكين الحاليين ما زالا يستخدمان SMTP، ولا يوجد بعد دليل على multi-channel routing أو durable queues/retries أو provider diversity أو production delivery operations.

## مؤجل عمدًا

- Outbox / durable delivery;
- retries/backoff;
- background scheduler;
- templates/localization infrastructure;
- notification preferences;
- recipient groups;
- in-app inbox/read-unread;
- SMS/Push/WhatsApp/Webhook;
- SLA reminder scheduling;
- Production Approval أو اعتماد خارجي.
