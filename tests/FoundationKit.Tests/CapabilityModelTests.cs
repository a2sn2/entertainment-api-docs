using FoundationKit.Application.Capabilities;

namespace FoundationKit.Tests;

public sealed class CapabilityModelTests
{
    [Fact]
    public void Approval_capability_resolves_required_dependencies()
    {
        var resolver = CapabilityResolver.CreateDefault();

        var resolved = resolver.Resolve([FoundationCapabilityIds.Approvals]);
        var ids = resolved.Select(capability => capability.Id).ToArray();

        Assert.Contains(FoundationCapabilityIds.Kernel, ids);
        Assert.Contains(FoundationCapabilityIds.Security, ids);
        Assert.Contains(FoundationCapabilityIds.Identity, ids);
        Assert.Contains(FoundationCapabilityIds.Authorization, ids);
        Assert.Contains(FoundationCapabilityIds.Auditing, ids);
        Assert.Contains(FoundationCapabilityIds.Workflow, ids);
        Assert.Equal(FoundationCapabilityIds.Approvals, ids[^1]);
    }

    [Fact]
    public void Resolver_deduplicates_transitive_dependencies()
    {
        var resolver = CapabilityResolver.CreateDefault();

        var resolved = resolver.Resolve(
            [FoundationCapabilityIds.Approvals, FoundationCapabilityIds.Documents]);

        Assert.Single(resolved.Where(capability => capability.Id == FoundationCapabilityIds.Auditing));
        Assert.Single(resolved.Where(capability => capability.Id == FoundationCapabilityIds.Authorization));
    }

    [Fact]
    public void Selection_allows_removing_independent_profile_capability()
    {
        var resolver = CapabilityResolver.CreateDefault();

        var resolved = resolver.ResolveSelection(
            FoundationCapabilityProfiles.Standard,
            exclude: [FoundationCapabilityIds.Localization]);

        Assert.DoesNotContain(resolved, capability => capability.Id == FoundationCapabilityIds.Localization);
        Assert.Contains(resolved, capability => capability.Id == FoundationCapabilityIds.Identity);
    }

    [Fact]
    public void Selection_rejects_excluding_required_dependency()
    {
        var resolver = CapabilityResolver.CreateDefault();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            resolver.ResolveSelection(
                FoundationCapabilityProfiles.Standard,
                include: [FoundationCapabilityIds.Approvals],
                exclude: [FoundationCapabilityIds.Auditing]));

        Assert.Contains(FoundationCapabilityIds.Auditing, exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Unknown_capability_is_rejected()
    {
        var resolver = CapabilityResolver.CreateDefault();

        Assert.Throws<KeyNotFoundException>(() => resolver.Resolve(["not-a-capability"]));
    }

    [Fact]
    public void Dependency_cycle_is_rejected()
    {
        CapabilityDescriptor[] descriptors =
        [
            new("a", "A", CapabilityKind.Optional, CapabilityMaturity.Planned, "Test", "A", ["b"]),
            new("b", "B", CapabilityKind.Optional, CapabilityMaturity.Planned, "Test", "B", ["a"])
        ];
        var resolver = new CapabilityResolver(descriptors);

        Assert.Throws<InvalidOperationException>(() => resolver.Resolve(["a"]));
    }

    [Fact]
    public void Manifest_composes_profile_additions_and_provider_dependencies()
    {
        var resolver = CapabilityResolver.CreateDefault();
        var manifest = new FoundationKitProjectManifest(
            "Example",
            FoundationCapabilityProfiles.Minimal,
            [FoundationCapabilityIds.Caching],
            Array.Empty<string>(),
            [FoundationCapabilityIds.RedisProvider]);

        var resolved = manifest.Resolve(resolver);

        Assert.Contains(resolved, capability => capability.Id == FoundationCapabilityIds.Caching);
        Assert.Contains(resolved, capability => capability.Id == FoundationCapabilityIds.RedisProvider);
        Assert.Contains(resolved, capability => capability.Id == FoundationCapabilityIds.Kernel);
    }

    [Fact]
    public void Catalog_marks_future_features_without_claiming_implementation()
    {
        var workflow = FoundationCapabilityCatalog.All.Single(
            capability => capability.Id == FoundationCapabilityIds.Workflow);
        var kernel = FoundationCapabilityCatalog.All.Single(
            capability => capability.Id == FoundationCapabilityIds.Kernel);

        Assert.Equal(CapabilityMaturity.Planned, workflow.Maturity);
        Assert.Equal(CapabilityMaturity.Stable, kernel.Maturity);
    }
}
