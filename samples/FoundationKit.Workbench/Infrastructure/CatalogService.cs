using System.Text.Json;
using FoundationKit.Caching;
using FoundationKit.Workbench.Application.Shared;

namespace FoundationKit.Workbench.Infrastructure;

public sealed class CatalogService : ICapabilityCatalog
{
    private const string CatalogResourceName =
        "FoundationKit.Workbench.Catalog.foundationkit.catalog.json";

    private static readonly CacheKey CatalogCacheKey =
        new("workbench/catalog/embedded-v1");

    private static readonly CacheEntryOptions CatalogCacheOptions =
        new(TimeSpan.FromMinutes(15));

    private readonly ICacheStore _cacheStore;

    public CatalogService(ICacheStore cacheStore)
    {
        ArgumentNullException.ThrowIfNull(cacheStore);
        _cacheStore = cacheStore;
    }

    public async Task<JsonElement> ReadAsync(CancellationToken cancellationToken = default)
    {
        var cached = await _cacheStore.GetAsync(CatalogCacheKey, cancellationToken);
        if (cached.Found)
        {
            using var cachedDocument = JsonDocument.Parse(cached.Value);
            return cachedDocument.RootElement.Clone();
        }

        await using var stream = typeof(CatalogService).Assembly
            .GetManifestResourceStream(CatalogResourceName)
            ?? throw new InvalidOperationException(
                $"Embedded catalog resource '{CatalogResourceName}' was not found.");

        using var buffer = new MemoryStream();
        await stream.CopyToAsync(buffer, cancellationToken);
        var payload = buffer.ToArray();

        using var document = JsonDocument.Parse(payload);
        var result = document.RootElement.Clone();

        await _cacheStore.SetAsync(
            CatalogCacheKey,
            payload,
            CatalogCacheOptions,
            cancellationToken);

        return result;
    }

    public async Task<IReadOnlySet<string>> ReadCapabilityIdsAsync(
        CancellationToken cancellationToken = default)
    {
        var catalog = await ReadAsync(cancellationToken);
        var ids = catalog
            .GetProperty("packages")
            .EnumerateArray()
            .SelectMany(package => package.GetProperty("capabilities").EnumerateArray())
            .Select(capability => capability.GetProperty("id").GetString())
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => id!)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return ids;
    }
}
