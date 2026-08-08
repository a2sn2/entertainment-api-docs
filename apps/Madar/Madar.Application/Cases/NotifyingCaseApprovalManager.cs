using FoundationKit.Application.Results;
using Madar.Contracts.Cases;

namespace Madar.Application.Cases;

public sealed class NotifyingCaseApprovalManager(
    CaseApprovalManager inner,
    ICaseNotificationCoordinator notificationCoordinator) : ICaseApprovalManager
{
    public Task<Result<IReadOnlyList<CaseApprovalDto>>> ListAsync(
        Guid caseId,
        CancellationToken cancellationToken = default) =>
        inner.ListAsync(caseId, cancellationToken);

    public Task<Result<CaseApprovalDto>> RequestAsync(
        Guid caseId,
        CancellationToken cancellationToken = default) =>
        inner.RequestAsync(caseId, cancellationToken);

    public async Task<Result<CaseApprovalDto>> DecideAsync(
        Guid caseId,
        Guid approvalId,
        DecideCaseApprovalRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await inner.DecideAsync(
            caseId,
            approvalId,
            request,
            cancellationToken);

        if (result.IsSuccess)
        {
            await notificationCoordinator.NotifyApprovalDecisionAsync(
                caseId,
                result.Value.RequestedByUserId,
                result.Value.Status,
                cancellationToken);
        }

        return result;
    }
}
