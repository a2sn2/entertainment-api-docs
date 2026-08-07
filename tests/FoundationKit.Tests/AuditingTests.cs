using FoundationKit.Application.Abstractions;
using FoundationKit.Auditing;

namespace FoundationKit.Tests;

public sealed class AuditingTests
{
    [Fact]
    public void Audit_event_normalizes_identifiers_and_time()
    {
        var request = new AuditRequest(
            "initiative.approved",
            "initiative",
            " 42 ",
            AuditOutcome.Succeeded,
            "policy-approved");
        var context = new AuditContext(
            " user-7 ",
            " correlation-9 ",
            " tenant-3 ",
            " web-api ");
        var localTime = new DateTimeOffset(2026, 8, 7, 20, 0, 0, TimeSpan.FromHours(3));
        var eventId = Guid.Parse("c43d86c0-ae46-4ed1-9f8e-13163cf05c71");

        var auditEvent = AuditEvent.Create(request, context, localTime, eventId);

        Assert.Equal(eventId, auditEvent.EventId);
        Assert.Equal(new DateTimeOffset(2026, 8, 7, 17, 0, 0, TimeSpan.Zero), auditEvent.OccurredAtUtc);
        Assert.Equal("initiative.approved", auditEvent.Action);
        Assert.Equal("initiative", auditEvent.SubjectType);
        Assert.Equal("42", auditEvent.SubjectId);
        Assert.Equal("user-7", auditEvent.ActorId);
        Assert.Equal("correlation-9", auditEvent.CorrelationId);
        Assert.Equal("tenant-3", auditEvent.TenantId);
        Assert.Equal("web-api", auditEvent.Source);
        Assert.Equal("policy-approved", auditEvent.ReasonCode);
    }

    [Theory]
    [InlineData("password")]
    [InlineData("access_token")]
    [InlineData("Client-Secret")]
    [InlineData("recovery.code")]
    [InlineData("connection_string")]
    public void Audit_event_rejects_sensitive_attribute_names(string attributeName)
    {
        var request = new AuditRequest(
            "account.changed",
            "account",
            Attributes: new Dictionary<string, string>
            {
                [attributeName] = "must-not-be-recorded"
            });

        var exception = Assert.Throws<ArgumentException>(() =>
            AuditEvent.Create(request, AuditContext.Empty, DateTimeOffset.UtcNow));

        Assert.Contains("sensitive data", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Audit_event_copies_attributes_before_exposing_them()
    {
        var attributes = new Dictionary<string, string>
        {
            ["branch"] = "sanaa"
        };
        var request = new AuditRequest(
            "customer.updated",
            "customer",
            Attributes: attributes);

        var auditEvent = AuditEvent.Create(request, AuditContext.Empty, DateTimeOffset.UtcNow);
        attributes["branch"] = "changed-after-recording";

        Assert.Equal("sanaa", auditEvent.Attributes["branch"]);
    }

    [Fact]
    public void Audit_event_rejects_invalid_action_codes()
    {
        var request = new AuditRequest("customer updated", "customer");

        Assert.Throws<ArgumentException>(() =>
            AuditEvent.Create(request, AuditContext.Empty, DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Audit_event_rejects_too_many_attributes()
    {
        var attributes = Enumerable.Range(1, 33)
            .ToDictionary(index => $"field-{index}", index => index.ToString(System.Globalization.CultureInfo.InvariantCulture));
        var request = new AuditRequest(
            "bulk.updated",
            "record",
            Attributes: attributes);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            AuditEvent.Create(request, AuditContext.Empty, DateTimeOffset.UtcNow));
    }

    [Fact]
    public async Task Recorder_stamps_context_and_writes_exactly_once()
    {
        var instant = new DateTimeOffset(2026, 8, 7, 17, 15, 0, TimeSpan.Zero);
        var sink = new CapturingAuditSink();
        var context = new StaticAuditContextAccessor(
            new AuditContext("employee-15", "corr-15", "tenant-1", "worker"));
        var recorder = new AuditRecorder(sink, context, new FixedClock(instant));
        using var cancellation = new CancellationTokenSource();

        var recorded = await recorder.RecordAsync(
            new AuditRequest(
                "request.approved",
                "request",
                "REQ-19",
                AuditOutcome.Succeeded,
                "four-eyes"),
            cancellation.Token);

        Assert.Same(recorded, sink.LastEvent);
        Assert.Equal(1, sink.WriteCount);
        Assert.Equal(instant, recorded.OccurredAtUtc);
        Assert.Equal("employee-15", recorded.ActorId);
        Assert.Equal("corr-15", recorded.CorrelationId);
        Assert.Equal("tenant-1", recorded.TenantId);
        Assert.Equal("worker", recorded.Source);
        Assert.Equal(cancellation.Token, sink.LastCancellationToken);
    }

    private sealed class FixedClock : IClock
    {
        public FixedClock(DateTimeOffset utcNow)
        {
            UtcNow = utcNow;
        }

        public DateTimeOffset UtcNow { get; }
    }

    private sealed class StaticAuditContextAccessor : IAuditContextAccessor
    {
        public StaticAuditContextAccessor(AuditContext context)
        {
            Current = context;
        }

        public AuditContext Current { get; }
    }

    private sealed class CapturingAuditSink : IAuditSink
    {
        public AuditEvent? LastEvent { get; private set; }

        public int WriteCount { get; private set; }

        public CancellationToken LastCancellationToken { get; private set; }

        public ValueTask WriteAsync(
            AuditEvent auditEvent,
            CancellationToken cancellationToken = default)
        {
            LastEvent = auditEvent;
            LastCancellationToken = cancellationToken;
            WriteCount++;
            return ValueTask.CompletedTask;
        }
    }
}
