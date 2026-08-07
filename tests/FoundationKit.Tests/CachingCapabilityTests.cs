using FoundationKit.Caching;
using Xunit;

namespace FoundationKit.Tests;

public sealed class CachingCapabilityTests
{
    [Fact]
    public void Cache_key_normalizes_and_rejects_unsafe_shapes()
    {
        Assert.Equal("workbench/catalog-v1", new CacheKey(" Workbench/Catalog-V1 ").Value);
        Assert.Throws<ArgumentException>(() => new CacheKey("cache key with spaces"));
        Assert.Throws<ArgumentException>(() => new CacheKey("@cache"));
    }

    [Fact]
    public async Task In_memory_store_returns_defensive_snapshot()
    {
        var store = new InMemoryCacheStore();
        var source = new byte[] { 1, 2, 3 };
        var key = new CacheKey("tests/value");

        await store.SetAsync(key, source, new CacheEntryOptions(TimeSpan.FromMinutes(5)));
        source[0] = 99;

        var first = await store.GetAsync(key);
        var firstBytes = first.Value.ToArray();
        firstBytes[1] = 88;
        var second = await store.GetAsync(key);

        Assert.True(first.Found);
        Assert.Equal(new byte[] { 1, 2, 3 }, first.Value.ToArray());
        Assert.Equal(new byte[] { 1, 2, 3 }, second.Value.ToArray());
        Assert.Equal("Hit", first.ToString());
    }

    [Fact]
    public async Task Expired_entry_becomes_a_miss_and_is_removed()
    {
        var clock = new ManualTimeProvider(new DateTimeOffset(2026, 8, 8, 0, 0, 0, TimeSpan.Zero));
        var store = new InMemoryCacheStore(timeProvider: clock);
        var key = new CacheKey("tests/expiry");

        await store.SetAsync(key, new byte[] { 7 }, new CacheEntryOptions(TimeSpan.FromMinutes(5)));
        clock.Advance(TimeSpan.FromMinutes(5));

        var result = await store.GetAsync(key);

        Assert.False(result.Found);
        Assert.Equal("Miss", result.ToString());
    }

    [Fact]
    public async Task Remove_is_idempotent()
    {
        var store = new InMemoryCacheStore();
        var key = new CacheKey("tests/remove");
        await store.SetAsync(key, new byte[] { 1 }, new CacheEntryOptions(TimeSpan.FromMinutes(1)));

        await store.RemoveAsync(key);
        await store.RemoveAsync(key);

        Assert.False((await store.GetAsync(key)).Found);
    }

    [Fact]
    public async Task Capacity_evicts_the_earliest_expiring_entry_deterministically()
    {
        var clock = new ManualTimeProvider(new DateTimeOffset(2026, 8, 8, 0, 0, 0, TimeSpan.Zero));
        var store = new InMemoryCacheStore(
            new InMemoryCacheOptions
            {
                MaximumEntries = 2,
                MaximumValueBytes = 32,
                MaximumTimeToLive = TimeSpan.FromHours(1)
            },
            clock);

        await store.SetAsync(new CacheKey("tests/a"), new byte[] { 1 }, new CacheEntryOptions(TimeSpan.FromMinutes(10)));
        await store.SetAsync(new CacheKey("tests/b"), new byte[] { 2 }, new CacheEntryOptions(TimeSpan.FromMinutes(20)));
        await store.SetAsync(new CacheKey("tests/c"), new byte[] { 3 }, new CacheEntryOptions(TimeSpan.FromMinutes(30)));

        Assert.False((await store.GetAsync(new CacheKey("tests/a"))).Found);
        Assert.True((await store.GetAsync(new CacheKey("tests/b"))).Found);
        Assert.True((await store.GetAsync(new CacheKey("tests/c"))).Found);
    }

    [Fact]
    public async Task Provider_rejects_values_and_ttl_above_configured_bounds()
    {
        var store = new InMemoryCacheStore(new InMemoryCacheOptions
        {
            MaximumEntries = 2,
            MaximumValueBytes = 2,
            MaximumTimeToLive = TimeSpan.FromMinutes(5)
        });
        var key = new CacheKey("tests/bounds");

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () =>
            await store.SetAsync(key, new byte[] { 1, 2, 3 }, new CacheEntryOptions(TimeSpan.FromMinutes(1))));

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () =>
            await store.SetAsync(key, new byte[] { 1 }, new CacheEntryOptions(TimeSpan.FromMinutes(6))));
    }

    [Fact]
    public async Task Caller_cancellation_remains_cancellation()
    {
        var store = new InMemoryCacheStore();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await store.GetAsync(new CacheKey("tests/cancel"), cancellation.Token));
    }

    private sealed class ManualTimeProvider(DateTimeOffset now) : TimeProvider
    {
        private DateTimeOffset _now = now;

        public override DateTimeOffset GetUtcNow() => _now;

        public void Advance(TimeSpan duration) => _now = _now.Add(duration);
    }
}
