using FoundationKit.Notifications;
using FoundationKit.Notifications.Smtp;
using Xunit;

namespace FoundationKit.Tests;

public sealed class SmtpNotificationProviderTests
{
    [Fact]
    public async Task Missing_transport_configuration_returns_not_configured_and_observes_purpose()
    {
        var observer = new TestObserver();
        var sender = new SmtpNotificationSender(
            new SmtpNotificationOptions(),
            observer);
        var message = NotificationMessage.Create(
            "user@example.com",
            "Notice",
            "Body",
            "account.notice");

        var result = await sender.SendAsync(message);

        Assert.Equal(NotificationDeliveryStatus.NotConfigured, result.Status);
        Assert.Equal("account.notice", observer.NotConfiguredPurpose);
        Assert.Null(observer.FailedPurpose);
        Assert.Null(observer.ErrorType);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(65536)]
    public void Invalid_port_is_rejected(int port)
    {
        var options = new SmtpNotificationOptions { Port = port };

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            SmtpNotificationOptionsValidator.Validate(options));
    }

    [Fact]
    public void Control_characters_in_transport_identifiers_are_rejected()
    {
        var options = new SmtpNotificationOptions
        {
            Host = "smtp.example.com\nattacker",
            Port = 587
        };

        Assert.Throws<ArgumentException>(() =>
            SmtpNotificationOptionsValidator.Validate(options));
    }

    [Fact]
    public async Task Provider_snapshots_options_at_construction()
    {
        var options = new SmtpNotificationOptions();
        var sender = new SmtpNotificationSender(options);
        options.Host = "smtp.example.com";
        options.FromAddress = "sender@example.com";

        var result = await sender.SendAsync(NotificationMessage.Create(
            "user@example.com",
            "Notice",
            "Body",
            "account.notice"));

        Assert.Equal(NotificationDeliveryStatus.NotConfigured, result.Status);
    }

    [Fact]
    public async Task Provider_reports_only_error_type_to_observer_on_format_failure()
    {
        var observer = new TestObserver();
        var sender = new SmtpNotificationSender(
            new SmtpNotificationOptions
            {
                Host = "localhost",
                Port = 25,
                FromAddress = "@"
            },
            observer);
        var message = NotificationMessage.Create(
            "user@example.com",
            "Notice",
            "Sensitive body",
            "account.notice");

        var result = await sender.SendAsync(message);

        Assert.Equal(NotificationDeliveryStatus.Failed, result.Status);
        Assert.Equal("account.notice", observer.FailedPurpose);
        Assert.Equal(nameof(FormatException), observer.ErrorType);
    }

    [Fact]
    public async Task Cancellation_is_preserved_before_transport_work()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var sender = new SmtpNotificationSender(new SmtpNotificationOptions());

        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await sender.SendAsync(
                NotificationMessage.Create(
                    "user@example.com",
                    "Notice",
                    "Body",
                    "account.notice"),
                cancellation.Token));
    }

    private sealed class TestObserver : ISmtpNotificationObserver
    {
        public string? NotConfiguredPurpose { get; private set; }

        public string? FailedPurpose { get; private set; }

        public string? ErrorType { get; private set; }

        public void NotConfigured(string purpose) =>
            NotConfiguredPurpose = purpose;

        public void DeliveryFailed(string purpose, string errorType)
        {
            FailedPurpose = purpose;
            ErrorType = errorType;
        }
    }
}
