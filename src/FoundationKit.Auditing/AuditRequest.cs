namespace FoundationKit.Auditing;

public sealed record AuditRequest(
    string Action,
    string SubjectType,
    string? SubjectId = null,
    AuditOutcome Outcome = AuditOutcome.Succeeded,
    string? ReasonCode = null,
    IReadOnlyDictionary<string, string>? Attributes = null);
