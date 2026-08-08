namespace FoundationKit.Caching;

public sealed class InMemoryCacheOptions
{
    public int MaximumEntries { get; init; } = 1_024;

    public int MaximumValueBytes { get; init; } = 1_048_576;

    public TimeSpan MaximumTimeToLive { get; init; } = TimeSpan.FromDays(30);

    internal void Validate()
    {
        if (MaximumEntries is < 1 or > 100_000)
        {
            throw new ArgumentOutOfRangeException(
                nameof(MaximumEntries),
                "MaximumEntries must be between 1 and 100000.");
        }

        if (MaximumValueBytes is < 1 or > 16_777_216)
        {
            throw new ArgumentOutOfRangeException(
                nameof(MaximumValueBytes),
                "MaximumValueBytes must be between 1 byte and 16 MiB.");
        }

        if (MaximumTimeToLive <= TimeSpan.Zero
            || MaximumTimeToLive == TimeSpan.MaxValue)
        {
            throw new ArgumentOutOfRangeException(
                nameof(MaximumTimeToLive),
                "MaximumTimeToLive must be a finite positive duration.");
        }
    }
}

public sealed class InMemoryCacheStore : ICacheStore
{
    private readonly Dictionary<string, CacheItem> _entries =
        new(StringComparer.Ordinal);
    private readonly object _gate = new();
    private readonly TimeProvider _timeProvider;
    private readonly InMemoryCacheOptions _options;

    public InMemoryCacheStore(
        InMemoryCacheOptions? options = null,
        TimeProvider? timeProvider = null)
    {
        _options = options ?? new InMemoryCacheOptions();
        _options.Validate();
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public ValueTask<CacheReadResult> GetAsync(
        CacheKey key,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(key);
        cancellationToken.ThrowIfCancellationRequested();

        lock (_gate)
        {
            if (!_entries.TryGetValue(key.Value, out var item))
            {
                return ValueTask.FromResult(CacheReadResult.Miss);
            }

            if (item.ExpiresUtc <= _timeProvider.GetUtcNow())
            {
                _entries.Remove(key.Value);
                return ValueTask.FromResult(CacheReadResult.Miss);
            }

            return ValueTask.FromResult(CacheReadResult.Hit(item.Value));
        }
    }

    public ValueTask SetAsync(
        CacheKey key,
        ReadOnlyMemory<byte> value,
        CacheEntryOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(options);
        cancellationToken.ThrowIfCancellationRequested();

        if (value.Length > _options.MaximumValueBytes)
        {
            throw new ArgumentOutOfRangeException(
                nameof(value),
                $"Cache value cannot exceed {_options.MaximumValueBytes} bytes for this provider instance.");
        }

        if (options.TimeToLive > _options.MaximumTimeToLive)
        {
            throw new ArgumentOutOfRangeException(
                nameof(options),
                $"Cache time-to-live cannot exceed {_options.MaximumTimeToLive} for this provider instance.");
        }

        var now = _timeProvider.GetUtcNow();
        var maximumSafeTimeToLive = DateTimeOffset.MaxValue - now;
        if (options.TimeToLive > maximumSafeTimeToLive)
        {
            throw new ArgumentOutOfRangeException(
                nameof(options),
                "Cache time-to-live exceeds the DateTimeOffset range supported from the current provider time.");
        }

        var expiresUtc = now.Add(options.TimeToLive);
        var snapshot = value.ToArray();

        lock (_gate)
        {
            RemoveExpiredUnsafe(now);

            if (!_entries.ContainsKey(key.Value)
                && _entries.Count >= _options.MaximumEntries)
            {
                EvictEarliestExpiryUnsafe();
            }

            _entries[key.Value] = new CacheItem(snapshot, expiresUtc);
        }

        return ValueTask.CompletedTask;
    }

    public ValueTask RemoveAsync(
        CacheKey key,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(key);
        cancellationToken.ThrowIfCancellationRequested();

        lock (_gate)
        {
            _entries.Remove(key.Value);
        }

        return ValueTask.CompletedTask;
    }

    private void RemoveExpiredUnsafe(DateTimeOffset now)
    {
        var expired = _entries
            .Where(pair => pair.Value.ExpiresUtc <= now)
            .Select(pair => pair.Key)
            .ToArray();

        foreach (var key in expired)
        {
            _entries.Remove(key);
        }
    }

    private void EvictEarliestExpiryUnsafe()
    {
        var candidate = _entries
            .OrderBy(pair => pair.Value.ExpiresUtc)
            .ThenBy(pair => pair.Key, StringComparer.Ordinal)
            .First();

        _entries.Remove(candidate.Key);
    }

    private sealed record CacheItem(
        byte[] Value,
        DateTimeOffset ExpiresUtc);
}
