using FoundationKit.Application.Results;
using FoundationKit.Domain.Primitives;
using FoundationKit.Workflow;

namespace Madar.Domain.Cases;

public static class CaseApprovalStatuses
{
    public const string Pending = "pending";
    public const string Approved = "approved";
    public const string Rejected = "rejected";
}

public static class CaseApprovalRequirement
{
    public static bool IsRequired(string? caseType) =>
        caseType is CaseTypes.AccessRequest or CaseTypes.ComplianceCase;
}

public static class CaseApprovalWorkflow
{
    public const string WorkflowId = "madar.case-approval";

    public static WorkflowDefinition Definition { get; } = new(
        WorkflowId,
        [
            new WorkflowTransitionDefinition(
                "approve-pending-case-review",
                CaseApprovalStatuses.Pending,
                "approve",
                CaseApprovalStatuses.Approved),
            new WorkflowTransitionDefinition(
                "reject-pending-case-review",
                CaseApprovalStatuses.Pending,
                "reject",
                CaseApprovalStatuses.Rejected)
        ]);
}

public sealed class CaseApproval : Entity<Guid>
{
    private CaseApproval()
    {
    }

    private CaseApproval(
        Guid id,
        Guid caseId,
        Guid requestedByUserId,
        DateTimeOffset requestedUtc)
        : base(id)
    {
        CaseId = caseId;
        RequestedByUserId = requestedByUserId;
        RequestedUtc = requestedUtc;
        Status = CaseApprovalStatuses.Pending;
    }

    public Guid CaseId { get; private set; }

    public Guid RequestedByUserId { get; private set; }

    public DateTimeOffset RequestedUtc { get; private set; }

    public string Status { get; private set; } = CaseApprovalStatuses.Pending;

    public Guid? ReviewedByUserId { get; private set; }

    public DateTimeOffset? DecidedUtc { get; private set; }

    public string? DecisionNotes { get; private set; }

    public byte[] RowVersion { get; private set; } = [];

    public static Result<CaseApproval> Create(
        Guid caseId,
        Guid requestedByUserId,
        DateTimeOffset requestedUtc)
    {
        if (caseId == Guid.Empty)
            return Result<CaseApproval>.Failure(CaseApprovalErrors.InvalidCase);

        if (requestedByUserId == Guid.Empty)
            return Result<CaseApproval>.Failure(CaseApprovalErrors.InvalidRequester);

        return Result<CaseApproval>.Success(
            new CaseApproval(
                Guid.NewGuid(),
                caseId,
                requestedByUserId,
                requestedUtc));
    }

    public Result Decide(
        Guid reviewedByUserId,
        string? decision,
        string? notes,
        DateTimeOffset decidedUtc)
    {
        if (reviewedByUserId == Guid.Empty)
            return Result.Failure(CaseApprovalErrors.InvalidReviewer);

        if (reviewedByUserId == RequestedByUserId)
            return Result.Failure(CaseApprovalErrors.SelfReviewNotAllowed);

        if (!CaseApprovalWorkflow.Definition.TryResolve(
                Status,
                NormalizeCode(decision),
                out var transition))
        {
            return Result.Failure(CaseApprovalErrors.InvalidDecision);
        }

        var normalizedNotes = NormalizeNotes(notes);
        if (normalizedNotes.Length > 1000)
            return Result.Failure(CaseApprovalErrors.InvalidNotes);

        if (decidedUtc < RequestedUtc)
            return Result.Failure(CaseApprovalErrors.InvalidDecisionTime);

        Status = transition.ToState;
        ReviewedByUserId = reviewedByUserId;
        DecidedUtc = decidedUtc;
        DecisionNotes = normalizedNotes.Length == 0 ? null : normalizedNotes;

        return Result.Success();
    }

    private static string NormalizeCode(string? value) =>
        value?.Trim().ToLowerInvariant() ?? string.Empty;

    private static string NormalizeNotes(string? value) => value?.Trim() ?? string.Empty;
}

public static class CaseApprovalErrors
{
    public static readonly Error InvalidCase = Error.Validation(
        "Madar.Approval.InvalidCase",
        "الحالة المرتبطة بطلب الاعتماد غير صالحة.");

    public static readonly Error InvalidRequester = Error.Validation(
        "Madar.Approval.InvalidRequester",
        "تعذر تحديد طالب الاعتماد.");

    public static readonly Error InvalidReviewer = Error.Forbidden(
        "Madar.Approval.InvalidReviewer",
        "تعذر تحديد منفذ قرار الاعتماد.");

    public static readonly Error SelfReviewNotAllowed = Error.Forbidden(
        "Madar.Approval.SelfReviewNotAllowed",
        "لا يمكن لطالب الاعتماد اتخاذ القرار على طلبه نفسه.");

    public static readonly Error InvalidDecision = Error.Conflict(
        "Madar.Approval.InvalidDecision",
        "قرار الاعتماد غير صالح أو أن الطلب لم يعد بانتظار القرار.");

    public static readonly Error InvalidNotes = Error.Validation(
        "Madar.Approval.InvalidNotes",
        "ملاحظات قرار الاعتماد يجب ألا تتجاوز 1000 حرف.");

    public static readonly Error InvalidDecisionTime = Error.Validation(
        "Madar.Approval.InvalidDecisionTime",
        "وقت قرار الاعتماد لا يمكن أن يسبق وقت طلب الاعتماد.");
}
