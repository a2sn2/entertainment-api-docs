namespace FoundationKit.Settings;

public interface ISettingSource
{
    ValueTask<SettingEntry?> FindAsync(
        string key,
        SettingScope scope,
        CancellationToken cancellationToken = default);
}

public interface ISettingReader
{
    ValueTask<ResolvedSetting?> ResolveAsync(
        string key,
        SettingResolutionContext context,
        CancellationToken cancellationToken = default);
}

public sealed class SettingReader(ISettingSource source) : ISettingReader
{
    private readonly ISettingSource _source = source
        ?? throw new ArgumentNullException(nameof(source));

    public async ValueTask<ResolvedSetting?> ResolveAsync(
        string key,
        SettingResolutionContext context,
        CancellationToken cancellationToken = default)
    {
        var normalizedKey = SettingKey.Normalize(key);
        ArgumentNullException.ThrowIfNull(context);

        foreach (var scope in context.Scopes)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var entry = await _source.FindAsync(
                normalizedKey,
                scope,
                cancellationToken).ConfigureAwait(false);

            if (entry is not null)
            {
                return new ResolvedSetting(entry.Key, entry.Value, entry.Scope);
            }
        }

        return null;
    }
}

public sealed class CompositeSettingSource : ISettingSource
{
    public const int MaximumSources = 16;

    private readonly IReadOnlyList<ISettingSource> _sources;

    public CompositeSettingSource(IEnumerable<ISettingSource> sources)
    {
        ArgumentNullException.ThrowIfNull(sources);
        var materialized = sources.ToArray();

        if (materialized.Length == 0)
        {
            throw new ArgumentException(
                "At least one setting source is required.",
                nameof(sources));
        }

        if (materialized.Length > MaximumSources)
        {
            throw new ArgumentOutOfRangeException(
                nameof(sources),
                $"A composite setting source cannot exceed {MaximumSources} sources.");
        }

        if (materialized.Any(source => source is null))
        {
            throw new ArgumentException(
                "Setting sources cannot contain null entries.",
                nameof(sources));
        }

        _sources = Array.AsReadOnly(materialized);
    }

    public async ValueTask<SettingEntry?> FindAsync(
        string key,
        SettingScope scope,
        CancellationToken cancellationToken = default)
    {
        var normalizedKey = SettingKey.Normalize(key);
        ArgumentNullException.ThrowIfNull(scope);

        foreach (var source in _sources)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var entry = await source.FindAsync(
                normalizedKey,
                scope,
                cancellationToken).ConfigureAwait(false);

            if (entry is not null)
            {
                return entry;
            }
        }

        return null;
    }
}

public sealed class InMemorySettingSource : ISettingSource
{
    private readonly IReadOnlyDictionary<SettingAddress, SettingEntry> _entries;

    public InMemorySettingSource(IEnumerable<SettingEntry> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);

        var dictionary = new Dictionary<SettingAddress, SettingEntry>();
        foreach (var entry in entries)
        {
            ArgumentNullException.ThrowIfNull(entry);
            var address = SettingAddress.From(entry.Scope, entry.Key);
            if (!dictionary.TryAdd(address, entry))
            {
                throw new ArgumentException(
                    $"Duplicate setting entry '{entry}' is not allowed.",
                    nameof(entries));
            }
        }

        _entries = dictionary;
    }

    public ValueTask<SettingEntry?> FindAsync(
        string key,
        SettingScope scope,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var normalizedKey = SettingKey.Normalize(key);
        ArgumentNullException.ThrowIfNull(scope);

        _entries.TryGetValue(
            SettingAddress.From(scope, normalizedKey),
            out var entry);
        return ValueTask.FromResult(entry);
    }

    private readonly record struct SettingAddress(
        string ScopeKind,
        string? ScopeIdentifier,
        string Key)
    {
        public static SettingAddress From(SettingScope scope, string key) =>
            new(scope.Kind, scope.Identifier, SettingKey.Normalize(key));
    }
}
