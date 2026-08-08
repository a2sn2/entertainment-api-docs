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

public static class DepartmentRoutes
{
    public const string Root = "/api/departments";

    public static string Queue(Guid departmentId) =>
        $"{Root}/{departmentId:D}/queue";
}
