using Madar.Contracts.Cases;

namespace Madar.Application.Cases;

public interface ICaseQueryService
{
    Task<CaseDto?> GetByIdAsync(
        Guid caseId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CaseDto>> ListForUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CaseDto>> ListAllAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CaseDto>> ListDepartmentQueueAsync(
        Guid departmentId,
        CancellationToken cancellationToken = default);
}

public interface ICaseSlaQueryService
{
    Task<IReadOnlyList<Guid>> ListDueCaseIdsAsync(
        DateTimeOffset evaluatedUtc,
        int limit,
        CancellationToken cancellationToken = default);
}
