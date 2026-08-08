using FoundationKit.Application.Abstractions;
using FoundationKit.Application.Persistence;
using FoundationKit.Application.Results;
using FoundationKit.Approvals;
using FoundationKit.Auditing;
using FoundationKit.Authorization;
using Madar.Application.Security;
using Madar.Contracts.Cases;
using Madar.Domain.Cases;

namespace Madar.Application.Cases;

public interface ICaseApprovalManager
{
    Task<Result<IReadOnlyList<CaseApprovalDto>>> ListAsync(
        Guid caseId,
        CancellationToken cancellationToken = default);

    Task<Result<CaseApprovalDto>> RequestAsync(
        Guid caseId,
        CancellationToken cancellationToken = default);

    Task<Result<CaseApprovalDto>> DecideAsync(
        Guid caseId,
        Guid approvalId,
        DecideCaseApprovalRequest request,
        CancellationToken cancellationToken = default);
}

public sealed class CaseApprovalManager(
    ICurrentUser currentUser,
    IAuthorizationEvaluator authorization,
    IRepository<Case, Guid> caseRepository,
    ICaseApprovalRepository approvalRepository,
    ICaseApprovalQueryService approvalQueryService,
    IUnitOfWork unitOfWork,
    IAuditRecorder auditRecorder,
    IClock clock) : ICaseApprovalManager
{
    public async Task<Result<IReadOnlyList<CaseApprovalDto>>> ListAsync(
        Guid caseId,
        CancellationToken cancellationToken = default)
    {
        var access = await AuthorizeCaseReadAsync(caseId, cancellationToken);
        if (access.IsFailure)
            return Result<IReadOnlyList<CaseApprovalDto>>.Failure(access.Error);

        var approvals = await approvalQueryService.ListForCaseAsync(
            caseId,
            cancellationToken);
        return Result<IReadOnlyList<CaseApprovalDto>>.Success(approvals);
    }

    public async Task<Result<CaseApprovalDto>> RequestAsync(
        Guid caseId,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetCurrentUserId(out var userId))
            return Result<CaseApprovalDto>.Failure(CaseApplicationErrors.AuthenticationRequired);

        var item = await caseRepository.GetByIdAsync(caseId, cancellationToken);
        if (item is null || !CaseAccessRules.CanRead(item, userId, authorization))
            return Result<CaseApprovalDto>.Failure(CaseApplicationErrors.CaseNotFound);

        if (!CaseApprovalRequirement.IsRequired(item.CaseType))
            return Result<CaseApprovalDto>.Failure(CaseApprovalApplicationErrors.ApprovalNotRequired);

        if (item.Status != CaseStatuses.InProgress)
            return Result<CaseApprovalDto>.Failure(CaseApprovalApplicationErrors.InvalidRequestState);

        if (item.AssignedToUserId != userId
            && !authorization.HasPermission(MadarPermissions.ProgressAnyCase))
        {
            return Result<CaseApprovalDto>.Failure(CaseApplicationErrors.ProgressForbidden);
        }

        var latest = await approvalQueryService.GetLatestForCaseAsync(
            caseId,
            cancellationToken);
        if (latest?.Status == CaseApprovalStatuses.Pending)
            return Result<CaseApprovalDto>.Failure(CaseApprovalApplicationErrors.AlreadyPending);

        if (latest?.Status == CaseApprovalStatuses.Approved)
            return Result<CaseApprovalDto>.Failure(CaseApprovalApplicationErrors.AlreadyApproved);

        var creation = CaseApproval.Create(caseId, userId, clock.UtcNow);
        if (creation.IsFailure)
            return Result<CaseApprovalDto>.Failure(creation.Error);

        await approvalRepository.AddAsync(creation.Value, cancellationToken);
        await auditRecorder.RecordAsync(
            new AuditRequest(
                "madar.case.approval-requested",
                nameof(Case),
                caseId.ToString("D"),
                Attributes: new Dictionary<string, string>
                {
                    ["approvalId"] = creation.Value.Id.ToString("D")
                }),
            cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return await ReloadAsync(creation.Value.Id, cancellationToken);
    }

    public async Task<Result<CaseApprovalDto>> DecideAsync(
        Guid caseId,
        Guid approvalId,
        DecideCaseApprovalRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!TryGetCurrentUserId(out var userId))
            return Result<CaseApprovalDto>.Failure(CaseApplicationErrors.AuthenticationRequired);

        if (!ApprovalPolicy.HasDecisionPermission(
                authorization,
                MadarPermissions.ApproveCases))
        {
            return Result<CaseApprovalDto>.Failure(
                CaseApprovalApplicationErrors.DecisionForbidden);
        }

        var item = await caseRepository.GetByIdAsync(caseId, cancellationToken);
        if (item is null || !CaseAccessRules.CanRead(item, userId, authorization))
            return Result<CaseApprovalDto>.Failure(CaseApplicationErrors.CaseNotFound);

        var approval = await approvalRepository.GetByIdAsync(
            approvalId,
            cancellationToken);
        if (approval is null || approval.CaseId != caseId)
            return Result<CaseApprovalDto>.Failure(CaseApprovalApplicationErrors.ApprovalNotFound);

        var eligibility = ApprovalPolicy.Evaluate(
            authorization,
            MadarPermissions.ApproveCases,
            approval.RequestedByUserId.ToString("D"),
            userId.ToString("D"));

        if (eligibility == ApprovalEligibility.PermissionDenied)
        {
            return Result<CaseApprovalDto>.Failure(
                CaseApprovalApplicationErrors.DecisionForbidden);
        }

        if (eligibility == ApprovalEligibility.MakerCheckerViolation)
        {
            return Result<CaseApprovalDto>.Failure(
                CaseApprovalApplicationErrors.SelfReviewNotAllowed);
        }

        if (!ApprovalDecisions.TryResolve(
                CaseApprovalWorkflow.Definition,
                approval.Status,
                request.Decision,
                out var resolution))
        {
            return Result<CaseApprovalDto>.Failure(
                CaseApprovalApplicationErrors.InvalidDecision);
        }

        var decision = approval.Decide(
            userId,
            resolution.DecisionToken,
            request.Notes,
            clock.UtcNow);
        if (decision.IsFailure)
            return Result<CaseApprovalDto>.Failure(decision.Error);

        await auditRecorder.RecordAsync(
            new AuditRequest(
                "madar.case.approval-decided",
                nameof(Case),
                caseId.ToString("D"),
                Attributes: new Dictionary<string, string>
                {
                    ["approvalId"] = approval.Id.ToString("D"),
                    ["decision"] = resolution.DecisionToken
                }),
            cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return await ReloadAsync(approval.Id, cancellationToken);
    }

    private async Task<Result> AuthorizeCaseReadAsync(
        Guid caseId,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
            return Result.Failure(CaseApplicationErrors.AuthenticationRequired);

        var item = await caseRepository.GetByIdAsync(caseId, cancellationToken);
        return item is null || !CaseAccessRules.CanRead(item, userId, authorization)
            ? Result.Failure(CaseApplicationErrors.CaseNotFound)
            : Result.Success();
    }

    private bool TryGetCurrentUserId(out Guid userId)
    {
        userId = currentUser.UserId ?? Guid.Empty;
        return currentUser.IsAuthenticated && userId != Guid.Empty;
    }

    private async Task<Result<CaseApprovalDto>> ReloadAsync(
        Guid approvalId,
        CancellationToken cancellationToken)
    {
        var response = await approvalQueryService.GetByIdAsync(
            approvalId,
            cancellationToken);
        return response is null
            ? Result<CaseApprovalDto>.Failure(CaseApprovalApplicationErrors.ApprovalNotFound)
            : Result<CaseApprovalDto>.Success(response);
    }
}

