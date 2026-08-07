namespace FoundationKit.Notifications;

public enum NotificationDeliveryStatus
{
    Delivered,
    NotConfigured,
    Failed
}

public readonly record struct NotificationDeliveryResult(NotificationDeliveryStatus Status)
{
    public bool IsDelivered => Status == NotificationDeliveryStatus.Delivered;

    public static NotificationDeliveryResult Delivered() =>
        new(NotificationDeliveryStatus.Delivered);

    public static NotificationDeliveryResult NotConfigured() =>
        new(NotificationDeliveryStatus.NotConfigured);

    public static NotificationDeliveryResult Failed() =>
        new(NotificationDeliveryStatus.Failed);
}

public interface INotificationSender
{
    Task<NotificationDeliveryResult> SendAsync(
        NotificationMessage message,
        CancellationToken cancellationToken = default);
}
