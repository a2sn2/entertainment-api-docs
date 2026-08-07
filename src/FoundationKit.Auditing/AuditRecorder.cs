using FoundationKit.Application.Abstractions;

namespace FoundationKit.Auditing;

public interface IAuditRecorder
{
    ValueTask<AuditEvent> RecordAsync(
        AuditRequest request,
        CancellationToken cancellationToken = default);
}

public sealed class AuditRecorder : IAuditRecorder
{
    private readonly IAuditSink _sink;
    private readonly IAuditContextAccessor _contextAccessor;
    private readonly IClock _clock;

    public AuditRecorder(
        IAuditSink sink,
        IAuditContextAccessor contextAccessor,
        IClock clock)
    {
        _sink = sink ?? throw new ArgumentNullException(nameof(sink));
        _contextAccessor = contextAccessor ?? throw new ArgumentNullException(nameof(contextAccessor));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
    }

    public async ValueTask<AuditEvent> RecordAsync(
        AuditRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var auditEvent = AuditEvent.Create(request, _contextAccessor.Current, _clock.UtcNow);
        await _sink.WriteAsync(auditEvent, cancellationToken);
        return auditEvent;
    }
}
