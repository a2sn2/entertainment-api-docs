namespace Madar.Contracts.Cases;

public sealed record CreateCaseRequest(
    string Title,
    string Description,
    string CaseType,
    string Priority);

public sealed record AssignCaseRequest(Guid AssigneeUserId);

public sealed record RouteCaseRequest(Guid DepartmentId);

public sealed record TransitionCaseRequest(string Trigger);

public sealed record EvaluateCaseSlaRequest(int Limit = 50);

public sealed record CaseSlaEvaluationResponse(
    DateTimeOffset EvaluatedUtc,
    int EvaluatedCount,
    int BreachedCount,
    bool HasMore);

public sealed record CaseDto(
    Guid Id,
    Guid CreatedByUserId,
    string Title,
    string Description,
    string CaseType,
    string Priority,
    string Status,
    Guid? DepartmentId,
    DateTimeOffset? RoutedUtc,
    Guid? AssignedToUserId,
    DateTimeOffset CreatedUtc,
    DateTimeOffset UpdatedUtc,
    DateTimeOffset? ResolvedUtc,
    DateTimeOffset? ClosedUtc,
    DateTimeOffset? SlaTargetUtc,
    DateTimeOffset? SlaBreachedUtc,
    DateTimeOffset? EscalatedUtc,
    string SlaState);

public static class CaseRoutes
{
    public const string Root = "/api/cases";

    public const string EvaluateSla = $"{Root}/sla/evaluate";

    public static string ById(Guid caseId) => $"{Root}/{caseId:D}";

    public static string Route(Guid caseId) => $"{ById(caseId)}/route";

    public static string Claim(Guid caseId) => $"{ById(caseId)}/claim";

    public static string Assign(Guid caseId) => $"{ById(caseId)}/assignment";

    public static string Transition(Guid caseId) => $"{ById(caseId)}/transition";
}
