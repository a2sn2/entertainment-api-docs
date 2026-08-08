using FoundationKit.Auditing;

namespace Madar.Infrastructure.Auditing;

public sealed class SqlAuditSink(MadarDbContext dbContext) : IAuditSink
{
    public ValueTask WriteAsync(
        AuditEvent auditEvent,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(auditEvent);
        dbContext.AuditEvents.Add(MadarAuditRecord.From(auditEvent));
        return ValueTask.CompletedTask;
    }
}
