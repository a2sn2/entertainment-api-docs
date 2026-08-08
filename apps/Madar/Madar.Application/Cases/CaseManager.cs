using FoundationKit.Application.Abstractions;
using FoundationKit.Application.Persistence;
using FoundationKit.Application.Results;
using FoundationKit.Auditing;
using FoundationKit.Authorization;
using Madar.Application.Security;
using Madar.Contracts.Cases;
using Madar.Domain.Cases;

namespace Madar.Application.Cases;

public interface ICaseManager
{
    Task<Result<CaseDto>> CreateAsync(
        CreateCaseRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<CaseDto>> GetAsync(
        Guid caseId,
        CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<CaseDto>>> ListAsync(
        CancellationToken cancellationToken = default);

    Task<Result<CaseDto>> AssignAsync(
        Guid caseId,
        AssignCaseRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<CaseDto>> TransitionAsync(
        Guid caseId,
        TransitionCaseRequest request,
        CancellationToken cancellationToken = default);
}

public sealed class CaseManager(
    ICurrentUser currentUser,
    IAuthorizationEvaluator authorization,
    ICaseQueryService queryService,
    IRepository<Case, Guid> caseRepository,
    IUserDirectory userDirectory,
    ICaseSlaPolicy slaPolicy,
    IUnitOfWork unitOfWork,
    IAuditRecorder auditRecorder,
    IClock clock,
    ICaseApprovalQueryService? approvalQueryService = null,
    ICaseNotificationCoordinator? notificationCoordinator = null) : ICaseManager
{
    public async Task<Result<CaseDto>> CreateAsync(
        CreateCaseRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!TryGetCurrentUserId(out var userId))
            return Result<CaseDto>.Failure(CaseApplicationErrors.AuthenticationRequired);

        var createdUtc = clock.UtcNow;
        var slaDuration = slaPolicy.ResolveDuration(request.Priority);
        var slaTargetUtc = slaDuration.HasValue
            ? createdUtc.Add(slaDuration.Value)
            : (DateTimeOffset?)null;

        var creation = Case.Create(
            userId,
            request.Title,
            request.Description,
            request.CaseType,
            request.Priority,
            createdUtc,
            slaTargetUtc);

        if (creation.IsFailure)
            return Result<CaseDto>.Failure(creation.Error);

        await caseRepository.AddAsync(creation.Value, cancellationToken);

        var attributes = new Dictionary<string, string>
        {
            ["caseType"] = creation.Value.CaseType,
            ["priority"] = creation.Value.Priority
        };
        if (creation.Value.SlaTargetUtc.HasValue)
        {
            attributes["slaTargetUtc"] = creation.Value.SlaTargetUtc.Value
                .ToString("O", System.Globalization.CultureInfo.InvariantCulture);
        }

        await auditRecorder.RecordAsync(
            new AuditRequest(
                "madar.case.created",
                nameof(Case),
                creation.Value.Id.ToString("D"),
                Attributes: attributes),
            cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return await ReloadAsync(creation.Value.Id, cancellationToken);
    }

    public async Task<Result<CaseDto>> GetAsync(
        Guid caseId,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetCurrentUserId(out var userId))
            return Result<CaseDto>.Failure(CaseApplicationErrors.AuthenticationRequired);

        var item = await caseRepository.GetByIdAsync(caseId, cancellationToken);
        if (item is null || !CanRead(item, userId))
            return Result<CaseDto>.Failure(CaseApplicationErrors.CaseNotFound);

        var response = await queryService.GetByIdAsync(caseId, cancellationToken);
        return response is null
            ? Result<CaseDto>.Failure(CaseApplicationErrors.CaseNotFound)
            : Result<CaseDto>.Success(response);
    }

    public async Task<Result<IReadOnlyList<CaseDto>>> ListAsync(
        CancellationToken cancellationToken = default)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Result<IReadOnlyList<CaseDto>>.Failure(
                CaseApplicationErrors.AuthenticationRequired);
        }

        var items = authorization.HasPermission(MadarPermissions.ReadAllCases)
            ? await queryService.ListAllAsync(cancellationToken)
            : await queryService.ListForUserAsync(userId, cancellationToken);

        return Result<IReadOnlyList<CaseDto>>.Success(items);
    }

