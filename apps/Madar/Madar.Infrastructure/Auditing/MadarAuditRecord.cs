using System.Text.Json;
using FoundationKit.Auditing;

namespace Madar.Infrastructure.Auditing;

public sealed class MadarAuditRecord
{
    private MadarAuditRecord()
    {
    }

    public Guid Id { get; private set; }

    public DateTimeOffset OccurredAtUtc { get; private set; }

    public string Action { get; private set; } = string.Empty;

    public string SubjectType { get; private set; } = string.Empty;

    public string? SubjectId { get; private set; }

    public AuditOutcome Outcome { get; private set; }

    public string? ActorId { get; private set; }

    public string? CorrelationId { get; private set; }

    public string? TenantId { get; private set; }

    public string? Source { get; private set; }

    public string? ReasonCode { get; private set; }

    public string AttributesJson { get; private set; } = "{}";

    public static MadarAuditRecord From(AuditEvent auditEvent)
    {
        ArgumentNullException.ThrowIfNull(auditEvent);

        return new MadarAuditRecord
        {
            Id = auditEvent.EventId,
            OccurredAtUtc = auditEvent.OccurredAtUtc,
            Action = auditEvent.Action,
            SubjectType = auditEvent.SubjectType,
            SubjectId = auditEvent.SubjectId,
            Outcome = auditEvent.Outcome,
            ActorId = auditEvent.ActorId,
            CorrelationId = auditEvent.CorrelationId,
            TenantId = auditEvent.TenantId,
            Source = auditEvent.Source,
            ReasonCode = auditEvent.ReasonCode,
            AttributesJson = JsonSerializer.Serialize(auditEvent.Attributes)
        };
    }
}
