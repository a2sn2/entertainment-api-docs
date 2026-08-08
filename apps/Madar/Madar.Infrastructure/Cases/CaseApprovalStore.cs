using Madar.Application.Cases;
using Madar.Contracts.Cases;
using Madar.Domain.Cases;
using Madar.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;

namespace Madar.Infrastructure.Cases;

public sealed class CaseApprovalStore(MadarDbContext dbContext)
    : ICaseApprovalRepository, ICaseApprovalQueryService
{
    public Task<CaseApproval?> GetByIdAsync(
        Guid approvalId,
        CancellationToken cancellationToken = default) =>
        dbContext.Set<CaseApproval>()
            .FirstOrDefaultAsync(item => item.Id == approvalId, cancellationToken);

    public async Task AddAsync(
        CaseApproval approval,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(approval);
        await dbContext.Set<CaseApproval>().AddAsync(approval, cancellationToken);
    }

    async Task<CaseApprovalDto?> ICaseApprovalQueryService.GetByIdAsync(
        Guid approvalId,
        CancellationToken cancellationToken)
    {
        var query =
            from approval in dbContext.Set<CaseApproval>().AsNoTracking()
            join requester in dbContext.Set<MadarUser>().AsNoTracking()
                on approval.RequestedByUserId equals requester.Id
            join reviewerCandidate in dbContext.Set<MadarUser>().AsNoTracking()
                on approval.ReviewedByUserId equals reviewerCandidate.Id into reviewerGroup
            from reviewer in reviewerGroup.DefaultIfEmpty()
            where approval.Id == approvalId
            select new CaseApprovalDto(
                approval.Id,
                approval.CaseId,
                approval.RequestedByUserId,
                requester.DisplayName,
                approval.RequestedUtc,
                approval.Status,
                approval.ReviewedByUserId,
                reviewer == null ? null : reviewer.DisplayName,
                approval.DecidedUtc,
                approval.DecisionNotes);

        return await query.SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<CaseApprovalDto?> GetLatestForCaseAsync(
        Guid caseId,
        CancellationToken cancellationToken = default)
    {
        var query =
            from approval in dbContext.Set<CaseApproval>().AsNoTracking()
            join requester in dbContext.Set<MadarUser>().AsNoTracking()
                on approval.RequestedByUserId equals requester.Id
            join reviewerCandidate in dbContext.Set<MadarUser>().AsNoTracking()
                on approval.ReviewedByUserId equals reviewerCandidate.Id into reviewerGroup
            from reviewer in reviewerGroup.DefaultIfEmpty()
            where approval.CaseId == caseId
            orderby approval.RequestedUtc descending, approval.Id descending
            select new CaseApprovalDto(
                approval.Id,
                approval.CaseId,
                approval.RequestedByUserId,
                requester.DisplayName,
                approval.RequestedUtc,
                approval.Status,
                approval.ReviewedByUserId,
                reviewer == null ? null : reviewer.DisplayName,
                approval.DecidedUtc,
                approval.DecisionNotes);

        return await query.FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<CaseApprovalDto>> ListForCaseAsync(
        Guid caseId,
        CancellationToken cancellationToken = default)
    {
        var query =
            from approval in dbContext.Set<CaseApproval>().AsNoTracking()
            join requester in dbContext.Set<MadarUser>().AsNoTracking()
                on approval.RequestedByUserId equals requester.Id
            join reviewerCandidate in dbContext.Set<MadarUser>().AsNoTracking()
                on approval.ReviewedByUserId equals reviewerCandidate.Id into reviewerGroup
            from reviewer in reviewerGroup.DefaultIfEmpty()
            where approval.CaseId == caseId
            orderby approval.RequestedUtc, approval.Id
            select new CaseApprovalDto(
                approval.Id,
                approval.CaseId,
                approval.RequestedByUserId,
                requester.DisplayName,
                approval.RequestedUtc,
                approval.Status,
                approval.ReviewedByUserId,
                reviewer == null ? null : reviewer.DisplayName,
                approval.DecidedUtc,
                approval.DecisionNotes);

        return await query.ToListAsync(cancellationToken);
    }
}