    public async Task<Result<CaseDto>> AssignAsync(
        Guid caseId,
        AssignCaseRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!TryGetCurrentUserId(out var userId))
            return Result<CaseDto>.Failure(CaseApplicationErrors.AuthenticationRequired);

        if (!authorization.HasPermission(MadarPermissions.AssignCases))
            return Result<CaseDto>.Failure(CaseApplicationErrors.AssignmentForbidden);

        if (!await userDirectory.IsAssignableOperatorAsync(
                request.AssigneeUserId,
                cancellationToken))
        {
            return Result<CaseDto>.Failure(CaseApplicationErrors.AssigneeNotEligible);
        }

        var item = await caseRepository.GetByIdAsync(caseId, cancellationToken);
        if (item is null)
            return Result<CaseDto>.Failure(CaseApplicationErrors.CaseNotFound);

        var assignment = item.Assign(request.AssigneeUserId, userId, clock.UtcNow);
        if (assignment.IsFailure)
            return Result<CaseDto>.Failure(assignment.Error);

        await auditRecorder.RecordAsync(
            new AuditRequest(
                "madar.case.assigned",
                nameof(Case),
                item.Id.ToString("D"),
                Attributes: new Dictionary<string, string>
                {
                    ["assigneeUserId"] = request.AssigneeUserId.ToString("D")
                }),
            cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        if (notificationCoordinator is not null)
        {
            await notificationCoordinator.NotifyAssignmentAsync(
                item.Id,
                request.AssigneeUserId,
                cancellationToken);
        }

        return await ReloadAsync(item.Id, cancellationToken);
    }

    public async Task<Result<CaseDto>> TransitionAsync(
        Guid caseId,
        TransitionCaseRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!TryGetCurrentUserId(out var userId))
            return Result<CaseDto>.Failure(CaseApplicationErrors.AuthenticationRequired);

        var item = await caseRepository.GetByIdAsync(caseId, cancellationToken);
        if (item is null)
            return Result<CaseDto>.Failure(CaseApplicationErrors.CaseNotFound);

        var trigger = request.Trigger?.Trim().ToLowerInvariant() ?? string.Empty;
        var changedUtc = clock.UtcNow;
        Result transition;

        switch (trigger)
        {
            case CaseTriggers.StartProgress:
            case CaseTriggers.Resolve:
                if (item.AssignedToUserId != userId
                    && !authorization.HasPermission(MadarPermissions.ProgressAnyCase))
                {
                    return Result<CaseDto>.Failure(CaseApplicationErrors.ProgressForbidden);
                }

                if (trigger == CaseTriggers.Resolve
                    && CaseApprovalRequirement.IsRequired(item.CaseType))
                {
                    var latestApproval = approvalQueryService is null
                        ? null
                        : await approvalQueryService.GetLatestForCaseAsync(
                            item.Id,
                            cancellationToken);
                    if (latestApproval?.Status != CaseApprovalStatuses.Approved)
                    {
                        return Result<CaseDto>.Failure(
                            CaseApprovalApplicationErrors.ApprovalRequired);
                    }
                }

                transition = trigger == CaseTriggers.StartProgress
                    ? item.StartProgress(userId, changedUtc)
                    : item.Resolve(userId, changedUtc);
                break;

            case CaseTriggers.Close:
                if (!authorization.HasPermission(MadarPermissions.CloseCases))
                    return Result<CaseDto>.Failure(CaseApplicationErrors.CloseForbidden);

                transition = item.Close(userId, changedUtc);
                break;

            default:
                return Result<CaseDto>.Failure(CaseApplicationErrors.InvalidTrigger);
        }

