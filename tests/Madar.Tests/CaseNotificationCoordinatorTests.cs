using FoundationKit.Application.Abstractions;
using FoundationKit.Auditing;
using FoundationKit.Notifications;
using Madar.Application.Cases;
using Madar.Application.Security;
using Xunit;

namespace Madar.Tests;

public sealed class CaseNotificationCoordinatorTests
{
    [Fact]
    public async Task Assignment_Delivered_UsesDestinationWithoutAuditingSensitiveContent()
    {
        var caseId = Guid.NewGuid();
        var targetUserId = Guid.NewGuid();
        const string destination = "operator@example.test";
        var sender = new RecordingNotificationSender(
            NotificationDeliveryResult.Delivered());
        var audit = new RecordingAuditRecorder();
        var unitOfWork = new RecordingUnitOfWork();
        var coordinator = new CaseNotificationCoordinator(
            new NotificationUserDirectory(targetUserId, destination),
            sender,
            audit,
            unitOfWork);

        await coordinator.NotifyAssignmentAsync(caseId, targetUserId);

        var message = Assert.Single(sender.Messages);
        Assert.Equal(destination, message.Destination);
        Assert.Equal(MadarNotificationPurposes.CaseAssigned, message.Purpose);

        var auditEvent = Assert.Single(audit.Events);
        Assert.Equal("madar.case.notification-delivery", auditEvent.Action);
        Assert.Equal(MadarNotificationPurposes.CaseAssigned, auditEvent.Attributes["purpose"]);
        Assert.Equal(targetUserId.ToString("D"), auditEvent.Attributes["targetUserId"]);
        Assert.Equal("delivered", auditEvent.Attributes["deliveryStatus"]);
        Assert.DoesNotContain(
            auditEvent.Attributes.Values,
            value => value.Contains(destination, StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(
            auditEvent.Attributes.Values,
            value => value.Contains(message.Body, StringComparison.Ordinal));
        Assert.Equal(1, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task Assignment_MissingDestination_IsNotConfiguredWithoutCallingProvider()
    {
        var caseId = Guid.NewGuid();
        var targetUserId = Guid.NewGuid();
        var sender = new RecordingNotificationSender(
            NotificationDeliveryResult.Delivered());
        var audit = new RecordingAuditRecorder();
        var coordinator = new CaseNotificationCoordinator(
            new NotificationUserDirectory(targetUserId, null),
            sender,
            audit,
            new RecordingUnitOfWork());

        await coordinator.NotifyAssignmentAsync(caseId, targetUserId);

        Assert.Empty(sender.Messages);
        var auditEvent = Assert.Single(audit.Events);
        Assert.Equal("notconfigured", auditEvent.Attributes["deliveryStatus"]);
    }

    [Fact]
    public async Task ApprovalDecision_ProviderFailure_IsRecordedAsBoundedStatus()
    {
        var caseId = Guid.NewGuid();
        var targetUserId = Guid.NewGuid();
        const string destination = "requester@example.test";
        var sender = new RecordingNotificationSender(
            NotificationDeliveryResult.Failed());
        var audit = new RecordingAuditRecorder();
        var coordinator = new CaseNotificationCoordinator(
            new NotificationUserDirectory(targetUserId, destination),
            sender,
            audit,
            new RecordingUnitOfWork());

        await coordinator.NotifyApprovalDecisionAsync(
            caseId,
            targetUserId,
            "approved");

        Assert.Single(sender.Messages);
        var auditEvent = Assert.Single(audit.Events);
        Assert.Equal(MadarNotificationPurposes.CaseApprovalDecided, auditEvent.Attributes["purpose"]);
        Assert.Equal("failed", auditEvent.Attributes["deliveryStatus"]);
        Assert.DoesNotContain(
            auditEvent.Attributes.Values,
            value => value.Contains(destination, StringComparison.OrdinalIgnoreCase));
    }

    private sealed class NotificationUserDirectory(
        Guid targetUserId,
        string? destination) : IUserDirectory
    {
        public Task<bool> ExistsAsync(
            Guid userId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(userId == targetUserId);

        public Task<string?> GetNotificationDestinationAsync(
            Guid userId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(userId == targetUserId ? destination : null);
    }

    private sealed class RecordingNotificationSender(
        NotificationDeliveryResult result) : INotificationSender
    {
        public List<NotificationMessage> Messages { get; } = [];

        public Task<NotificationDeliveryResult> SendAsync(
            NotificationMessage message,
            CancellationToken cancellationToken = default)
        {
            Messages.Add(message);
            return Task.FromResult(result);
        }
    }

    private sealed class RecordingAuditRecorder : IAuditRecorder
    {
        public List<AuditEvent> Events { get; } = [];

        public ValueTask<AuditEvent> RecordAsync(
            AuditRequest request,
            CancellationToken cancellationToken = default)
        {
            var auditEvent = AuditEvent.Create(
                request,
                AuditContext.Empty,
                new DateTimeOffset(2026, 8, 8, 13, 0, 0, TimeSpan.Zero));
            Events.Add(auditEvent);
            return ValueTask.FromResult(auditEvent);
        }
    }

    private sealed class RecordingUnitOfWork : IUnitOfWork
    {
        public int SaveCount { get; private set; }

        public Task<int> SaveChangesAsync(
            CancellationToken cancellationToken = default)
        {
            SaveCount++;
            return Task.FromResult(1);
        }
    }
}
