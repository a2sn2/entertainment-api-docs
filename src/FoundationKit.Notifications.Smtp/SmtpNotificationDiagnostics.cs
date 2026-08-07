namespace FoundationKit.Notifications.Smtp;

public interface ISmtpNotificationObserver
{
    void NotConfigured(string purpose);

    void DeliveryFailed(string purpose, string errorType);
}
