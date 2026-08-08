using System.Text.Json;
using Madar.Application.Cases;
using Madar.Contracts.Cases;
using Microsoft.EntityFrameworkCore;

namespace Madar.Infrastructure.Cases;

public sealed class CaseTimelineQueryService(MadarDbContext dbContext)
    : ICaseTimelineQueryService
{
    public async Task<IReadOnlyList<CaseTimelineEntryDto>> ListAsync(
        Guid caseId,
        CancellationToken cancellationToken = default)
    {
        var subjectId = caseId.ToString("D");
        var records = await dbContext.AuditEvents
            .AsNoTracking()
            .Where(item =>
                item.SubjectType == "Case"
                && item.SubjectId == subjectId)
            .OrderBy(item => item.OccurredAtUtc)
            .ThenBy(item => item.Id)
            .ToListAsync(cancellationToken);

        return records
            .Select(item => new CaseTimelineEntryDto(
                item.Id,
                item.OccurredAtUtc,
                item.Action,
                item.ActorId,
                item.CorrelationId,
                item.ReasonCode,
                DeserializeAttributes(item.AttributesJson)))
            .ToArray();
    }

    private static IReadOnlyDictionary<string, string> DeserializeAttributes(
        string value) =>
        JsonSerializer.Deserialize<Dictionary<string, string>>(value)
        ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
}
