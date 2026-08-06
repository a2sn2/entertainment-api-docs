using System.Text;
using System.Text.Json;

namespace FoundationKit.CatalogGenerator;

internal static class Program
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    public static async Task<int> Main(string[] args)
    {
        var arguments = args.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var checkOnly = arguments.Contains("--check");
        var rootArgumentIndex = Array.FindIndex(
            args,
            value => value.Equals("--root", StringComparison.OrdinalIgnoreCase));
        var repositoryRoot = rootArgumentIndex >= 0 && rootArgumentIndex + 1 < args.Length
            ? Path.GetFullPath(args[rootArgumentIndex + 1])
            : FindRepositoryRoot(AppContext.BaseDirectory);

        var catalogPath = Path.Combine(repositoryRoot, "catalog", "foundationkit.catalog.json");
        var outputPath = Path.Combine(repositoryRoot, "docs", "FEATURES.md");

        var json = await File.ReadAllTextAsync(catalogPath);
        var catalog = JsonSerializer.Deserialize<CatalogDocument>(json, JsonOptions)
            ?? throw new InvalidOperationException("The FoundationKit catalog is empty.");

        Validate(catalog);
        var generated = GenerateMarkdown(catalog);

        if (checkOnly)
        {
            if (!File.Exists(outputPath))
                throw new InvalidOperationException($"Generated documentation is missing: {outputPath}");

            var current = await File.ReadAllTextAsync(outputPath);
            if (!string.Equals(Normalize(current), Normalize(generated), StringComparison.Ordinal))
            {
                Console.Error.WriteLine("docs/FEATURES.md is out of date. Run:");
                Console.Error.WriteLine("  dotnet run --project tools/FoundationKit.CatalogGenerator");
                return 1;
            }

            Console.WriteLine("Catalog validation and generated documentation check passed.");
            return 0;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
        await File.WriteAllTextAsync(outputPath, generated, new UTF8Encoding(false));
        Console.WriteLine(
            $"Generated {Path.GetRelativePath(repositoryRoot, outputPath)} from the canonical catalog.");
        return 0;
    }

    private static string FindRepositoryRoot(string startPath)
    {
        var directory = new DirectoryInfo(startPath);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "FoundationKit.sln")))
                return directory.FullName;
            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the FoundationKit repository root.");
    }

    private static void Validate(CatalogDocument catalog)
    {
        if (catalog.SchemaVersion != 1)
            throw new InvalidOperationException(
                $"Unsupported catalog schema version: {catalog.SchemaVersion}.");

        if (string.IsNullOrWhiteSpace(catalog.CoreVersion))
            throw new InvalidOperationException("coreVersion is required.");

        if (catalog.Packages.Count == 0)
            throw new InvalidOperationException("At least one package is required.");

        var packageIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var capabilityIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var package in catalog.Packages)
        {
            if (!packageIds.Add(package.PackageId))
                throw new InvalidOperationException($"Duplicate packageId: {package.PackageId}.");

            if (package.Capabilities.Count == 0)
                throw new InvalidOperationException(
                    $"Package {package.PackageId} has no capabilities.");

            foreach (var capability in package.Capabilities)
            {
                if (!capabilityIds.Add(capability.Id))
                    throw new InvalidOperationException(
                        $"Duplicate capability id: {capability.Id}.");

                if (!capability.Status.Equals("implemented", StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException(
                        $"Catalog capability {capability.Id} is not implemented. " +
                        "Design intent and future work must not be listed as implemented behavior.");
                }
            }
        }

        var ideaIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var idea in catalog.Ideas)
        {
            if (!ideaIds.Add(idea.Id))
                throw new InvalidOperationException($"Duplicate idea id: {idea.Id}.");

            foreach (var capabilityId in idea.RecommendedCapabilityIds)
            {
                if (!capabilityIds.Contains(capabilityId))
                {
                    throw new InvalidOperationException(
                        $"Idea {idea.Id} references unknown capability {capabilityId}.");
                }
            }
        }
    }

    private static string GenerateMarkdown(CatalogDocument catalog)
    {
        var builder = new StringBuilder();
        builder.AppendLine("# FoundationKit Capabilities");
        builder.AppendLine();
        builder.AppendLine(
            "> Generated from `catalog/foundationkit.catalog.json`. Do not edit this file manually.");
        builder.AppendLine();
        builder.AppendLine($"Core version: `{catalog.CoreVersion}`");
        builder.AppendLine();
        builder.AppendLine(
            "Only implemented behavior is listed below. Product-specific concerns and future " +
            "recommendations remain outside the reusable core.");
        builder.AppendLine();

        foreach (var package in catalog.Packages)
        {
            builder.AppendLine($"## {package.PackageId}");
            builder.AppendLine();
            builder.AppendLine(package.SummaryEn);
            builder.AppendLine();

            foreach (var capability in package.Capabilities)
            {
                builder.AppendLine($"### {capability.TitleEn}");
                builder.AppendLine();
                builder.AppendLine(capability.DescriptionEn);
                builder.AppendLine();
                builder.AppendLine(
                    $"Public surface: {string.Join(", ", capability.PublicTypes.Select(type => $"`{type}`"))}");
                builder.AppendLine();
            }
        }

        builder.AppendLine("## Project ideas");
        builder.AppendLine();
        foreach (var idea in catalog.Ideas)
            builder.AppendLine($"- **{idea.TitleEn}** — {idea.DescriptionEn}");

        builder.AppendLine();
        builder.AppendLine("## Keeping the catalog current");
        builder.AppendLine();
        builder.AppendLine("When an implemented public capability changes:");
        builder.AppendLine();
        builder.AppendLine("1. update the code and tests;");
        builder.AppendLine("2. update `catalog/foundationkit.catalog.json`;");
        builder.AppendLine(
            "3. run `dotnet run --project tools/FoundationKit.CatalogGenerator`;");
        builder.AppendLine("4. update `CHANGELOG.md`.");
        builder.AppendLine();

        return builder.ToString();
    }

    private static string Normalize(string value) =>
        value.Replace("\r\n", "\n", StringComparison.Ordinal);
}

internal sealed record CatalogDocument(
    int SchemaVersion,
    string CoreVersion,
    DateTimeOffset UpdatedUtc,
    Contact Contact,
    IReadOnlyList<CatalogPackage> Packages,
    IReadOnlyList<ProjectIdea> Ideas,
    IReadOnlyList<AdoptionStep> AdoptionSteps);

internal sealed record Contact(
    string Name,
    string GithubProfile,
    string Repository,
    string NewIssue);

internal sealed record CatalogPackage(
    string Id,
    string PackageId,
    string TitleAr,
    string TitleEn,
    string SummaryAr,
    string SummaryEn,
    IReadOnlyList<Capability> Capabilities);

internal sealed record Capability(
    string Id,
    string TitleAr,
    string TitleEn,
    string DescriptionAr,
    string DescriptionEn,
    string Status,
    IReadOnlyList<string> PublicTypes);

internal sealed record ProjectIdea(
    string Id,
    string Icon,
    string TitleAr,
    string TitleEn,
    string DescriptionAr,
    string DescriptionEn,
    IReadOnlyList<string> RecommendedCapabilityIds,
    IReadOnlyList<string> ProductDecisions);

internal sealed record AdoptionStep(
    int Number,
    string TitleAr,
    string TitleEn,
    string DescriptionAr,
    string DescriptionEn,
    string? Command);
