using Madar.Contracts.Cases;
using Madar.Domain.Cases;

namespace Madar.Application.Cases;

public interface ICaseApprovalQueryService
{
    Task<CaseApprovalDto?> GetByIdAsync(
        Guid approvalId,
        CancellationToken cancellationToken = default);

    Task<CaseApprovalDto?> GetLatestForCaseAsync(
        Guid caseId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CaseApprovalDto>> ListForCaseAsync(
        Guid caseId,
        CancellationToken cancellationToken = default);
}

public interface ICaseApprovalRepository
{
    Task<CaseApproval?> GetByIdAsync(
        Guid approvalId,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        CaseApproval approval,
        CancellationToken cancellationToken = default);
}
