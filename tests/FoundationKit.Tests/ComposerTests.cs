using FoundationKit.Application.Capabilities;
using FoundationKit.Composer;

namespace FoundationKit.Tests;

public sealed class ComposerTests
{
    [Fact]
    public void Parser_accepts_strict_v1_manifest()
    {
        var manifest = ComposerManifestParser.Parse(
            """
            {
              "schemaVersion": 1,
              "name": "CustomerPortal",
              "profile": "minimal",
              "includeCapabilities": ["auditing"],
              "excludeCapabilities": [],
              "providers": ["provider-sqlserver"]
            }
            """);

        Assert.Equal(1, manifest.SchemaVersion);
        Assert.Equal("CustomerPortal", manifest.Name);
        Assert.Equal(FoundationCapabilityProfiles.Minimal, manifest.Profile);
        Assert.Equal([FoundationCapabilityIds.Auditing], manifest.IncludeCapabilities);
        Assert.Equal([FoundationCapabilityIds.SqlServerProvider], manifest.Providers);
    }

    [Fact]
    public void Parser_rejects_unknown_json_fields()
    {
        var exception = Assert.Throws<ComposerManifestException>(() =>
            ComposerManifestParser.Parse(
                """
                {
                  "schemaVersion": 1,
                  "name": "CustomerPortal",
                  "profile": "minimal",
                  "includeCapabilities": [],
                  "excludeCapabilities": [],
                  "providers": [],
                  "surprise": true
                }
                """));

        Assert.Contains("not valid FoundationKit JSON", exception.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("9Invalid")]
    [InlineData("Invalid Name")]
    [InlineData("Invalid/Name")]
    public void Parser_rejects_unsafe_project_names(string projectName)
    {
        var json = $$"""
            {
              "schemaVersion": 1,
              "name": "{{projectName}}",
              "profile": "minimal",
              "includeCapabilities": [],
              "excludeCapabilities": [],
              "providers": []
            }
            """;

        Assert.Throws<ComposerManifestException>(() => ComposerManifestParser.Parse(json));
    }

    [Fact]
    public void Analyzer_rejects_provider_in_capability_list()
    {
        var manifest = ComposerManifestParser.Parse(
            """
            {
              "schemaVersion": 1,
              "name": "CustomerPortal",
              "profile": "minimal",
              "includeCapabilities": ["provider-sqlserver"],
              "excludeCapabilities": [],
              "providers": []
            }
            """);

        var exception = Assert.Throws<ComposerManifestException>(() =>
            CompositionAnalyzer.Analyze(manifest));

        Assert.Contains("must be listed under 'providers'", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Analyzer_explains_transitive_approval_dependencies()
    {
        var manifest = new ComposerManifest(
            1,
            "ApprovalSystem",
            FoundationCapabilityProfiles.Minimal,
            [FoundationCapabilityIds.Approvals],
            Array.Empty<string>(),
            Array.Empty<string>());

        var analysis = CompositionAnalyzer.Analyze(manifest);
        var authorization = analysis.Entries.Single(
            entry => entry.Capability.Id == FoundationCapabilityIds.Authorization);
        var auditing = analysis.Entries.Single(
            entry => entry.Capability.Id == FoundationCapabilityIds.Auditing);

        Assert.Contains("required-by:approvals", authorization.Reasons);
        Assert.Contains("required-by:workflow", auditing.Reasons);
        Assert.False(analysis.IsStableOnly);
    }

    [Fact]
    public async Task Cli_validate_returns_warning_but_success_for_nonstable_selection()
    {
        var path = await WriteManifestAsync(
            """
            {
              "schemaVersion": 1,
              "name": "MinimalApi",
              "profile": "minimal",
              "includeCapabilities": [],
              "excludeCapabilities": [],
              "providers": []
            }
            """);

        try
        {
            using var output = new StringWriter();
            using var error = new StringWriter();

            var exitCode = await ComposerCli.RunAsync(
                ["validate", path],
                output,
                error);

            Assert.Equal(0, exitCode);
            Assert.Contains("Manifest valid", output.ToString(), StringComparison.Ordinal);
            Assert.Contains("WARNING", output.ToString(), StringComparison.Ordinal);
            Assert.Equal(string.Empty, error.ToString());
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task Cli_require_stable_fails_closed_when_profile_contains_preview_capability()
    {
        var path = await WriteManifestAsync(
            """
            {
              "schemaVersion": 1,
              "name": "MinimalApi",
              "profile": "minimal",
              "includeCapabilities": [],
              "excludeCapabilities": [],
              "providers": []
            }
            """);

        try
        {
            using var output = new StringWriter();
            using var error = new StringWriter();

            var exitCode = await ComposerCli.RunAsync(
                ["validate", path, "--require-stable"],
                output,
                error);

            Assert.Equal(3, exitCode);
            Assert.Contains("NOT READY", output.ToString(), StringComparison.Ordinal);
            Assert.Equal(string.Empty, error.ToString());
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task Cli_explain_outputs_dependency_reason_without_echoing_manifest_json()
    {
        var path = await WriteManifestAsync(
            """
            {
              "schemaVersion": 1,
              "name": "ApprovalSystem",
              "profile": "minimal",
              "includeCapabilities": ["approvals"],
              "excludeCapabilities": [],
              "providers": []
            }
            """);

        try
        {
            using var output = new StringWriter();
            using var error = new StringWriter();

            var exitCode = await ComposerCli.RunAsync(
                ["explain", path],
                output,
                error);
            var text = output.ToString();

            Assert.Equal(0, exitCode);
            Assert.Contains("authorization", text, StringComparison.Ordinal);
            Assert.Contains("required-by:approvals", text, StringComparison.Ordinal);
            Assert.DoesNotContain("schemaVersion", text, StringComparison.Ordinal);
            Assert.Equal(string.Empty, error.ToString());
        }
        finally
        {
            File.Delete(path);
        }
    }

    private static async Task<string> WriteManifestAsync(string json)
    {
        var path = Path.Combine(
            Path.GetTempPath(),
            $"foundationkit-composer-{Guid.NewGuid():N}.json");
        await File.WriteAllTextAsync(path, json);
        return path;
    }
}
