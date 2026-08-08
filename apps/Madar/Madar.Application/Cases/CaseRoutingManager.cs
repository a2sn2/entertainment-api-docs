using FoundationKit.Application.Abstractions;
using FoundationKit.Application.Persistence;
using FoundationKit.Application.Results;
using FoundationKit.Auditing;
using FoundationKit.Authorization;
using Madar.Application.Organization;
using Madar.Application.Security;
using Madar.Contracts.Cases;
using Madar.Contracts.Organization;
using Madar.Domain.Cases;

namespace Madar.Application.Cases;

public interface ICaseRoutingManager
{
    Task<Result<IReadOnlyList<DepartmentDto>>> ListDepartmentsAsync(
        CancellationToken cancellationToken = default);

    Task<Result<DepartmentQueueDto>> GetQueueAsync(
        Guid departmentId,
        CancellationToken cancellationToken = default);

    Task<Result<CaseDto>> RouteAsync(
        Guid caseId,
        RouteCaseRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<CaseDto>> TransferAsync(
        Guid caseId,
        TransferCaseRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<CaseDto>> ReassignAsync(
        Guid caseId,
        ReassignCaseRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<CaseDto>> ClaimAsync(
        Guid caseId,
        CancellationToken cancellationToken = default);
}

public sealed class CaseRoutingManager(
    ICurrentUser currentUser,
    IAuthorizationEvaluator authorization,
    IRepository<Case, Guid> caseRepository,
    ICaseQueryService queryService,
    IUserDirectory userDirectory,
    IDepartmentDirectory departmentDirectory,
    IUnitOfWork unitOfWork,
    IAuditRecorder auditRecorder,
    IClock clock,
    ICaseNotificationCoordinator? notificationCoordinator = null) : ICaseRoutingManager
{
    public async Task<Result<IReadOnlyList<DepartmentDto>>> ListDepartmentsAsync(
        CancellationToken cancellationToken = default)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Result<IReadOnlyList<DepartmentDto>>.Failure(
                CaseApplicationErrors.AuthenticationRequired);
        }

        var departments = authorization.HasPermission(MadarPermissions.ReadAllCases)
            ? await departmentDirectory.ListActiveAsync(cancellationToken)
            : await departmentDirectory.ListForUserAsync(userId, cancellationToken);

        return Result<IReadOnlyList<DepartmentDto>>.Success(departments);
    }

    public async Task<Result<DepartmentQueueDto>> GetQueueAsync(
        Guid departmentId,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetCurrentUserId(out var userId))
            return Result<DepartmentQueueDto>.Failure(CaseApplicationErrors.AuthenticationRequired);

        var department = await departmentDirectory.GetAsync(
            departmentId,
            cancellationToken);
        if (department is null || !department.IsActive)
            return Result<DepartmentQueueDto>.Failure(CaseRoutingErrors.DepartmentNotFound);

        var canReadAll = authorization.HasPermission(MadarPermissions.ReadAllCases);
        if (!canReadAll
            && !await departmentDirectory.IsMemberAsync(
                departmentId,
                userId,
                cancellationToken))
        {
            return Result<DepartmentQueueDto>.Failure(CaseRoutingErrors.QueueForbidden);
        }

        var cases = await queryService.ListDepartmentQueueAsync(
            departmentId,
            cancellationToken);
        return Result<DepartmentQueueDto>.Success(
            new DepartmentQueueDto(department, cases));
    }

    public async Task<Result<CaseDto>> RouteAsync(
        Guid caseId,
        RouteCaseRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!TryGetCurrentUserId(out var userId))
            return Result<CaseDto>.Failure(CaseApplicationErrors.AuthenticationRequired);

        if (!authorization.HasPermission(MadarPermissions.RouteCases))
            return Result<CaseDto>.Failure(CaseRoutingErrors.RouteForbidden);

        var department = await GetActiveDepartmentAsync(
            request.DepartmentId,
            cancellationToken);
        if (department is null)
            return Result<CaseDto>.Failure(CaseRoutingErrors.DepartmentNotFound);

        var item = await caseRepository.GetByIdAsync(caseId, cancellationToken);
        if (item is null)
            return Result<CaseDto>.Failure(CaseApplicationErrors.CaseNotFound);

