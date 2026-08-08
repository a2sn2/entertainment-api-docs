using Madar.Contracts.Cases;
using Madar.Domain.Cases;

namespace Madar.Application.Cases;

public interface ICaseCommentStore
{
    Task AddAsync(
        CaseComment comment,
        CancellationToken cancellationToken = default);
}

public interface ICaseCommentQueryService
{
    Task<CaseCommentDto?> GetByIdAsync(
        Guid commentId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CaseCommentDto>> ListForCaseAsync(
        Guid caseId,
        CancellationToken cancellationToken = default);
}
