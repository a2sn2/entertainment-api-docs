using FoundationKit.Settings;
using Xunit;

namespace FoundationKit.Tests;

public sealed class SettingsCapabilityTests
{
    [Fact]
    public async Task Reader_prefers_the_most_specific_scope_and_never_exposes_values_in_diagnostics()
    {
        var userScope = new SettingScope("user", "user-42");
        var organizationScope = new SettingScope("organization", "org-7");
        var source = new InMemorySettingSource(
        [
            new SettingEntry(SettingScope.Global, "experience.theme", "global-secret-like-value"),
            new SettingEntry(organizationScope, "experience.theme", "organization-value"),
            new SettingEntry(userScope, "experience.theme", "user-value")
        ]);
        var reader = new SettingReader(source);

        var result = await reader.ResolveAsync(
            " EXPERIENCE.THEME ",
            new SettingResolutionContext([userScope, organizationScope]));

        Assert.NotNull(result);
        Assert.Equal("experience.theme", result.Key);
        Assert.Equal("user-value", result.Value);
        Assert.Equal(userScope, result.Scope);
        Assert.DoesNotContain("user-value", result.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Composite_source_uses_declared_source_precedence_within_the_same_scope()
    {
        var primary = new InMemorySettingSource(
        [
            new SettingEntry(SettingScope.Global, "service.mode", "primary")
        ]);
        var fallback = new InMemorySettingSource(
        [
            new SettingEntry(SettingScope.Global, "service.mode", "fallback")
        ]);
        var reader = new SettingReader(new CompositeSettingSource([primary, fallback]));

        var result = await reader.ResolveAsync(
            "service.mode",
            SettingResolutionContext.Global);

        Assert.NotNull(result);
        Assert.Equal("primary", result.Value);
    }

    [Fact]
    public async Task Scope_specificity_precedes_source_priority()
    {
        var userScope = new SettingScope("user", "user-42");
        var primary = new InMemorySettingSource(
        [
            new SettingEntry(SettingScope.Global, "service.mode", "global-primary")
        ]);
        var scopedFallback = new InMemorySettingSource(
        [
            new SettingEntry(userScope, "service.mode", "user-fallback")
        ]);
        var reader = new SettingReader(
            new CompositeSettingSource([primary, scopedFallback]));

        var result = await reader.ResolveAsync(
            "service.mode",
            new SettingResolutionContext([userScope]));

        Assert.NotNull(result);
        Assert.Equal("user-fallback", result.Value);
        Assert.Equal(userScope, result.Scope);
    }

    [Fact]
    public void Settings_reject_duplicate_addresses_and_unsafe_keys()
    {
        Assert.Throws<ArgumentException>(() =>
            new InMemorySettingSource(
            [
                new SettingEntry(SettingScope.Global, "service.mode", "one"),
                new SettingEntry(SettingScope.Global, "SERVICE.MODE", "two")
            ]));

        Assert.Throws<ArgumentException>(() => SettingKey.Normalize("service mode"));
        Assert.Throws<ArgumentException>(() => new SettingScope("global", "not-allowed"));
    }

    [Fact]
    public void Setting_diagnostics_omit_the_setting_value()
    {
        var entry = new SettingEntry(
            SettingScope.Global,
            "security.example",
            "must-not-appear-in-diagnostics");

        Assert.DoesNotContain(
            "must-not-appear-in-diagnostics",
            entry.ToString(),
            StringComparison.Ordinal);
    }
}
