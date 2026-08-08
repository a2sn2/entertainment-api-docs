using FoundationKit.Application.Abstractions;
using FoundationKit.Application.Persistence;
using FoundationKit.Application.Results;
using FoundationKit.Auditing;
using FoundationKit.Authorization;
using Madar.Application.Security;
using Madar.Contracts.Organization;
using Madar.Domain.Organization;

namespace Madar.Application.Organization;

public interface IDepartmentAdministrationStore
{
    Task<IReadOnlyList<DepartmentAdminDto>> ListAsync(
        CancellationToken cancellationToken = default);

    Task<Department?> FindAsync(
        Guid departmentId,
        CancellationToken cancellationToken = default);

    Task<bool> CodeExistsAsync(
        string code,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        Department department,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DepartmentMemberDto>> ListMembersAsync(
        Guid departmentId,
        CancellationToken cancellationToken = default);

    Task<DepartmentMemberDto?> GetMemberAsync(
        Guid departmentId,
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<DepartmentMembership?> FindMembershipAsync(
        Guid departmentId,
        Guid userId,
        CancellationToken cancellationToken = default);

    Task AddMembershipAsync(
        DepartmentMembership membership,
        CancellationToken cancellationToken = default);

    void RemoveMembership(DepartmentMembership membership);

    Task<bool> HasOpenCasesAsync(
        Guid departmentId,
        CancellationToken cancellationToken = default);

    Task<bool> HasOpenAssignedCasesAsync(
        Guid departmentId,
        Guid userId,
        CancellationToken cancellationToken = default);
}

public interface IDepartmentAdministrationManager
{
    Task<Result<IReadOnlyList<DepartmentAdminDto>>> ListAsync(
        CancellationToken cancellationToken = default);

    Task<Result<DepartmentAdminDto>> CreateAsync(
        CreateDepartmentRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<DepartmentAdminDto>> UpdateAsync(
        Guid departmentId,
        UpdateDepartmentRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<DepartmentMemberDto>>> ListMembersAsync(
        Guid departmentId,
        CancellationToken cancellationToken = default);

    Task<Result<DepartmentMemberDto>> AddMemberAsync(
        Guid departmentId,
        AddDepartmentMemberRequest request,
        CancellationToken cancellationToken = default);

    Task<Result> RemoveMemberAsync(
        Guid departmentId,
        Guid userId,
        CancellationToken cancellationToken = default);
}

public sealed class DepartmentAdministrationManager(
    ICurrentUser currentUser,
    IAuthorizationEvaluator authorization,
    IDepartmentAdministrationStore store,
    IUserDirectory userDirectory,
    IUnitOfWork unitOfWork,
    IAuditRecorder auditRecorder,
    IClock clock) : IDepartmentAdministrationManager
{
    public async Task<Result<IReadOnlyList<DepartmentAdminDto>>> ListAsync(
        CancellationToken cancellationToken = default)
    {
        var access = ValidateAccess<IReadOnlyList<DepartmentAdminDto>>();
        if (access is not null)
            return access;

        return Result<IReadOnlyList<DepartmentAdminDto>>.Success(
            await store.ListAsync(cancellationToken));
    }

    public async Task<Result<DepartmentAdminDto>> CreateAsync(
        CreateDepartmentRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var access = ValidateAccess<DepartmentAdminDto>();
        if (access is not null)
            return access;

        var creation = Department.Create(
            request.Code,
            request.Name,
            clock.UtcNow);
        if (creation.IsFailure)
            return Result<DepartmentAdminDto>.Failure(creation.Error);

        if (await store.CodeExistsAsync(creation.Value.Code, cancellationToken))
        {
            return Result<DepartmentAdminDto>.Failure(
                DepartmentAdministrationErrors.CodeAlreadyExists);
        }

        await store.AddAsync(creation.Value, cancellationToken);
        await auditRecorder.RecordAsync(
            new AuditRequest(
                "madar.department.created",
                nameof(Department),
                creation.Value.Id.ToString("D"),
                Attributes: new Dictionary<string, string>
                {
                    ["code"] = creation.Value.Code,
                    ["active"] = creation.Value.IsActive.ToString()
                }),
            cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<DepartmentAdminDto>.Success(ToDto(creation.Value));
    }

    public async Task<Result<DepartmentAdminDto>> UpdateAsync(
        Guid departmentId,
        UpdateDepartmentRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var access = ValidateAccess<DepartmentAdminDto>();
        if (access is not null)
            return access;

        var department = await store.FindAsync(departmentId, cancellationToken);
        if (department is null)
        {
            return Result<DepartmentAdminDto>.Failure(
                DepartmentAdministrationErrors.DepartmentNotFound);
        }

        if (department.IsActive
            && !request.IsActive
            && await store.HasOpenCasesAsync(department.Id, cancellationToken))
        {
            return Result<DepartmentAdminDto>.Failure(
                DepartmentAdministrationErrors.DepartmentHasOpenCases);
        }

        var previousName = department.Name;
        var previousActive = department.IsActive;
        var update = department.Update(
            request.Name,
            request.IsActive,
            clock.UtcNow);
        if (update.IsFailure)
            return Result<DepartmentAdminDto>.Failure(update.Error);

        if (previousName == department.Name
            && previousActive == department.IsActive)
        {
            return Result<DepartmentAdminDto>.Success(ToDto(department));
        }

        await auditRecorder.RecordAsync(
            new AuditRequest(
                "madar.department.updated",
                nameof(Department),
                department.Id.ToString("D"),
                Attributes: new Dictionary<string, string>
                {
                    ["code"] = department.Code,
                    ["previousActive"] = previousActive.ToString(),
                    ["active"] = department.IsActive.ToString()
                }),
            cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<DepartmentAdminDto>.Success(ToDto(department));
    }

    public async Task<Result<IReadOnlyList<DepartmentMemberDto>>> ListMembersAsync(
        Guid departmentId,
        CancellationToken cancellationToken = default)
    {
        var access = ValidateAccess<IReadOnlyList<DepartmentMemberDto>>();
        if (access is not null)
            return access;

        var department = await store.FindAsync(departmentId, cancellationToken);
        if (department is null)
        {
            return Result<IReadOnlyList<DepartmentMemberDto>>.Failure(
                DepartmentAdministrationErrors.DepartmentNotFound);
        }

        return Result<IReadOnlyList<DepartmentMemberDto>>.Success(
            await store.ListMembersAsync(departmentId, cancellationToken));
    }

    public async Task<Result<DepartmentMemberDto>> AddMemberAsync(
        Guid departmentId,
        AddDepartmentMemberRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var access = ValidateAccess<DepartmentMemberDto>();
        if (access is not null)
            return access;

        var department = await store.FindAsync(departmentId, cancellationToken);
        if (department is null)
        {
            return Result<DepartmentMemberDto>.Failure(
                DepartmentAdministrationErrors.DepartmentNotFound);
        }

        if (!await userDirectory.IsAssignableOperatorAsync(
                request.UserId,
                cancellationToken))
        {
            return Result<DepartmentMemberDto>.Failure(
                DepartmentAdministrationErrors.MemberMustBeOperator);
        }

        if (await store.FindMembershipAsync(
                department.Id,
                request.UserId,
                cancellationToken) is not null)
        {
            return Result<DepartmentMemberDto>.Failure(
                DepartmentAdministrationErrors.MembershipAlreadyExists);
        }

        var membership = DepartmentMembership.Create(
            department.Id,
            request.UserId,
            clock.UtcNow);
        if (membership.IsFailure)
            return Result<DepartmentMemberDto>.Failure(membership.Error);

        await store.AddMembershipAsync(membership.Value, cancellationToken);
        await auditRecorder.RecordAsync(
            new AuditRequest(
                "madar.department.member-added",
                nameof(Department),
                department.Id.ToString("D"),
                Attributes: new Dictionary<string, string>
                {
                    ["departmentId"] = department.Id.ToString("D"),
                    ["userId"] = request.UserId.ToString("D")
                }),
            cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var response = await store.GetMemberAsync(
            department.Id,
            request.UserId,
            cancellationToken);
        return response is null
            ? Result<DepartmentMemberDto>.Failure(
                DepartmentAdministrationErrors.MemberNotFound)
            : Result<DepartmentMemberDto>.Success(response);
    }

    public async Task<Result> RemoveMemberAsync(
        Guid departmentId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var access = ValidateAccess();
        if (access is not null)
            return access;

        var department = await store.FindAsync(departmentId, cancellationToken);
        if (department is null)
            return Result.Failure(DepartmentAdministrationErrors.DepartmentNotFound);

        var membership = await store.FindMembershipAsync(
            department.Id,
            userId,
            cancellationToken);
        if (membership is null)
            return Result.Failure(DepartmentAdministrationErrors.MemberNotFound);

        if (await store.HasOpenAssignedCasesAsync(
                department.Id,
                userId,
                cancellationToken))
        {
            return Result.Failure(
                DepartmentAdministrationErrors.MemberHasOpenAssignments);
        }

        store.RemoveMembership(membership);
        await auditRecorder.RecordAsync(
            new AuditRequest(
                "madar.department.member-removed",
                nameof(Department),
                department.Id.ToString("D"),
                Attributes: new Dictionary<string, string>
                {
                    ["departmentId"] = department.Id.ToString("D"),
                    ["userId"] = userId.ToString("D")
                }),
            cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    private Result<T>? ValidateAccess<T>()
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId is null)
        {
            return Result<T>.Failure(
                DepartmentAdministrationErrors.AuthenticationRequired);
        }

        return authorization.HasPermission(MadarPermissions.ManageDepartments)
            ? null
            : Result<T>.Failure(DepartmentAdministrationErrors.Forbidden);
    }

    private Result? ValidateAccess()
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId is null)
            return Result.Failure(DepartmentAdministrationErrors.AuthenticationRequired);

        return authorization.HasPermission(MadarPermissions.ManageDepartments)
            ? null
            : Result.Failure(DepartmentAdministrationErrors.Forbidden);
    }

    private static DepartmentAdminDto ToDto(Department department) =>
        new(
            department.Id,
            department.Code,
            department.Name,
            department.IsActive,
            department.CreatedUtc,
            department.UpdatedUtc);
}

public static class DepartmentAdministrationErrors
{
    public static readonly Error AuthenticationRequired = Error.Unauthorized(
        "Madar.DepartmentAdmin.AuthenticationRequired",
        "يجب تسجيل الدخول لإدارة الأقسام.");

    public static readonly Error Forbidden = Error.Forbidden(
        "Madar.DepartmentAdmin.Forbidden",
        "لا تملك صلاحية إدارة الأقسام وعضوياتها.");

    public static readonly Error DepartmentNotFound = Error.NotFound(
        "Madar.DepartmentAdmin.NotFound",
        "القسم المطلوب غير موجود.");

    public static readonly Error CodeAlreadyExists = Error.Conflict(
        "Madar.DepartmentAdmin.CodeAlreadyExists",
        "رمز القسم مستخدم مسبقًا.");

    public static readonly Error DepartmentHasOpenCases = Error.Conflict(
        "Madar.DepartmentAdmin.HasOpenCases",
        "لا يمكن تعطيل القسم قبل معالجة أو نقل جميع الحالات غير المغلقة التابعة له.");

    public static readonly Error MemberMustBeOperator = Error.Validation(
        "Madar.DepartmentAdmin.MemberMustBeOperator",
        "يمكن إضافة المستخدم إلى عضوية القسم التشغيلية فقط إذا كان يحمل دور Operator.");

    public static readonly Error MembershipAlreadyExists = Error.Conflict(
        "Madar.DepartmentAdmin.MembershipAlreadyExists",
        "المستخدم عضو في هذا القسم مسبقًا.");

    public static readonly Error MemberNotFound = Error.NotFound(
        "Madar.DepartmentAdmin.MemberNotFound",
        "عضوية المستخدم في القسم غير موجودة.");

    public static readonly Error MemberHasOpenAssignments = Error.Conflict(
        "Madar.DepartmentAdmin.MemberHasOpenAssignments",
        "لا يمكن إزالة الموظف من القسم بينما لديه حالات غير مغلقة مسندة إليه في هذا القسم.");
}
