namespace FoundationKit.Auditing;

public sealed record AuditContext(
    string? ActorId,
    string? CorrelationId,
    string? TenantId,
    string? Source)
{
    public static AuditContext Empty { get; } = new(null, null, null, null);
}

public interface IAuditContextAccessor
{
    AuditContext Current { get; }
}
