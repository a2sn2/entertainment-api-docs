namespace Madar.Contracts.Cases;

public sealed record CreateCaseRequest(
    string Title,
    string Description,
    string CaseType,
    string Priority);

public sealed record AssignCaseRequest(Guid AssigneeUserId);

public sealed record TransitionCaseRequest(string Trigger);

public sealed record CaseDto(
    Guid Id,
    Guid CreatedByUserId,
    string Title,
    string Description,
    string CaseType,
    string Priority,
    string Status,
    Guid? AssignedToUserId,
    DateTimeOffset CreatedUtc,
    DateTimeOffset UpdatedUtc,
    DateTimeOffset? ResolvedUtc,
    DateTimeOffset? ClosedUtc);

public static class CaseRoutes
{
    public const string Root = "/api/cases";

    public static string ById(Guid caseId) => $"{Root}/{caseId:D}";

    public static string Assign(Guid caseId) => $"{ById(caseId)}/assignment";

    public static string Transition(Guid caseId) => $"{ById(caseId)}/transition";
}
