using FoundationKit.FeatureManagement;
using FoundationKit.Settings;
using Xunit;

namespace FoundationKit.Tests;

public sealed class FeatureManagementCapabilityTests
{
    [Fact]
    public async Task Feature_uses_explicit_scoped_setting_when_present()
    {
        var userScope = new SettingScope("user", "user-42");
        var feature = new FeatureDefinition("catalog.preview", defaultEnabled: false);
        var source = new InMemorySettingSource(
        [
            new SettingEntry(
                SettingScope.Global,
                SettingBackedFeatureEvaluator.GetEnabledSettingKey(feature.Id),
                "false"),
            new SettingEntry(
                userScope,
                SettingBackedFeatureEvaluator.GetEnabledSettingKey(feature.Id),
                "true")
        ]);
        var evaluator = new SettingBackedFeatureEvaluator(new SettingReader(source));

        var decision = await evaluator.EvaluateAsync(
            feature,
            new FeatureEvaluationContext(
                new SettingResolutionContext([userScope])));

        Assert.True(decision.IsEnabled);
        Assert.Equal(FeatureDecisionSource.Setting, decision.Source);
        Assert.Equal(userScope, decision.MatchedScope);
    }

    [Fact]
    public async Task Feature_falls_back_to_definition_default_when_setting_is_missing()
    {
        var evaluator = new SettingBackedFeatureEvaluator(
            new SettingReader(new InMemorySettingSource([])));

        var decision = await evaluator.EvaluateAsync(
            new FeatureDefinition("safe.default", defaultEnabled: true),
            FeatureEvaluationContext.Global);

        Assert.True(decision.IsEnabled);
        Assert.Equal(FeatureDecisionSource.Default, decision.Source);
        Assert.Null(decision.MatchedScope);
    }

    [Fact]
    public async Task Invalid_feature_setting_fails_closed_even_when_default_is_enabled()
    {
        var feature = new FeatureDefinition("payments.experimental", defaultEnabled: true);
        var source = new InMemorySettingSource(
        [
            new SettingEntry(
                SettingScope.Global,
                SettingBackedFeatureEvaluator.GetEnabledSettingKey(feature.Id),
                "not-a-boolean")
        ]);
        var evaluator = new SettingBackedFeatureEvaluator(new SettingReader(source));

        var decision = await evaluator.EvaluateAsync(
            feature,
            FeatureEvaluationContext.Global);

        Assert.False(decision.IsEnabled);
        Assert.Equal(FeatureDecisionSource.InvalidSetting, decision.Source);
        Assert.Equal(SettingScope.Global, decision.MatchedScope);
    }

    [Theory]
    [InlineData("feature with spaces")]
    [InlineData("feature/with/slashes")]
    [InlineData("@feature")]
    public void Feature_id_rejects_unsafe_shapes(string featureId)
    {
        Assert.Throws<ArgumentException>(() => FeatureId.Normalize(featureId));
    }

    [Fact]
    public void Decision_diagnostics_do_not_include_setting_values()
    {
        var decision = new FeatureDecision(
            "catalog.preview",
            true,
            FeatureDecisionSource.Setting,
            SettingScope.Global);

        Assert.DoesNotContain("true", decision.ToString(), StringComparison.OrdinalIgnoreCase);
    }
}