        if (transition.IsFailure)
            return Result<CaseDto>.Failure(transition.Error);

        if (trigger == CaseTriggers.Resolve && item.EvaluateSla(changedUtc))
            await RecordSlaBreachAsync(item, changedUtc, cancellationToken);

        await auditRecorder.RecordAsync(
            new AuditRequest(
                "madar.case.transitioned",
                nameof(Case),
                item.Id.ToString("D"),
                Attributes: new Dictionary<string, string>
                {
                    ["trigger"] = trigger,
                    ["status"] = item.Status
                }),
            cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        if (trigger == CaseTriggers.Resolve
            && item.CreatedByUserId != userId
            && notificationCoordinator is not null)
        {
            await notificationCoordinator.NotifyResolutionAsync(
                item.Id,
                item.CreatedByUserId,
                cancellationToken);
        }

        return await ReloadAsync(item.Id, cancellationToken);
    }

    private async Task RecordSlaBreachAsync(
        Case item,
        DateTimeOffset evaluatedUtc,
        CancellationToken cancellationToken)
    {
        await auditRecorder.RecordAsync(
            new AuditRequest(
                "madar.case.sla-breached",
                nameof(Case),
                item.Id.ToString("D"),
                Attributes: new Dictionary<string, string>
                {
                    ["priority"] = item.Priority,
                    ["slaTargetUtc"] = item.SlaTargetUtc!.Value.ToString(
                        "O",
                        System.Globalization.CultureInfo.InvariantCulture),
                    ["escalatedUtc"] = evaluatedUtc.ToString(
                        "O",
                        System.Globalization.CultureInfo.InvariantCulture)
                }),
            cancellationToken);
    }

    private bool TryGetCurrentUserId(out Guid userId)
    {
        userId = currentUser.UserId ?? Guid.Empty;
        return currentUser.IsAuthenticated && userId != Guid.Empty;
    }

    private bool CanRead(Case item, Guid userId) =>
        item.CreatedByUserId == userId
        || item.AssignedToUserId == userId
        || authorization.HasPermission(MadarPermissions.ReadAllCases);

    private async Task<Result<CaseDto>> ReloadAsync(
        Guid caseId,
        CancellationToken cancellationToken)
    {
        var response = await queryService.GetByIdAsync(caseId, cancellationToken);
        return response is null
            ? Result<CaseDto>.Failure(CaseApplicationErrors.CaseNotFound)
            : Result<CaseDto>.Success(response);
    }
}

public static class CaseApplicationErrors
{
    public static readonly Error AuthenticationRequired = Error.Unauthorized(
        "Madar.AuthenticationRequired",
        "يجب تسجيل الدخول لتنفيذ هذه العملية.");

    public static readonly Error CaseNotFound = Error.NotFound(
        "Madar.CaseNotFound",
        "الحالة غير موجودة أو لا تملك صلاحية الوصول إليها.");

    public static readonly Error AssignmentForbidden = Error.Forbidden(
        "Madar.AssignmentForbidden",
        "لا تملك صلاحية إسناد الحالات.");

    public static readonly Error AssigneeNotEligible = Error.Validation(
        "Madar.AssigneeNotEligible",
        "المستخدم المحدد غير مؤهل لاستلام الحالات التشغيلية.");

    public static readonly Error ProgressForbidden = Error.Forbidden(
        "Madar.ProgressForbidden",
        "لا تملك صلاحية معالجة هذه الحالة.");

    public static readonly Error CloseForbidden = Error.Forbidden(
        "Madar.CloseForbidden",
        "لا تملك صلاحية إغلاق الحالات.");

    public static readonly Error InvalidTrigger = Error.Validation(
        "Madar.InvalidTrigger",
        "الإجراء المطلوب على الحالة غير مدعوم.");
}
