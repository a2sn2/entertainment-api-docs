using Madar.Contracts.Organization;

namespace Madar.Application.Organization;

public interface IDepartmentDirectory
{
    Task<DepartmentDto?> GetAsync(
        Guid departmentId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DepartmentDto>> ListActiveAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DepartmentDto>> ListForUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<bool> IsMemberAsync(
        Guid departmentId,
        Guid userId,
        CancellationToken cancellationToken = default);
}
