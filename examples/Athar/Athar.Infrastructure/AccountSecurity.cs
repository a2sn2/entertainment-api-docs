using FoundationKit.Identity;
using FoundationKit.Notifications;
using FoundationKit.Notifications.Smtp;
using Microsoft.Extensions.Logging;

namespace Athar.Infrastructure;

public sealed class AccountSecurityDeliveryOptions
{
    public const string SectionName = AccountSecurityOptions.SectionName;

    public string SmtpHost { get; set; } = string.Empty;

    public int SmtpPort { get; set; } = 587;

    public bool SmtpEnableSsl { get; set; } = true;

    public string SmtpUsername { get; set; } = string.Empty;

    public string SmtpPassword { get; set; } = string.Empty;

    public string FromAddress { get; set; } = string.Empty;

    public SmtpNotificationOptions ToProviderOptions() => new()
    {
        Host = SmtpHost,
        Port = SmtpPort,
        EnableSsl = SmtpEnableSsl,
        Username = SmtpUsername,
        Password = SmtpPassword,
        FromAddress = FromAddress
    };
}

public sealed class AccountSecurityNotificationAdapter(INotificationSender sender)
    : IAccountNotificationSender
{
    public Task<bool> SendEmailConfirmationAsync(
        string destinationEmail,
        string confirmationToken,
        CancellationToken cancellationToken = default) =>
        SendAsync(
            destinationEmail,
            "تأكيد البريد الإلكتروني — منصة أثر",
            "استخدم رمز التأكيد التالي داخل صفحة الحساب في منصة أثر. لا تشارك هذا الرمز مع أي شخص:\n\n"
            + confirmationToken,
            "account.email-confirmation",
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
            "account.password-reset",
            cancellationToken);

    public Task<bool> SendSecurityNotificationAsync(
        string destinationEmail,
        AccountSecurityNotification notification,
        CancellationToken cancellationToken = default)
    {
        var (subject, action, purpose) = notification switch
        {
            AccountSecurityNotification.PasswordChanged =>
                ("تنبيه أمني — تم تغيير كلمة المرور", "تم تغيير كلمة مرور حسابك", "account.security.password-changed"),
            AccountSecurityNotification.PasswordReset =>
                ("تنبيه أمني — تمت إعادة تعيين كلمة المرور", "تمت إعادة تعيين كلمة مرور حسابك", "account.security.password-reset"),
            AccountSecurityNotification.MfaEnabled =>
                ("تنبيه أمني — تم تفعيل المصادقة الثنائية", "تمت إضافة عامل مصادقة ثنائية إلى حسابك", "account.security.mfa-enabled"),
            AccountSecurityNotification.MfaDisabled =>
                ("تنبيه أمني — تم تعطيل المصادقة الثنائية", "تمت إزالة عامل المصادقة الثنائية من حسابك", "account.security.mfa-disabled"),
            AccountSecurityNotification.RecoveryCodesRegenerated =>
                ("تنبيه أمني — تم تجديد رموز الاسترداد", "تم إبطال رموز الاسترداد السابقة وإنشاء مجموعة جديدة لحسابك", "account.security.recovery-codes-regenerated"),
            _ => throw new ArgumentOutOfRangeException(nameof(notification), notification, null)
        };

        var body = $"{action} في منصة أثر. إذا لم تكن أنت من نفذ هذه العملية، تواصل فورًا مع الجهة المسؤولة عن المنصة. لا تحتوي هذه الرسالة على كلمات مرور أو رموز مصادقة أو رموز استرداد.";
        return SendAsync(
            destinationEmail,
            subject,
            body,
            purpose,
            cancellationToken);
    }

    private async Task<bool> SendAsync(
        string destination,
        string title,
        string body,
        string purpose,
        CancellationToken cancellationToken)
    {
        var message = NotificationMessage.Create(
            destination,
            title,
            body,
            purpose);
        var result = await sender.SendAsync(message, cancellationToken);
        return result.IsDelivered;
    }
}

public sealed class AtharSmtpNotificationObserver(
    ILogger<AtharSmtpNotificationObserver> logger)
    : ISmtpNotificationObserver
{
    private static readonly Action<ILogger, string, Exception?> DeliveryNotConfiguredLog =
        LoggerMessage.Define<string>(
            LogLevel.Warning,
            new EventId(2101, "NotificationNotConfigured"),
            "Notification delivery is not configured for purpose {Purpose}. No notification destination or body was logged.");

    private static readonly Action<ILogger, string, string, Exception?> DeliveryFailedLog =
        LoggerMessage.Define<string, string>(
            LogLevel.Error,
            new EventId(2102, "NotificationDeliveryFailed"),
            "Notification delivery failed for purpose {Purpose} with error type {ErrorType}. No notification destination or body was logged.");

    public void NotConfigured(string purpose) =>
        DeliveryNotConfiguredLog(logger, purpose, null);

    public void DeliveryFailed(string purpose, string errorType) =>
        DeliveryFailedLog(logger, purpose, errorType, null);
}
