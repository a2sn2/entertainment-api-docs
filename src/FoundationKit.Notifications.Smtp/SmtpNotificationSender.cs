using System.Net;
using System.Net.Mail;
using FoundationKit.Notifications;

namespace FoundationKit.Notifications.Smtp;

public sealed class SmtpNotificationSender(
    SmtpNotificationOptions options,
    ISmtpNotificationObserver? observer = null)
    : INotificationSender
{
    private readonly SmtpNotificationOptions _options = Snapshot(options);

    public async Task<NotificationDeliveryResult> SendAsync(
        NotificationMessage message,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(_options.Host)
            || string.IsNullOrWhiteSpace(_options.FromAddress))
        {
            observer?.NotConfigured(message.Purpose);
            return NotificationDeliveryResult.NotConfigured();
        }

        try
        {
            using var mailMessage = new MailMessage(
                _options.FromAddress,
                message.Destination,
                message.Title,
                message.Body);

            using var client = new SmtpClient(
                _options.Host,
                _options.Port)
            {
                EnableSsl = _options.EnableSsl,
                DeliveryMethod = SmtpDeliveryMethod.Network
            };

            if (!string.IsNullOrWhiteSpace(_options.Username))
            {
                client.Credentials = new NetworkCredential(
                    _options.Username,
                    _options.Password);
            }

            await client.SendMailAsync(mailMessage, cancellationToken);
            return NotificationDeliveryResult.Delivered();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception) when (exception is SmtpException or InvalidOperationException or FormatException)
        {
            observer?.DeliveryFailed(
                message.Purpose,
                exception.GetType().Name);
            return NotificationDeliveryResult.Failed();
        }
    }

    private static SmtpNotificationOptions Snapshot(SmtpNotificationOptions options)
    {
        SmtpNotificationOptionsValidator.Validate(options);

        return new SmtpNotificationOptions
        {
            Host = options.Host.Trim(),
            Port = options.Port,
            EnableSsl = options.EnableSsl,
            Username = options.Username.Trim(),
            Password = options.Password,
            FromAddress = options.FromAddress.Trim()
        };
    }
}