public static class CaseApprovalApplicationErrors
{
    public static readonly Error ApprovalNotRequired = Error.Validation(
        "Madar.Approval.NotRequired",
        "نوع هذه الحالة لا يتطلب اعتمادًا قبل تسجيل الحل.");

    public static readonly Error InvalidRequestState = Error.Conflict(
        "Madar.Approval.InvalidRequestState",
        "يمكن طلب الاعتماد فقط عندما تكون الحالة قيد المعالجة.");

    public static readonly Error AlreadyPending = Error.Conflict(
        "Madar.Approval.AlreadyPending",
        "يوجد طلب اعتماد قيد الانتظار لهذه الحالة.");

    public static readonly Error AlreadyApproved = Error.Conflict(
        "Madar.Approval.AlreadyApproved",
        "آخر طلب اعتماد لهذه الحالة معتمد بالفعل.");

    public static readonly Error DecisionForbidden = Error.Forbidden(
        "Madar.Approval.DecisionForbidden",
        "لا تملك صلاحية اتخاذ قرار الاعتماد.");

    public static readonly Error SelfReviewNotAllowed = Error.Forbidden(
        "Madar.Approval.SelfReviewNotAllowed",
        "لا يمكن لطالب الاعتماد اتخاذ القرار على طلبه نفسه.");

    public static readonly Error InvalidDecision = Error.Conflict(
        "Madar.Approval.InvalidDecision",
        "قرار الاعتماد غير صالح أو أن الطلب لم يعد بانتظار القرار.");

    public static readonly Error ApprovalNotFound = Error.NotFound(
        "Madar.Approval.NotFound",
        "طلب الاعتماد غير موجود أو لا ينتمي إلى هذه الحالة.");

    public static readonly Error ApprovalRequired = Error.Conflict(
        "Madar.Approval.Required",
        "يجب اعتماد الحالة قبل تسجيل الحل.");
}