        var routed = item.RouteToDepartment(
            department.Id,
            userId,
            clock.UtcNow);
        if (routed.IsFailure)
            return Result<CaseDto>.Failure(routed.Error);

        await auditRecorder.RecordAsync(
            new AuditRequest(
                "madar.case.routed",
                nameof(Case),
                item.Id.ToString("D"),
                Attributes: new Dictionary<string, string>
                {
                    ["departmentId"] = department.Id.ToString("D")
                }),
            cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return await ReloadAsync(item.Id, cancellationToken);
    }

    public async Task<Result<CaseDto>> TransferAsync(
        Guid caseId,
        TransferCaseRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!TryGetCurrentUserId(out var userId))
            return Result<CaseDto>.Failure(CaseApplicationErrors.AuthenticationRequired);

        if (!authorization.HasPermission(MadarPermissions.TransferCases))
            return Result<CaseDto>.Failure(CaseRoutingErrors.TransferForbidden);

        var department = await GetActiveDepartmentAsync(
            request.DepartmentId,
            cancellationToken);
        if (department is null)
            return Result<CaseDto>.Failure(CaseRoutingErrors.DepartmentNotFound);

        var item = await caseRepository.GetByIdAsync(caseId, cancellationToken);
        if (item is null)
            return Result<CaseDto>.Failure(CaseApplicationErrors.CaseNotFound);

        var previousDepartmentId = item.DepartmentId;
        var previousAssigneeUserId = item.AssignedToUserId;
        var previousStatus = item.Status;
        var transferred = item.TransferToDepartment(
            department.Id,
            userId,
            clock.UtcNow);
        if (transferred.IsFailure)
            return Result<CaseDto>.Failure(transferred.Error);

        var attributes = new Dictionary<string, string>
        {
            ["fromDepartmentId"] = previousDepartmentId!.Value.ToString("D"),
            ["toDepartmentId"] = department.Id.ToString("D"),
            ["previousStatus"] = previousStatus
        };
        if (previousAssigneeUserId.HasValue)
        {
            attributes["previousAssigneeUserId"] =
                previousAssigneeUserId.Value.ToString("D");
        }

        await auditRecorder.RecordAsync(
            new AuditRequest(
                "madar.case.transferred",
                nameof(Case),
                item.Id.ToString("D"),
                Attributes: attributes),
            cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return await ReloadAsync(item.Id, cancellationToken);
    }

    public async Task<Result<CaseDto>> ReassignAsync(
        Guid caseId,
        ReassignCaseRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!TryGetCurrentUserId(out var userId))
            return Result<CaseDto>.Failure(CaseApplicationErrors.AuthenticationRequired);

        if (!authorization.HasPermission(MadarPermissions.ReassignCases))
            return Result<CaseDto>.Failure(CaseRoutingErrors.ReassignmentForbidden);

        if (!await userDirectory.IsAssignableOperatorAsync(
                request.AssigneeUserId,
                cancellationToken))
        {
            return Result<CaseDto>.Failure(CaseApplicationErrors.AssigneeNotEligible);
        }

        var item = await caseRepository.GetByIdAsync(caseId, cancellationToken);
        if (item is null)
            return Result<CaseDto>.Failure(CaseApplicationErrors.CaseNotFound);

        if (item.DepartmentId.HasValue)
        {
            var department = await GetActiveDepartmentAsync(
                item.DepartmentId.Value,
                cancellationToken);
            if (department is null)
                return Result<CaseDto>.Failure(CaseRoutingErrors.DepartmentNotFound);

            if (!await departmentDirectory.IsMemberAsync(
                    item.DepartmentId.Value,
                    request.AssigneeUserId,
                    cancellationToken))
            {
                return Result<CaseDto>.Failure(CaseRoutingErrors.AssigneeOutsideDepartment);
            }
        }

        var previousAssigneeUserId = item.AssignedToUserId;
        var reassigned = item.Reassign(
            request.AssigneeUserId,
            userId,
            clock.UtcNow);
        if (reassigned.IsFailure)
            return Result<CaseDto>.Failure(reassigned.Error);

