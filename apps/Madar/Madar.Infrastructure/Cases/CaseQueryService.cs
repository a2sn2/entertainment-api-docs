using FoundationKit.Application.Abstractions;
using Madar.Application.Cases;
using Madar.Contracts.Cases;
using Madar.Domain.Cases;
using Microsoft.EntityFrameworkCore;

namespace Madar.Infrastructure.Cases;

public sealed class CaseQueryService(
    MadarDbContext dbContext,
    IClock clock) : ICaseQueryService, ICaseSlaQueryService
{
    public async Task<CaseDto?> GetByIdAsync(
        Guid caseId,
        CancellationToken cancellationToken = default)
    {
        var item = await dbContext.Cases
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == caseId, cancellationToken);

        return item is null ? null : ToDto(item, clock.UtcNow);
    }

    public async Task<IReadOnlyList<CaseDto>> ListForUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var items = await dbContext.Cases
            .AsNoTracking()
            .Where(item =>
                item.CreatedByUserId == userId
                || item.AssignedToUserId == userId)
            .OrderByDescending(item => item.UpdatedUtc)
            .ToListAsync(cancellationToken);

        var evaluatedUtc = clock.UtcNow;
        return items.Select(item => ToDto(item, evaluatedUtc)).ToArray();
    }

    public async Task<IReadOnlyList<CaseDto>> ListAllAsync(
        CancellationToken cancellationToken = default)
    {
        var items = await dbContext.Cases
            .AsNoTracking()
            .OrderByDescending(item => item.UpdatedUtc)
            .ToListAsync(cancellationToken);

        var evaluatedUtc = clock.UtcNow;
        return items.Select(item => ToDto(item, evaluatedUtc)).ToArray();
    }

    public async Task<IReadOnlyList<CaseDto>> ListDepartmentQueueAsync(
        Guid departmentId,
        CancellationToken cancellationToken = default)
    {
        var items = await dbContext.Cases
            .AsNoTracking()
            .Where(item =>
                item.DepartmentId == departmentId
                && item.Status == CaseStatuses.New
                && item.AssignedToUserId == null)
            .OrderBy(item => item.CreatedUtc)
            .ThenBy(item => item.Id)
            .ToListAsync(cancellationToken);

        var evaluatedUtc = clock.UtcNow;
        return items.Select(item => ToDto(item, evaluatedUtc)).ToArray();
    }

    public async Task<IReadOnlyList<Guid>> ListDueCaseIdsAsync(
        DateTimeOffset evaluatedUtc,
        int limit,
        CancellationToken cancellationToken = default) =>
        await dbContext.Cases
            .AsNoTracking()
            .Where(item =>
                item.SlaTargetUtc != null
                && item.SlaTargetUtc < evaluatedUtc
                && item.SlaBreachedUtc == null
                && item.ResolvedUtc == null)
            .OrderBy(item => item.SlaTargetUtc)
            .ThenBy(item => item.Id)
            .Select(item => item.Id)
            .Take(limit)
            .ToListAsync(cancellationToken);

    private static CaseDto ToDto(Case item, DateTimeOffset evaluatedUtc) =>
        new(
            item.Id,
            item.CreatedByUserId,
            item.Title,
            item.Description,
            item.CaseType,
            item.Priority,
            item.Status,
            item.DepartmentId,
            item.RoutedUtc,
            item.AssignedToUserId,
            item.CreatedUtc,
            item.UpdatedUtc,
            item.ResolvedUtc,
            item.ClosedUtc,
            item.SlaTargetUtc,
            item.SlaBreachedUtc,
            item.EscalatedUtc,
            item.GetSlaState(evaluatedUtc));
}
