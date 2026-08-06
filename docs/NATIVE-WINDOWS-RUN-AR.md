# تشغيل أثَر على Windows دون Docker

هذا المسار مخصص للأجهزة التي تحتوي على:

- .NET 8 SDK.
- SQL Server محلي يعمل بالـDefault Instance.
- Windows Authentication.

ولا تحتوي على Docker Desktop أو لا تريد استخدامه.

## التشغيل التلقائي

من جذر المستودع شغّل:

```text
START-ATHAR.cmd
```

السكربت يعمل بوضع `Auto`:

1. إذا وجد Docker جاهزًا يستخدم Docker.
2. إذا لم يجد Docker ينتقل تلقائيًا إلى `Native`.
3. في وضع Native ينشر Athar محليًا داخل `.local/athar-native/app`.
4. يستخدم الاتصال الافتراضي:

```text
Server=.;Database=Athar;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True
```

5. يطبق EF Core migrations.
6. ينشئ حساب المسؤول التجريبي.
7. يحفظ PID والسجلات داخل `.local`.
8. يفتح:

```text
http://localhost:8090
```

## إجبار الوضع المحلي

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File ".\scripts\athar-product.ps1" -Action Start -Mode Native
```

## الإيقاف

```text
STOP-ATHAR.cmd
```

أو:

```powershell
.\scripts\athar-product.ps1 -Action Stop -Mode Native
```

## فحص الحالة

```powershell
.\scripts\athar-product.ps1 -Action Status -Mode Native
```

## السجلات

```text
.local/logs/athar-native.out.log
.local/logs/athar-native.err.log
```

## بيانات الإدارة

تُنشأ تلقائيًا داخل:

```text
.local/athar-product.env
```

الملف مستبعد من Git.

## تغيير SQL Server

عدّل القيمة التالية داخل `.local/athar-product.env`:

```text
ATHAR_NATIVE_CONNECTION_STRING=Server=.;Database=Athar;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True
```

أمثلة:

```text
Server=ALHASSANASUSROG;Database=Athar;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True
```

```text
Server=.\SQLEXPRESS;Database=Athar;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True
```

## النسخ الاحتياطي

يتطلب أن تكون أداة `sqlcmd` متاحة في PATH:

```powershell
.\scripts\athar-product.ps1 -Action Backup -Mode Native
```

إذا لم تكن `sqlcmd` مثبتة، يمكن تنفيذ النسخ الاحتياطي من SSMS يدويًا.

## Reset

```powershell
.\scripts\athar-product.ps1 -Action Reset -Mode Native -Force
```

في وضع Native يحذف ملفات التشغيل والسجلات المحلية فقط، ويحتفظ بقاعدة `Athar` داخل SQL Server منعًا لحذف بيانات محلية بالخطأ.
