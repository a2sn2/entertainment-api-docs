using Athar.Application;
using Athar.Contracts;
using Athar.Domain;
using FoundationKit.Application.Abstractions;
using FoundationKit.Application.Pagination;
using Microsoft.EntityFrameworkCore;

namespace Athar.Infrastructure;

public sealed class InitiativeQueryService(AtharDbContext dbContext)
    : IInitiativeQueryService
{
    public async Task<InitiativeDetailsDto?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var initiative = await (
            from item in dbContext.Initiatives.AsNoTracking()
            join owner in dbContext.Users.AsNoTracking()
                on item.OwnerUserId equals owner.Id
            where item.Id == id
            select new
            {
                Item = item,
                Owner = owner.DisplayName
            }).SingleOrDefaultAsync(cancellationToken);

        if (initiative is null)
            return null;

        var reviews = await (
            from review in dbContext.InitiativeReviews.AsNoTracking()
            join reviewer in dbContext.Users.AsNoTracking()
                on review.ReviewerUserId equals reviewer.Id
            where review.InitiativeId == id
            orderby review.ReviewedUtc descending
            select new InitiativeReviewDto(
                review.Id,
                review.InitiativeId,
                review.Decision,
                reviewer.DisplayName,
                review.Notes,
                review.ReviewedUtc))
            .ToListAsync(cancellationToken);

        return ToDetails(initiative.Item, initiative.Owner, reviews);
    }

    public async Task<InitiativeDetailsDto?> FindByClientRequestIdAsync(
        Guid ownerUserId,
        Guid clientRequestId,
        CancellationToken cancellationToken = default)
    {
        var id = await dbContext.Initiatives
            .AsNoTracking()
            .Where(item =>
                item.OwnerUserId == ownerUserId
                && item.ClientRequestId == clientRequestId)
            .Select(item => (Guid?)item.Id)
            .SingleOrDefaultAsync(cancellationToken);

        return id is null
            ? null
            : await GetByIdAsync(id.Value, cancellationToken);
    }

    public Task<PagedResult<InitiativeSummaryDto>> GetMineAsync(
        Guid ownerUserId,
        InitiativeSearchRequest request,
        CancellationToken cancellationToken = default) =>
        GetPageAsync(
            dbContext.Initiatives
                .AsNoTracking()
                .Where(item => item.OwnerUserId == ownerUserId),
            request,
            cancellationToken);

    public Task<PagedResult<InitiativeSummaryDto>> GetAdminQueueAsync(
        InitiativeSearchRequest request,
        CancellationToken cancellationToken = default) =>
        GetPageAsync(
            dbContext.Initiatives.AsNoTracking(),
            request,
            cancellationToken);

    public async Task<AdminDashboardResponse> GetDashboardAsync(
        CancellationToken cancellationToken = default)
    {
        var counts = await dbContext.Initiatives
            .AsNoTracking()
            .GroupBy(item => item.Status)
            .Select(group => new
            {
                Status = group.Key,
                Count = group.Count()
            })
            .ToDictionaryAsync(
                item => item.Status,
                item => item.Count,
                cancellationToken);

        var approved = dbContext.Initiatives
            .AsNoTracking()
            .Where(item => item.Status == InitiativeStatuses.Approved);

        return new AdminDashboardResponse(
            counts.GetValueOrDefault(InitiativeStatuses.Submitted),
            counts.GetValueOrDefault(InitiativeStatuses.Approved),
            counts.GetValueOrDefault(InitiativeStatuses.Rejected),
            counts.Values.Sum(),
            await approved.SumAsync(
                item => (decimal?)item.RequestedBudget,
                cancellationToken) ?? 0,
            await approved.SumAsync(
                item => (int?)item.TargetBeneficiaries,
                cancellationToken) ?? 0);
    }

    private static InitiativeDetailsDto ToDetails(
        Initiative item,
        string ownerDisplayName,
        IReadOnlyList<InitiativeReviewDto> reviews) =>
        new(
            item.Id,
            item.CreatedUtc,
            item.UpdatedUtc,
            item.Title,
            item.Summary,
            item.Category,
            item.City,
            item.RequestedBudget,
            item.TargetBeneficiaries,
            item.Status,
            ownerDisplayName,
            reviews);

    private static async Task<PagedResult<InitiativeSummaryDto>> GetPageAsync(
        IQueryable<Initiative> query,
        InitiativeSearchRequest request,
        CancellationToken cancellationToken)
    {
        var pageRequest = new PageRequest(request.Page, request.PageSize);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim();
            query = query.Where(item =>
                item.Title.Contains(search)
                || item.Summary.Contains(search)
                || item.City.Contains(search)
                || item.Category.Contains(search));
        }

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            var status = request.Status.Trim().ToLowerInvariant();
            query = query.Where(item => item.Status == status);
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(item => item.CreatedUtc)
            .Skip(pageRequest.Skip)
            .Take(pageRequest.PageSize)
            .Select(item => new InitiativeSummaryDto(
                item.Id,
                item.CreatedUtc,
                item.UpdatedUtc,
                item.Title,
                item.Category,
                item.City,
                item.RequestedBudget,
                item.TargetBeneficiaries,
                item.Status))
            .ToListAsync(cancellationToken);

        return new PagedResult<InitiativeSummaryDto>(
            items,
            pageRequest.Page,
            pageRequest.PageSize,
            total);
    }
}

public sealed class AuditWriter(
    AtharDbContext dbContext,
    IClock clock) : IAuditWriter
{
    public Task WriteAsync(
        Guid? userId,
        string action,
        string entityType,
        Guid entityId,
        string details,
        CancellationToken cancellationToken = default)
    {
        dbContext.AuditEntries.Add(new AuditEntry
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            Details = details,
            CreatedUtc = clock.UtcNow
        });

        return Task.CompletedTask;
    }
}
