using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Athar.Infrastructure;

public sealed class AccountSecurityOptions
{
    public const string SectionName = "AccountSecurity";

    public bool RequireConfirmedEmail { get; set; }

    public bool RequireAdministratorMfa { get; set; }

    public string SmtpHost { get; set; } = string.Empty;

    public int SmtpPort { get; set; } = 587;

    public bool SmtpEnableSsl { get; set; } = true;

    public string SmtpUsername { get; set; } = string.Empty;

    public string SmtpPassword { get; set; } = string.Empty;

    public string FromAddress { get; set; } = string.Empty;
}

public interface IAccountNotificationSender
{
    Task<bool> SendEmailConfirmationAsync(
        string destinationEmail,
        string confirmationToken,
        CancellationToken cancellationToken = default);

    Task<bool> SendPasswordResetAsync(
        string destinationEmail,
        string resetToken,
        CancellationToken cancellationToken = default);
}

public sealed class SmtpAccountNotificationSender(
    IOptions<AccountSecurityOptions> options,
    ILogger<SmtpAccountNotificationSender> logger)
    : IAccountNotificationSender
{
    private static readonly Action<ILogger, Exception?> DeliveryNotConfiguredLog =
        LoggerMessage.Define(
            LogLevel.Warning,
            new EventId(2101, "AccountNotificationNotConfigured"),
            "Account notification delivery is not configured. No account token was logged or returned.");

    private readonly AccountSecurityOptions _options = options.Value;

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
}
