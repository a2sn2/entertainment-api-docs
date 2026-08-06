# إخراج أثَر كمنتج تجريبي مجاني

هذه الوثيقة تعتمد هدفًا واحدًا فقط:

> عرض وتشغيل أثَر كمنتج تجريبي كامل دون أي خدمة مدفوعة.

تم تجهيز طريقتين رسميتين داخل المستودع:

```text
الطريقة 1 — جهازك
Athar الحقيقي + API + Identity + EF Core + SQL Server

الطريقة 2 — مواقع مجانية
GitHub Pages كتجربة تفاعلية + GitHub Actions لإنتاج حزمة تشغيل وDocker Image
```

---

## الطريقة الأولى — تشغيل المنتج الحقيقي على جهازك

هذه هي النسخة الكاملة التي اختبرناها محليًا:

```text
Blazor WebAssembly
        ↓
ASP.NET Core API
        ↓
Identity + Roles + CSRF
        ↓
Application + Domain
        ↓
EF Core + SQL Server
```

### المتطلبات

- Windows 11.
- Docker Desktop.
- اتصال إنترنت في أول تشغيل لتنزيل Images والحزم.
- مساحة تخزين كافية لـSQL Server وBuild Images.

لا تحتاج إلى إنشاء قاعدة البيانات يدويًا، ولا تحتاج إلى كتابة كلمات مرور داخل Git.

### التشغيل بضغطة

من جذر المستودع شغّل:

```text
START-ATHAR.cmd
```

سيقوم الملف تلقائيًا بـ:

1. التحقق من Docker.
2. إنشاء كلمات مرور قوية محلية داخل `.local/athar-product.env`.
3. بناء Athar API وBlazor Client.
4. تشغيل SQL Server Developer داخل Container.
5. تطبيق EF Core migrations.
6. إنشاء حساب المسؤول الأولي.
7. انتظار `/health/ready`.
8. فتح المنتج على:

```text
http://localhost:8090
```

ملف `.local/athar-product.env` مستبعد من Git ولا يُرفع إلى المستودع.

### التشغيل من PowerShell

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\athar-product.ps1 -Action Start
```

### الإيقاف مع الاحتفاظ بالبيانات

```text
STOP-ATHAR.cmd
```

أو:

```powershell
.\scripts\athar-product.ps1 -Action Stop
```

هذا يوقف Containers فقط، ويحتفظ بقاعدة البيانات داخل Docker Volume.

### فحص الحالة

```powershell
.\scripts\athar-product.ps1 -Action Status
```

### فتح المنتج من جديد

```powershell
.\scripts\athar-product.ps1 -Action Open
```

### عرض الروابط داخل الشبكة المحلية

```powershell
.\scripts\athar-product.ps1 -Action Lan
```

سيعرض روابط مثل:

```text
http://192.168.1.20:8090
```

يمكن لأي جهاز على نفس Wi-Fi/LAN فتح الرابط إذا كان Windows Firewall يسمح بالمنفذ `8090`.

### إنشاء نسخة احتياطية

شغّل Athar أولًا، ثم:

```powershell
.\scripts\athar-product.ps1 -Action Backup
```

سيُنشأ ملف SQL Server backup داخل:

```text
.local/backups/AtharDb-yyyyMMdd-HHmmss.bak
```

### حذف التجربة بالكامل

هذا الأمر يحذف Containers وVolume وقاعدة البيانات المحلية:

```powershell
.\scripts\athar-product.ps1 -Action Reset -Force
```

لا تستخدمه إلا عندما تريد بدء تجربة جديدة تمامًا.

### رابط مؤقت عبر الإنترنت باستخدام جهازك

بعد تشغيل Athar على `http://localhost:8090` يمكن تشغيل:

```powershell
.\scripts\expose-athar-tunnel.ps1
```

السكربت يحتاج أداة `cloudflared` مثبتة ومتاحة في PATH. عند نجاحه يعطيك رابط HTTPS مؤقتًا يصل إلى المنتج الذي يعمل على جهازك.

حدود هذا الأسلوب:

- يجب أن يبقى جهازك شغالًا.
- يجب أن تبقى نافذة Tunnel مفتوحة.
- الرابط مؤقت وقد يتغير في كل تشغيل.
- البيانات وقاعدة SQL تبقى على جهازك.
- لا تُدخل بيانات حقيقية أو حساسة في العرض التجريبي.

---

## الطريقة الثانية — مواقع مجانية

### 1. تجربة أثَر على GitHub Pages

الرابط بعد نشر الفرع الرئيسي:

```text
https://a2sn2.github.io/foundationkit-dotnet/athar-demo/
```

هذه التجربة تعرض دورة المنتج كاملة داخل المتصفح:

