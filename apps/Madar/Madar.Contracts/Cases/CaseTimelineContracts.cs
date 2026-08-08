namespace Madar.Contracts.Cases;

public sealed record CaseTimelineEntryDto(
    Guid EventId,
    DateTimeOffset OccurredAtUtc,
    string Action,
    string? ActorId,
    string? CorrelationId,
    string? ReasonCode,
    IReadOnlyDictionary<string, string> Attributes);

public static class CaseTimelineRoutes
{
    public static string ForCase(Guid caseId) =>
        $"{CaseRoutes.ById(caseId)}/timeline";
}
