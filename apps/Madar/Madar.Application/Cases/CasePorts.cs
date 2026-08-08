using Madar.Contracts.Cases;
using Madar.Domain.Cases;

namespace Madar.Application.Cases;

public interface ICaseRepository
{
    Task<Case?> FindByIdAsync(Guid caseId, CancellationToken cancellationToken = default);

    Task AddAsync(Case @case, CancellationToken cancellationToken = default);
}

public interface ICaseQueryService
{
    Task<CaseDto?> FindByIdAsync(
        Guid caseId,
        Guid currentUserId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CaseDto>> ListVisibleAsync(
        Guid currentUserId,
        CancellationToken cancellationToken = default);
}
