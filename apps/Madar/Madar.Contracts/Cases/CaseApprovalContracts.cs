namespace Madar.Contracts.Cases;

public sealed record RequestCaseApprovalRequest;

public sealed record DecideCaseApprovalRequest(
    string Decision,
    string? Notes);

public sealed record CaseApprovalDto(
    Guid Id,
    Guid CaseId,
    Guid RequestedByUserId,
    string RequestedByDisplayName,
    DateTimeOffset RequestedUtc,
    string Status,
    Guid? ReviewedByUserId,
    string? ReviewedByDisplayName,
    DateTimeOffset? DecidedUtc,
    string? DecisionNotes);

public static class CaseApprovalRoutes
{
    public static string ForCase(Guid caseId) =>
        $"{CaseRoutes.ById(caseId)}/approvals";

    public static string Decision(Guid caseId, Guid approvalId) =>
        $"{ForCase(caseId)}/{approvalId:D}/decision";
}
