using FoundationKit.Localization;
using Xunit;

namespace FoundationKit.Tests;

public sealed class LocalizationCapabilityTests
{
    [Fact]
    public void Culture_definition_canonicalizes_name_and_derives_direction()
    {
        var culture = new CultureDefinition(" ar-ye ");

        Assert.Equal("ar-YE", culture.Name);
        Assert.Equal("ar", culture.ParentName);
        Assert.Equal(TextDirection.RightToLeft, culture.Direction);
    }

    [Fact]
    public void Left_to_right_culture_is_detected_from_bcl_metadata()
    {
        var culture = new CultureDefinition("en-US");

        Assert.Equal(TextDirection.LeftToRight, culture.Direction);
    }

    [Fact]
    public void Supported_culture_set_prefers_exact_match()
    {
        var cultures = new SupportedCultureSet(["ar-YE", "en-US"], "ar-YE");

        var resolution = cultures.Resolve("en-us");

        Assert.Equal("en-US", resolution.Culture.Name);
        Assert.Equal(CultureResolutionSource.Exact, resolution.Source);
    }

    [Fact]
    public void Supported_culture_set_can_fall_back_to_supported_parent()
    {
        var cultures = new SupportedCultureSet(["ar-YE", "en"], "ar-YE");

        var resolution = cultures.Resolve("en-US");

        Assert.Equal("en", resolution.Culture.Name);
        Assert.Equal(CultureResolutionSource.Parent, resolution.Source);
    }

    [Fact]
    public void Unsupported_culture_falls_back_to_explicit_default()
    {
        var cultures = new SupportedCultureSet(["ar-YE", "en-US"], "ar-YE");

        var resolution = cultures.Resolve("fr-FR");

        Assert.Equal("ar-YE", resolution.Culture.Name);
        Assert.Equal(CultureResolutionSource.Default, resolution.Source);
    }

    [Fact]
    public void Invalid_requested_culture_falls_back_with_explicit_provenance()
    {
        var cultures = new SupportedCultureSet(["ar-YE", "en-US"], "ar-YE");

        var resolution = cultures.Resolve("!!");

        Assert.Equal("ar-YE", resolution.Culture.Name);
        Assert.Equal(CultureResolutionSource.InvalidRequested, resolution.Source);
    }

    [Fact]
    public void Missing_requested_culture_uses_default()
    {
        var cultures = new SupportedCultureSet(["ar-YE", "en-US"], "ar-YE");

        var resolution = cultures.Resolve(null);

        Assert.Equal("ar-YE", resolution.Culture.Name);
        Assert.Equal(CultureResolutionSource.Default, resolution.Source);
    }

    [Fact]
    public void Duplicate_or_missing_default_cultures_are_rejected()
    {
        Assert.Throws<ArgumentException>(() =>
            new SupportedCultureSet(["ar-YE", "AR-ye"], "ar-YE"));

        Assert.Throws<ArgumentException>(() =>
            new SupportedCultureSet(["en-US"], "ar-YE"));
    }

    [Fact]
    public void Time_zone_id_is_opaque_and_accepts_common_provider_shapes()
    {
        Assert.Equal("Asia/Aden", new TimeZoneId(" Asia/Aden ").Value);
        Assert.Equal("Arab Standard Time", new TimeZoneId("Arab Standard Time").Value);
    }

    [Fact]
    public void Time_zone_id_rejects_control_characters()
    {
        Assert.Throws<ArgumentException>(() => new TimeZoneId("UTC\nInjected"));
    }
}
