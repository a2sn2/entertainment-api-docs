using FoundationKit.Application.Abstractions;
using FoundationKit.Auditing;
using FoundationKit.Notifications;
using Madar.Application.Security;
using Madar.Domain.Cases;

namespace Madar.Application.Cases;

public interface ICaseNotificationCoordinator
{
    Task NotifyAssignmentAsync(
        Guid caseId,
        Guid assigneeUserId,
        CancellationToken cancellationToken = default);

    Task NotifyReassignmentAsync(
        Guid caseId,
        Guid assigneeUserId,
        CancellationToken cancellationToken = default) =>
        NotifyAssignmentAsync(caseId, assigneeUserId, cancellationToken);

    Task NotifyApprovalDecisionAsync(
        Guid caseId,
        Guid requesterUserId,
        string decision,
        CancellationToken cancellationToken = default);

    Task NotifyResolutionAsync(
        Guid caseId,
        Guid creatorUserId,
        CancellationToken cancellationToken = default);
}

public static class MadarNotificationPurposes
{
    public const string CaseAssigned = "madar.case.assigned";
    public const string CaseReassigned = "madar.case.reassigned";
    public const string CaseApprovalDecided = "madar.case.approval-decided";
    public const string CaseResolved = "madar.case.resolved";
}

public sealed class CaseNotificationCoordinator(
    IUserDirectory userDirectory,
    INotificationSender sender,
    IAuditRecorder auditRecorder,
    IUnitOfWork unitOfWork) : ICaseNotificationCoordinator
{
    public Task NotifyAssignmentAsync(
        Guid caseId,
        Guid assigneeUserId,
        CancellationToken cancellationToken = default) =>
        SendAsync(
            caseId,
            assigneeUserId,
            MadarNotificationPurposes.CaseAssigned,
            "تم إسناد حالة إليك في مدار",
            $"تم إسناد الحالة {caseId:D} إليك. افتح مدار لمراجعة التفاصيل.",
            cancellationToken);

    public Task NotifyReassignmentAsync(
        Guid caseId,
        Guid assigneeUserId,
        CancellationToken cancellationToken = default) =>
        SendAsync(
            caseId,
            assigneeUserId,
            MadarNotificationPurposes.CaseReassigned,
            "تمت إعادة إسناد حالة إليك في مدار",
            $"تمت إعادة إسناد الحالة {caseId:D} إليك. افتح مدار لمراجعة التفاصيل.",
            cancellationToken);

    public Task NotifyApprovalDecisionAsync(
        Guid caseId,
        Guid requesterUserId,
        string decision,
        CancellationToken cancellationToken = default)
    {
        var decisionText = decision switch
        {
            CaseApprovalStatuses.Approved => "تم اعتماد الطلب.",
            CaseApprovalStatuses.Rejected => "تم رفض الطلب.",
            _ => "تم اتخاذ قرار على الطلب."
        };

        return SendAsync(
            caseId,
            requesterUserId,
            MadarNotificationPurposes.CaseApprovalDecided,
            "تم اتخاذ قرار على طلب اعتماد في مدار",
            $"{decisionText} الحالة: {caseId:D}. افتح مدار لمراجعة التفاصيل.",
            cancellationToken);
    }

    public Task NotifyResolutionAsync(
        Guid caseId,
        Guid creatorUserId,
        CancellationToken cancellationToken = default) =>
        SendAsync(
            caseId,
            creatorUserId,
            MadarNotificationPurposes.CaseResolved,
            "تم حل حالة في مدار",
            $"تم تسجيل حل للحالة {caseId:D}. افتح مدار لمراجعة التفاصيل.",
            cancellationToken);

    private async Task SendAsync(
        Guid caseId,
        Guid targetUserId,
        string purpose,
        string title,
        string body,
        CancellationToken cancellationToken)
    {
        var destination = await userDirectory.GetNotificationDestinationAsync(
            targetUserId,
            cancellationToken);

        NotificationDeliveryResult delivery;
        if (string.IsNullOrWhiteSpace(destination))
        {
            delivery = NotificationDeliveryResult.NotConfigured();
        }
        else
        {
            var message = NotificationMessage.Create(
                destination,
                title,
                body,
                purpose);
            delivery = await sender.SendAsync(message, cancellationToken);
        }

        await auditRecorder.RecordAsync(
            new AuditRequest(
                "madar.case.notification-delivery",
                nameof(Case),
                caseId.ToString("D"),
                Attributes: new Dictionary<string, string>
                {
                    ["purpose"] = purpose,
                    ["targetUserId"] = targetUserId.ToString("D"),
                    ["deliveryStatus"] = delivery.Status.ToString().ToLowerInvariant()
                }),
            cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