        var attributes = new Dictionary<string, string>
        {
            ["previousAssigneeUserId"] = previousAssigneeUserId!.Value.ToString("D"),
            ["assigneeUserId"] = request.AssigneeUserId.ToString("D"),
            ["status"] = item.Status
        };
        if (item.DepartmentId.HasValue)
            attributes["departmentId"] = item.DepartmentId.Value.ToString("D");

        await auditRecorder.RecordAsync(
            new AuditRequest(
                "madar.case.reassigned",
                nameof(Case),
                item.Id.ToString("D"),
                Attributes: attributes),
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

    public async Task<Result<CaseDto>> ClaimAsync(
        Guid caseId,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetCurrentUserId(out var userId))
            return Result<CaseDto>.Failure(CaseApplicationErrors.AuthenticationRequired);

        if (!authorization.HasPermission(MadarPermissions.ClaimCases)
            || !await userDirectory.IsAssignableOperatorAsync(userId, cancellationToken))
        {
            return Result<CaseDto>.Failure(CaseRoutingErrors.ClaimForbidden);
        }

        var item = await caseRepository.GetByIdAsync(caseId, cancellationToken);
        if (item is null)
            return Result<CaseDto>.Failure(CaseApplicationErrors.CaseNotFound);

        if (!item.DepartmentId.HasValue)
            return Result<CaseDto>.Failure(CaseRoutingErrors.CaseNotRouted);

        if (!await departmentDirectory.IsMemberAsync(
                item.DepartmentId.Value,
                userId,
                cancellationToken))
        {
            return Result<CaseDto>.Failure(CaseRoutingErrors.ClaimForbidden);
        }

        var assignment = item.Assign(userId, userId, clock.UtcNow);
        if (assignment.IsFailure)
            return Result<CaseDto>.Failure(assignment.Error);

        await auditRecorder.RecordAsync(
            new AuditRequest(
                "madar.case.claimed",
                nameof(Case),
                item.Id.ToString("D"),
                Attributes: new Dictionary<string, string>
                {
                    ["departmentId"] = item.DepartmentId.Value.ToString("D"),
                    ["claimantUserId"] = userId.ToString("D")
                }),
            cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return await ReloadAsync(item.Id, cancellationToken);
    }

    private async Task<DepartmentDto?> GetActiveDepartmentAsync(
        Guid departmentId,
        CancellationToken cancellationToken)
    {
        var department = await departmentDirectory.GetAsync(
            departmentId,
            cancellationToken);
        return department is { IsActive: true }
            ? department
            : null;
    }

    private bool TryGetCurrentUserId(out Guid userId)
    {
        userId = currentUser.UserId ?? Guid.Empty;
        return currentUser.IsAuthenticated && userId != Guid.Empty;
    }

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

public static class CaseRoutingErrors
{
    public static readonly Error DepartmentNotFound = Error.NotFound(
        "Madar.Department.NotFound",
        "القسم التشغيلي غير موجود أو غير فعال.");

    public static readonly Error QueueForbidden = Error.Forbidden(
        "Madar.Department.QueueForbidden",
        "لا تملك صلاحية عرض قائمة انتظار هذا القسم.");

    public static readonly Error RouteForbidden = Error.Forbidden(
        "Madar.Case.RouteForbidden",
        "لا تملك صلاحية توجيه الحالات إلى الأقسام.");

    public static readonly Error TransferForbidden = Error.Forbidden(
        "Madar.Case.TransferForbidden",
        "لا تملك صلاحية نقل الحالات بين الأقسام.");

    public static readonly Error ReassignmentForbidden = Error.Forbidden(
        "Madar.Case.ReassignmentForbidden",
        "لا تملك صلاحية إعادة إسناد الحالات.");

    public static readonly Error CaseNotRouted = Error.Conflict(
        "Madar.Case.NotRouted",
        "يجب توجيه الحالة إلى قسم قبل استلامها من قائمة الانتظار.");

    public static readonly Error ClaimForbidden = Error.Forbidden(
        "Madar.Case.ClaimForbidden",
        "لا يمكنك استلام هذه الحالة من قائمة انتظار القسم.");

    public static readonly Error AssigneeOutsideDepartment = Error.Validation(
        "Madar.Case.AssigneeOutsideDepartment",
        "الموظف المحدد ليس عضوًا في القسم الذي وُجهت إليه الحالة.");
}
