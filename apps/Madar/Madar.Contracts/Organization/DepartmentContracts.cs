using Madar.Contracts.Cases;

namespace Madar.Contracts.Organization;

public sealed record DepartmentDto(
    Guid Id,
    string Code,
    string Name,
    bool IsActive);

public sealed record DepartmentQueueDto(
    DepartmentDto Department,
    IReadOnlyList<CaseDto> Cases);

public sealed record CreateDepartmentRequest(
    string Code,
    string Name);

public sealed record UpdateDepartmentRequest(
    string Name,
    bool IsActive);

public sealed record AddDepartmentMemberRequest(Guid UserId);

public sealed record DepartmentAdminDto(
    Guid Id,
    string Code,
    string Name,
    bool IsActive,
    DateTimeOffset CreatedUtc,
    DateTimeOffset UpdatedUtc);

public sealed record DepartmentMemberDto(
    Guid UserId,
    string DisplayName,
    string Email,
    DateTimeOffset JoinedUtc);

public static class DepartmentRoutes
{
    public const string Root = "/api/departments";

    public static string Queue(Guid departmentId) =>
        $"{Root}/{departmentId:D}/queue";
}

public static class DepartmentAdminRoutes
{
    public const string Root = "/api/admin/departments";

    public static string ById(Guid departmentId) =>
        $"{Root}/{departmentId:D}";

    public static string Members(Guid departmentId) =>
        $"{ById(departmentId)}/members";

    public static string Member(Guid departmentId, Guid userId) =>
        $"{Members(departmentId)}/{userId:D}";
}
