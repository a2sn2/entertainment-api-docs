using System.Linq.Expressions;
using Madar.Application.Cases;
using Madar.Contracts.Cases;
using Madar.Domain.Cases;
using Microsoft.EntityFrameworkCore;

namespace Madar.Infrastructure.Cases;

public sealed class CaseQueryService(MadarDbContext dbContext) : ICaseQueryService
{
    private static readonly Expression<Func<Case, CaseDto>> Projection = item =>
        new CaseDto(
            item.Id,
            item.CreatedByUserId,
            item.Title,
            item.Description,
            item.CaseType,
            item.Priority,
            item.Status,
            item.AssignedToUserId,
            item.CreatedUtc,
            item.UpdatedUtc,
            item.ResolvedUtc,
            item.ClosedUtc);

    public Task<CaseDto?> GetByIdAsync(
        Guid caseId,
        CancellationToken cancellationToken = default) =>
        dbContext.Cases
            .AsNoTracking()
            .Where(item => item.Id == caseId)
            .Select(Projection)
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<CaseDto>> ListForUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default) =>
        await dbContext.Cases
            .AsNoTracking()
            .Where(item =>
                item.CreatedByUserId == userId
                || item.AssignedToUserId == userId)
            .OrderByDescending(item => item.UpdatedUtc)
            .Select(Projection)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<CaseDto>> ListAllAsync(
        CancellationToken cancellationToken = default) =>
        await dbContext.Cases
            .AsNoTracking()
            .OrderByDescending(item => item.UpdatedUtc)
            .Select(Projection)
            .ToListAsync(cancellationToken);
}
