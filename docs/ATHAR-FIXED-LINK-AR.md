# رابط أثَر المجاني والثابت

الرابط الرسمي السهل للحفظ هو:

```text
https://a2sn2.github.io/foundationkit-dotnet/athar-live/
```

لا يحتاج شراء نطاق مخصص.

## كيف يعمل؟

الرابط الثابت نفسه مستضاف داخل GitHub Pages. عند تشغيل:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\foundationkit.ps1 expose -Target Athar
```

يقوم النظام بالآتي:

1. يتأكد أن أثَر جاهز على `http://localhost:8090`.
2. ينشئ Cloudflare Quick Tunnel جديدًا.
3. يقرأ عنوان `trycloudflare.com` العشوائي الذي أنشأته Cloudflare.
4. يحدّث ملف الحالة في فرع `athar-live-link` تلقائيًا.
5. يبقى رابط GitHub Pages ثابتًا ويحوّل الزائر إلى النفق الحالي.
6. عند إيقاف النفق يحاول تسجيل الحالة `offline` بدل ترك الرابط ظاهرًا كأنه شغّال.

```text
الرابط الثابت على GitHub Pages
        ↓
ملف الحالة في athar-live-link
        ↓
رابط Cloudflare الحالي
        ↓
Athar على جهازك: localhost:8090
```

## الاستخدام اليومي

شغّل أثَر:

```powershell
.\foundationkit.ps1 start -Target Athar -Mode Auto
```

ثم افتح النفق:

```powershell
.\foundationkit.ps1 expose -Target Athar
```

شارك دائمًا:

```text
https://a2sn2.github.io/foundationkit-dotnet/athar-live/
```

ولا تشارك رابط `trycloudflare.com` العشوائي إلا كحل احتياطي عند فشل تحديث الرابط الثابت.

## المتطلبات

- المستودع Public على GitHub.
- GitHub Pages مفعّل للمستودع.
- تسجيل Git المحلي يسمح بالدفع إلى فرع `athar-live-link`.
- `cloudflared` مثبت ومتوافر في `PATH`.
- الجهاز متصل بالإنترنت.
- أثَر شغّال محليًا.

## ما الذي يبقى مؤقتًا؟

العنوان الذي يحفظه المستخدم ثابت، لكن التطبيق الحقيقي ما زال يعمل من جهازك. لذلك يجب أن يبقى:

- الجهاز شغّالًا؛
- أثَر شغّالًا؛
- الإنترنت متصلًا؛
- نافذة `expose` مفتوحة.

عند إغلاق النفق يظهر الرابط الثابت أن العرض متوقف بدل إنشاء عنوان جديد للمستخدم.

## حدود مهمة

- هذا حل عرض تجريبي وليس استضافة Production.
- Quick Tunnel لا يقدّم ضمان استمرارية أو SLA.
- قد يحتاج تحديث الحالة ثوانٍ قليلة حتى يظهر عبر GitHub Raw cache.
- إغلاق نافذة Windows بالقوة قد يمنع خطوة تسجيل `offline`؛ تشغيل `expose` مرة أخرى يصحح الحالة.
- لا تستخدم بيانات حقيقية أو حساسة في العرض العام.
- الخدمات الخارجية تخضع لشروط وحدود مزوديها وقد تتغير مستقبلًا.