```text
المستخدم ينشئ مبادرة
        ↓
تظهر في لوحة الإدارة
        ↓
اعتماد أو رفض مع ملاحظة
        ↓
القرار يظهر للمستخدم
```

وتحتوي على:

- واجهة عربية RTL.
- مساحة مستخدم.
- لوحة إدارة.
- إنشاء مبادرات.
- اعتماد ورفض.
- إحصاءات بسيطة.
- حفظ حالة التجربة في `localStorage`.
- زر إعادة التجربة.

### ما الذي لا تحتويه نسخة GitHub Pages؟

GitHub Pages استضافة ثابتة، لذلك هذه النسخة لا تحتوي على:

- ASP.NET Core API حقيقي.
- SQL Server.
- ASP.NET Core Identity.
- Cookie Authentication.
- سجل تدقيق على الخادم.
- بيانات مشتركة بين الزوار.

كل زائر يملك بياناته التجريبية داخل متصفحه فقط.

هذا ليس نقصًا مخفيًا؛ الواجهة تعلن بوضوح أنها `DEMO · بدون خادم`.

### 2. GitHub Actions لإنتاج ملفات المنتج

Workflow:

```text
Athar Experimental Product Package
```

يُشغّل يدويًا من:

```text
GitHub → Actions → Athar Experimental Product Package → Run workflow
```

ويقوم مجانًا ضمن خدمات GitHub المتاحة في المستودع بـ:

1. Restore.
2. Build Release.
3. تشغيل اختبارات Athar.
4. نشر Windows x64 output.
5. إنشاء ZIP قابل للتنزيل من Artifacts.
6. بناء Docker Image.
7. رفع الصورة إلى GitHub Container Registry باسم تجريبي.

الحزمة وDocker Image لا تعنيان أن GitHub يشغل SQL Server كتطبيق دائم؛ بل تعنيان أن ملفات المنتج جاهزة للتنزيل والتشغيل على جهاز أو سيرفر يدعم Docker.

---

## أي طريقة تستخدم؟

### للعرض الحقيقي الكامل

استخدم جهازك:

```text
START-ATHAR.cmd
```

هذه الطريقة تختبر جميع الطبقات وقاعدة البيانات والحسابات.

### لإرسال رابط سريع لأي شخص

استخدم GitHub Pages:

```text
https://a2sn2.github.io/foundationkit-dotnet/athar-demo/
```

هذه الطريقة لا تتطلب تشغيل جهازك، لكنها Demo متصفح فقط.

### لإرسال رابط للتطبيق الحقيقي مؤقتًا

شغّل Athar على جهازك ثم استخدم Tunnel:

```powershell
.\scripts\expose-athar-tunnel.ps1
```

---

## الأشياء التي لا أستطيع تنفيذها بدلًا عنك

تم تجهيز الكود والملفات والـWorkflows، لكن توجد أعمال مرتبطة بجهازك أو حساباتك لا يمكن تنفيذها عن بُعد من المستودع:

1. تثبيت Docker Desktop أو `cloudflared` على جهازك.
2. قبول شروط خدمة أي مزود خارجي باسمك.
3. إبقاء جهازك شغالًا ومتصلًا بالإنترنت.
4. فتح منفذ داخل Windows Firewall أو إعداد الراوتر دون وصول مباشر إلى جهازك.
5. تغيير DNS أو إعدادات الشبكة المنزلية.
6. الضغط على `Run workflow` عندما يتطلب GitHub تأكيدًا من صاحب الحساب.
7. تحويل GitHub Pages إلى Backend دائم؛ Pages يدعم الملفات الثابتة فقط.
8. ضمان استمرار أي خطة مجانية خارج GitHub إذا غيّر المزود شروطه مستقبلًا.
9. إدخال أسرارك أو كلمات مرورك في خدمات خارجية.

كل ما عدا هذه النقاط أصبح موجودًا داخل الريبو ككود وسكربتات ووثائق قابلة للتشغيل.

---

## حدود النسخة التجريبية

- SQL Server Developer مناسب للتطوير والاختبار التجريبي، وليس ترخيص تشغيل تجاري.
- بيانات GitHub Pages محلية داخل المتصفح.
- رابط Tunnel مؤقت ويعتمد على بقاء جهازك يعمل.
- النسخة التجريبية ليست بديلًا عن الاستضافة الإنتاجية أو النسخ الاحتياطي المؤسسي.
- لا تستخدم بيانات عملاء أو معلومات شخصية حقيقية.

---

## خريطة الملفات

```text
START-ATHAR.cmd
STOP-ATHAR.cmd
scripts/athar-product.ps1
scripts/expose-athar-tunnel.ps1
deploy/athar-compose.yml
site/athar-demo/index.html
site/athar-demo/styles.css
site/athar-demo/app.js
.github/workflows/experimental-product.yml
```
