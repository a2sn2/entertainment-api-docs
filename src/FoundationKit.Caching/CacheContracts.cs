namespace FoundationKit.Caching;

public sealed record CacheKey
{
    public const int MaximumLength = 240;

    public CacheKey(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        var normalized = value.Trim().ToLowerInvariant();

        if (normalized.Length > MaximumLength)
        {
            throw new ArgumentOutOfRangeException(
                nameof(value),
                $"Cache key cannot exceed {MaximumLength} characters.");
        }

        if (!char.IsLetterOrDigit(normalized[0])
            || normalized.Any(character =>
                !(char.IsLetterOrDigit(character)
                  || character is '.' or ':' or '-' or '_' or '/')))
        {
            throw new ArgumentException(
                "Cache key must start with a letter or digit and contain only letters, digits, '.', ':', '-', '_', or '/'.",
                nameof(value));
        }

        Value = normalized;
    }

    public string Value { get; }

    public override string ToString() => Value;
}

public sealed record CacheEntryOptions
{
    public CacheEntryOptions(TimeSpan timeToLive)
    {
        if (timeToLive <= TimeSpan.Zero || timeToLive == TimeSpan.MaxValue)
        {
            throw new ArgumentOutOfRangeException(
                nameof(timeToLive),
                "Cache time-to-live must be a finite positive duration.");
        }

        TimeToLive = timeToLive;
    }

    public TimeSpan TimeToLive { get; }
}

public sealed class CacheReadResult
{
    private readonly byte[] _value;

    private CacheReadResult(bool found, byte[] value)
    {
        Found = found;
        _value = value;
    }

    public bool Found { get; }

    public ReadOnlyMemory<byte> Value => _value.ToArray();

    public static CacheReadResult Miss { get; } = new(false, []);

    public static CacheReadResult Hit(ReadOnlySpan<byte> value) =>
        new(true, value.ToArray());

    public override string ToString() => Found ? "Hit" : "Miss";
}

public interface ICacheStore
{
    ValueTask<CacheReadResult> GetAsync(
        CacheKey key,
        CancellationToken cancellationToken = default);

    ValueTask SetAsync(
        CacheKey key,
        ReadOnlyMemory<byte> value,
        CacheEntryOptions options,
        CancellationToken cancellationToken = default);

    ValueTask RemoveAsync(
        CacheKey key,
        CancellationToken cancellationToken = default);
}
