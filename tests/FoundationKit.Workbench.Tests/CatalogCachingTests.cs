using FoundationKit.Caching;
using FoundationKit.Workbench.Infrastructure;

namespace FoundationKit.Workbench.Tests;

public sealed class CatalogCachingTests
{
    [Fact]
    public async Task Catalog_service_fills_cache_once_and_uses_hit_on_second_read()
    {
        var cache = new RecordingCacheStore(new InMemoryCacheStore());
        var service = new CatalogService(cache);

        var first = await service.ReadAsync();
        var second = await service.ReadAsync();

        Assert.Equal(first.GetRawText(), second.GetRawText());
        Assert.Equal(2, cache.GetCount);
        Assert.Equal(1, cache.SetCount);
        Assert.Equal(0, cache.RemoveCount);
        Assert.Equal("workbench/catalog/embedded-v1", cache.LastKey?.Value);
    }

    private sealed class RecordingCacheStore(ICacheStore inner) : ICacheStore
    {
        public int GetCount { get; private set; }

        public int SetCount { get; private set; }

        public int RemoveCount { get; private set; }

        public CacheKey? LastKey { get; private set; }

        public ValueTask<CacheReadResult> GetAsync(
            CacheKey key,
            CancellationToken cancellationToken = default)
        {
            GetCount++;
            LastKey = key;
            return inner.GetAsync(key, cancellationToken);
        }

        public ValueTask SetAsync(
            CacheKey key,
            ReadOnlyMemory<byte> value,
            CacheEntryOptions options,
            CancellationToken cancellationToken = default)
        {
            SetCount++;
            LastKey = key;
            return inner.SetAsync(key, value, options, cancellationToken);
        }

        public ValueTask RemoveAsync(
            CacheKey key,
            CancellationToken cancellationToken = default)
        {
            RemoveCount++;
            LastKey = key;
            return inner.RemoveAsync(key, cancellationToken);
        }
    }
}
