using FoundationKit.Workbench.Application.Admin;
using FoundationKit.Workbench.Contracts.Admin;
using FoundationKit.Workbench.Domain;
using Microsoft.EntityFrameworkCore;

namespace FoundationKit.Workbench.Infrastructure;

public sealed class EfAdminQueueReader(WorkbenchDbContext dbContext) : IAdminQueueReader
{
    public async Task<IReadOnlyList<AdminQueueItemResponse>> ReadAsync(
        string? status,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.BuildBriefs.AsNoTracking();

        if (string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(brief => brief.Status == BuildBriefStatus.Submitted);
        }
        else if (Enum.TryParse<BuildBriefStatus>(status, ignoreCase: true, out var parsedStatus))
        {
            query = query.Where(brief => brief.Status == parsedStatus);
        }

        var briefs = await query
            .OrderByDescending(brief => brief.CreatedUtc)
            .Take(100)
            .ToListAsync(cancellationToken);

        return briefs.Select(brief => new AdminQueueItemResponse(
                brief.Id,
                brief.ProjectName,
                brief.ProjectType,
                brief.Audience,
                brief.Goal,
                brief.SelectedCapabilityIds,
                brief.Priorities,
                brief.Notes,
                brief.Status.ToString().ToLowerInvariant(),
                brief.CreatedUtc,
                brief.UpdatedUtc))
            .ToArray();
    }
}
