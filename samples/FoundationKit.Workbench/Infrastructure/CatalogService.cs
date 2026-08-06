using System.Text.Json;

namespace FoundationKit.Workbench.Infrastructure;

public sealed class CatalogService
{
    private readonly string _catalogPath = Path.Combine(
        AppContext.BaseDirectory,
        "catalog",
        "foundationkit.catalog.json");

    public async Task<JsonElement> ReadAsync(CancellationToken cancellationToken = default)
    {
        await using var stream = File.OpenRead(_catalogPath);
        using var document = await JsonDocument.ParseAsync(
            stream,
            cancellationToken: cancellationToken);
        return document.RootElement.Clone();
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
