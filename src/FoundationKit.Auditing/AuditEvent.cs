using System.Collections.ObjectModel;

namespace FoundationKit.Auditing;

public sealed class AuditEvent
{
    internal const int MaxActionLength = 128;
    internal const int MaxSubjectTypeLength = 128;
    internal const int MaxIdentifierLength = 256;
    internal const int MaxReasonCodeLength = 128;
    internal const int MaxAttributeCount = 32;
    internal const int MaxAttributeNameLength = 64;
    internal const int MaxAttributeValueLength = 512;

    private AuditEvent(
        Guid eventId,
        DateTimeOffset occurredAtUtc,
        string action,
        string subjectType,
        string? subjectId,
        AuditOutcome outcome,
        string? actorId,
        string? correlationId,
        string? tenantId,
        string? source,
        string? reasonCode,
        IReadOnlyDictionary<string, string> attributes)
    {
        EventId = eventId;
        OccurredAtUtc = occurredAtUtc;
        Action = action;
        SubjectType = subjectType;
        SubjectId = subjectId;
        Outcome = outcome;
        ActorId = actorId;
        CorrelationId = correlationId;
        TenantId = tenantId;
        Source = source;
        ReasonCode = reasonCode;
        Attributes = attributes;
    }

    public Guid EventId { get; }

    public DateTimeOffset OccurredAtUtc { get; }

    public string Action { get; }

    public string SubjectType { get; }

    public string? SubjectId { get; }

    public AuditOutcome Outcome { get; }

    public string? ActorId { get; }

    public string? CorrelationId { get; }

    public string? TenantId { get; }

    public string? Source { get; }

    public string? ReasonCode { get; }

    public IReadOnlyDictionary<string, string> Attributes { get; }

    public static AuditEvent Create(
        AuditRequest request,
        AuditContext context,
        DateTimeOffset occurredAtUtc,
        Guid? eventId = null)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);

        var normalizedEventId = eventId ?? Guid.NewGuid();
        if (normalizedEventId == Guid.Empty)
        {
            throw new ArgumentException("Audit event ID cannot be empty.", nameof(eventId));
        }

        return new AuditEvent(
            normalizedEventId,
            occurredAtUtc.ToUniversalTime(),
            AuditGuard.RequiredCode(request.Action, nameof(request.Action), MaxActionLength),
            AuditGuard.RequiredCode(request.SubjectType, nameof(request.SubjectType), MaxSubjectTypeLength),
            AuditGuard.OptionalIdentifier(request.SubjectId, nameof(request.SubjectId), MaxIdentifierLength),
            request.Outcome,
            AuditGuard.OptionalIdentifier(context.ActorId, nameof(context.ActorId), MaxIdentifierLength),
            AuditGuard.OptionalIdentifier(
                context.CorrelationId,
                nameof(context.CorrelationId),
                MaxIdentifierLength),
            AuditGuard.OptionalIdentifier(context.TenantId, nameof(context.TenantId), MaxIdentifierLength),
            AuditGuard.OptionalCode(context.Source, nameof(context.Source), MaxSubjectTypeLength),
            AuditGuard.OptionalCode(request.ReasonCode, nameof(request.ReasonCode), MaxReasonCodeLength),
            AuditGuard.Attributes(request.Attributes));
    }
}

internal static class AuditGuard
{
    private static readonly HashSet<string> SensitiveAttributeNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "password",
        "passwd",
        "pwd",
        "token",
        "accesstoken",
        "refreshtoken",
        "authorization",
        "cookie",
        "secret",
        "clientsecret",
        "connectionstring",
        "otp",
        "totp",
        "recoverycode",
        "recoverycodes",
        "privatekey"
    };

    public static string RequiredCode(string value, string parameterName, int maxLength)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);
        return NormalizeCode(value, parameterName, maxLength);
    }

    public static string? OptionalCode(string? value, string parameterName, int maxLength)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : NormalizeCode(value, parameterName, maxLength);
    }

    public static string? OptionalIdentifier(string? value, string parameterName, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim();
        EnsureLength(normalized, parameterName, maxLength);
        EnsureNoControlCharacters(normalized, parameterName);
        return normalized;
    }

    public static IReadOnlyDictionary<string, string> Attributes(
        IReadOnlyDictionary<string, string>? attributes)
    {
        if (attributes is null || attributes.Count == 0)
        {
            return new ReadOnlyDictionary<string, string>(
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase));
        }

        if (attributes.Count > AuditEvent.MaxAttributeCount)
        {
            throw new ArgumentOutOfRangeException(
                nameof(attributes),
                $"Audit attributes cannot exceed {AuditEvent.MaxAttributeCount} entries.");
        }

        var copy = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var pair in attributes)
        {
            var key = RequiredCode(
                pair.Key,
                nameof(attributes),
                AuditEvent.MaxAttributeNameLength);
            var normalizedSensitiveName = NormalizeSensitiveName(key);
            if (SensitiveAttributeNames.Contains(normalizedSensitiveName))
            {
                throw new ArgumentException(
                    $"Audit attribute '{key}' is reserved for sensitive data and cannot be recorded.",
                    nameof(attributes));
            }

            if (pair.Value is null)
            {
                throw new ArgumentException(
                    $"Audit attribute '{key}' cannot contain a null value.",
                    nameof(attributes));
            }

            var value = pair.Value.Trim();
            EnsureLength(value, nameof(attributes), AuditEvent.MaxAttributeValueLength);
            EnsureNoControlCharacters(value, nameof(attributes));

            if (!copy.TryAdd(key, value))
            {
                throw new ArgumentException(
                    $"Duplicate audit attribute '{key}'.",
                    nameof(attributes));
            }
        }

        return new ReadOnlyDictionary<string, string>(copy);
    }

    private static string NormalizeCode(string value, string parameterName, int maxLength)
    {
        var normalized = value.Trim();
        EnsureLength(normalized, parameterName, maxLength);

        foreach (var character in normalized)
        {
            if (char.IsLetterOrDigit(character) || character is '.' or '_' or '-' or ':' or '/')
            {
                continue;
            }

            throw new ArgumentException(
                "Audit codes may contain only letters, digits, '.', '_', '-', ':', or '/'.",
                parameterName);
        }

        return normalized;
    }

    private static string NormalizeSensitiveName(string value)
    {
        return string.Concat(value.Where(char.IsLetterOrDigit)).ToLowerInvariant();
    }

    private static void EnsureLength(string value, string parameterName, int maxLength)
    {
        if (value.Length > maxLength)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                $"Value cannot exceed {maxLength} characters.");
        }
    }

    private static void EnsureNoControlCharacters(string value, string parameterName)
    {
        if (value.Any(char.IsControl))
        {
            throw new ArgumentException("Audit values cannot contain control characters.", parameterName);
        }
    }
}
