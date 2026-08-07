using FoundationKit.Notifications;
using Xunit;

namespace FoundationKit.Tests;

public sealed class NotificationsCapabilityTests
{
    [Fact]
    public void Message_normalizes_bounded_fields_without_exposing_sensitive_content_in_ToString()
    {
        var message = NotificationMessage.Create(
            " user@example.com ",
            " Security notice ",
            "Token: super-secret-token",
            " account.security.password-reset ");

        Assert.Equal("user@example.com", message.Destination);
        Assert.Equal("Security notice", message.Title);
        Assert.Equal("Token: super-secret-token", message.Body);
        Assert.Equal("account.security.password-reset", message.Purpose);

        var display = message.ToString();
        Assert.Contains("account.security.password-reset", display, StringComparison.Ordinal);
        Assert.DoesNotContain("user@example.com", display, StringComparison.Ordinal);
        Assert.DoesNotContain("super-secret-token", display, StringComparison.Ordinal);
    }

    [Fact]
    public void Message_allows_line_breaks_in_body_but_rejects_control_characters_in_destination()
    {
        var message = NotificationMessage.Create(
            "user@example.com",
            "Notice",
            "Line one\nLine two\tvalue",
            "account.notice");

        Assert.Contains("Line two", message.Body, StringComparison.Ordinal);

        Assert.Throws<ArgumentException>(() =>
            NotificationMessage.Create(
                "user@example.com\nBcc: attacker@example.com",
                "Notice",
                "Body",
                "account.notice"));
    }

    [Theory]
    [InlineData("account notice")]
    [InlineData("account/notice")]
    [InlineData("account@notice")]
    public void Purpose_rejects_unbounded_or_unsafe_code_shapes(string purpose)
    {
        Assert.Throws<ArgumentException>(() =>
            NotificationMessage.Create(
                "user@example.com",
                "Notice",
                "Body",
                purpose));
    }

    [Fact]
    public void Delivery_result_exposes_only_transport_status()
    {
        Assert.True(NotificationDeliveryResult.Delivered().IsDelivered);
        Assert.False(NotificationDeliveryResult.NotConfigured().IsDelivered);
        Assert.False(NotificationDeliveryResult.Failed().IsDelivered);
        Assert.Equal(
            NotificationDeliveryStatus.Failed,
            NotificationDeliveryResult.Failed().Status);
    }
}
