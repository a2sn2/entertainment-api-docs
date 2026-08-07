using System.Net;
using System.Net.Mail;
using FoundationKit.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Athar.Infrastructure;

public sealed class AccountSecurityDeliveryOptions
{
    public const string SectionName = IdentityPolicyOptions.SectionName;

    public string SmtpHost { get; set; } = string.Empty;

    public int SmtpPort { get; set; } = 587;

    public bool SmtpEnableSsl { get; set; } = true;

    public string SmtpUsername { get; set; } = string.Empty;

    public string SmtpPassword { get; set; } = string.Empty;

    public string FromAddress { get; set; } = string.Empty;
}

public sealed class SmtpAccountNotificationSender(
    IOptions<AccountSecurityDeliveryOptions> options,
    ILogger<SmtpAccountNotificationSender> logger)
    : IAccountNotificationSender
{
    private static readonly Action<ILogger, Exception?> DeliveryNotConfiguredLog =
        LoggerMessage.Define(
            LogLevel.Warning,
            new EventId(2101, "AccountNotificationNotConfigured"),
            "Account notification delivery is not configured. No account token or destination address was logged.");

    private static readonly Action<ILogger, Exception?> DeliveryFailedLog =
        LoggerMessage.Define(
            LogLevel.Error,
            new EventId(2102, "AccountNotificationDeliveryFailed"),
            "Account notification delivery failed. No account token or destination address was logged.");

    private readonly AccountSecurityDeliveryOptions _options = options.Value;

    public Task<bool> SendEmailConfirmationAsync(
        string destinationEmail,
        string confirmationToken,
        CancellationToken cancellationToken = default) =>
        SendAsync(
            destinationEmail,
            "تأكيد البريد الإلكتروني — منصة أثر",
            "استخدم رمز التأكيد التالي داخل صفحة الحساب في منصة أثر. لا تشارك هذا الرمز مع أي شخص:\n\n"
            + confirmationToken,
            cancellationToken);

    public Task<bool> SendPasswordResetAsync(
        string destinationEmail,
        string resetToken,
        CancellationToken cancellationToken = default) =>
        SendAsync(
            destinationEmail,
            "استعادة كلمة المرور — منصة أثر",
            "استخدم رمز استعادة كلمة المرور التالي داخل صفحة الحساب في منصة أثر. لا تشارك هذا الرمز مع أي شخص:\n\n"
            + resetToken,
            cancellationToken);

    public Task<bool> SendSecurityNotificationAsync(
        string destinationEmail,
        AccountSecurityNotification notification,
        CancellationToken cancellationToken = default)
    {
        var (subject, action) = notification switch
        {
            AccountSecurityNotification.PasswordChanged =>
                ("تنبيه أمني — تم تغيير كلمة المرور", "تم تغيير كلمة مرور حسابك"),
            AccountSecurityNotification.PasswordReset =>
                ("تنبيه أمني — تمت إعادة تعيين كلمة المرور", "تمت إعادة تعيين كلمة مرور حسابك"),
            AccountSecurityNotification.MfaEnabled =>
                ("تنبيه أمني — تم تفعيل المصادقة الثنائية", "تمت إضافة عامل مصادقة ثنائية إلى حسابك"),
            AccountSecurityNotification.MfaDisabled =>
                ("تنبيه أمني — تم تعطيل المصادقة الثنائية", "تمت إزالة عامل المصادقة الثنائية من حسابك"),
            AccountSecurityNotification.RecoveryCodesRegenerated =>
                ("تنبيه أمني — تم تجديد رموز الاسترداد", "تم إبطال رموز الاسترداد السابقة وإنشاء مجموعة جديدة لحسابك"),
            _ => throw new ArgumentOutOfRangeException(nameof(notification), notification, null)
        };

        var body = $"{action} في منصة أثر. إذا لم تكن أنت من نفذ هذه العملية، تواصل فورًا مع الجهة المسؤولة عن المنصة. لا تحتوي هذه الرسالة على كلمات مرور أو رموز مصادقة أو رموز استرداد.";
        return SendAsync(destinationEmail, subject, body, cancellationToken);
    }

    private async Task<bool> SendAsync(
        string destinationEmail,
        string subject,
        string body,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(_options.SmtpHost)
            || string.IsNullOrWhiteSpace(_options.FromAddress))
        {
            DeliveryNotConfiguredLog(logger, null);
            return false;
        }

        try
        {
            using var message = new MailMessage(
                _options.FromAddress.Trim(),
                destinationEmail.Trim(),
                subject,
                body);

            using var client = new SmtpClient(
                _options.SmtpHost.Trim(),
                _options.SmtpPort)
            {
                EnableSsl = _options.SmtpEnableSsl,
                DeliveryMethod = SmtpDeliveryMethod.Network
            };

            if (!string.IsNullOrWhiteSpace(_options.SmtpUsername))
            {
                client.Credentials = new NetworkCredential(
                    _options.SmtpUsername,
                    _options.SmtpPassword);
            }

            await client.SendMailAsync(message, cancellationToken);
            return true;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception) when (exception is SmtpException or InvalidOperationException or FormatException)
        {
            DeliveryFailedLog(logger, exception);
            return false;
        }
    }
}
