using AbilityKit.Game.View.Flow;
using Xunit;

namespace AbilityKit.Game.View.Runtime.Tests;

public sealed class PhaseStateFeatureValidatorTests
{
    [Fact]
    public void Validate_WithKnownFeatures_ReturnsValidResult()
    {
        var catalog = new PhaseFeatureCatalog()
            .Add("scene")
            .Add("hud")
            .Add("input");
        var specs = new[]
        {
            new PhaseStateFeatureSpec("Boot", clearBeforeEnter: true)
                .AddFeature("scene"),
            new PhaseStateFeatureSpec("Battle")
                .AddFeature("hud")
                .AddFeature("input")
        };

        var result = new PhaseStateFeatureValidator().Validate(specs, catalog);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validate_WithDuplicateStateIds_ReportsError()
    {
        var catalog = new PhaseFeatureCatalog().Add("scene");
        var specs = new[]
        {
            new PhaseStateFeatureSpec("Boot").AddFeature("scene"),
            new PhaseStateFeatureSpec("Boot").AddFeature("scene")
        };

        var result = new PhaseStateFeatureValidator().Validate(specs, catalog);

        Assert.False(result.IsValid);
        Assert.Contains("Phase state id duplicated: Boot", result.Errors);
    }

    [Fact]
    public void Validate_WithDuplicateFeatureIdsInState_ReportsError()
    {
        var catalog = new PhaseFeatureCatalog().Add("hud");
        var specs = new[]
        {
            new PhaseStateFeatureSpec("Battle")
                .AddFeature("hud")
                .AddFeature("hud")
        };

        var result = new PhaseStateFeatureValidator().Validate(specs, catalog);

        Assert.False(result.IsValid);
        Assert.Contains("Phase state 'Battle' references feature id more than once: hud", result.Errors);
    }

    [Fact]
    public void Validate_WithUnknownFeatureIds_ReportsError()
    {
        var catalog = new PhaseFeatureCatalog().Add("hud");
        var specs = new[]
        {
            new PhaseStateFeatureSpec("Battle")
                .AddFeature("hud")
                .AddFeature("scene")
        };

        var result = new PhaseStateFeatureValidator().Validate(specs, catalog);

        Assert.False(result.IsValid);
        Assert.Contains("Phase state 'Battle' references unknown feature id: scene", result.Errors);
    }

    [Fact]
    public void Validate_WithKnownActionRefs_ReturnsValidResult()
    {
        var featureCatalog = new PhaseFeatureCatalog().Add("hud");
        var actionCatalog = new PhaseActionCatalog()
            .Add("battle.reset")
            .Add("battle.return_lobby")
            .Add("battle.detach_session");
        var specs = new[]
        {
            new PhaseStateFeatureSpec("Battle")
                .AddFeature("hud")
                .AddEnterBeforeAction("battle.reset")
                .AddEnterAfterAction("battle.return_lobby")
                .AddExitAction("battle.detach_session")
        };

        var result = new PhaseStateFeatureValidator().Validate(specs, featureCatalog, actionCatalog);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validate_WithDuplicateOrUnknownActionRefs_ReportsErrors()
    {
        var featureCatalog = new PhaseFeatureCatalog().Add("hud");
        var actionCatalog = new PhaseActionCatalog()
            .Add("battle.reset")
            .Add("battle.return_lobby");
        var specs = new[]
        {
            new PhaseStateFeatureSpec("Battle")
                .AddFeature("hud")
                .AddEnterBeforeAction("battle.reset")
                .AddEnterBeforeAction("battle.reset")
                .AddEnterAfterAction("battle.missing")
                .AddExitAction("battle.detach_session")
        };

        var result = new PhaseStateFeatureValidator().Validate(specs, featureCatalog, actionCatalog);

        Assert.False(result.IsValid);
        Assert.Contains("Phase state 'Battle' references enter before action id more than once: battle.reset", result.Errors);
        Assert.Contains("Phase state 'Battle' references unknown enter after action id: battle.missing", result.Errors);
        Assert.Contains("Phase state 'Battle' references unknown exit action id: battle.detach_session", result.Errors);
    }

    [Fact]
    public void Validate_WithKnownSwitchFlowRefs_ReturnsValidResult()
    {
        var featureCatalog = new PhaseFeatureCatalog().Add("hud");
        var switchFlowCatalog = new PhaseSwitchFlowCatalog()
            .Add("battle.advance_connect")
            .Add("battle.advance_load");
        var specs = new[]
        {
            new PhaseStateFeatureSpec("Battle")
                .AddFeature("hud")
                .AddSwitchFlow("battle.advance_connect")
                .AddSwitchFlow("battle.advance_load")
        };

        var result = new PhaseStateFeatureValidator().Validate(specs, featureCatalog, actionCatalog: null, switchFlowCatalog);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validate_WithDuplicateOrUnknownSwitchFlowRefs_ReportsErrors()
    {
        var featureCatalog = new PhaseFeatureCatalog().Add("hud");
        var switchFlowCatalog = new PhaseSwitchFlowCatalog()
            .Add("battle.advance_connect");
        var specs = new[]
        {
            new PhaseStateFeatureSpec("Battle")
                .AddFeature("hud")
                .AddSwitchFlow("battle.advance_connect")
                .AddSwitchFlow("battle.advance_connect")
                .AddSwitchFlow("battle.missing")
        };

        var result = new PhaseStateFeatureValidator().Validate(specs, featureCatalog, actionCatalog: null, switchFlowCatalog);

        Assert.False(result.IsValid);
        Assert.Contains("Phase state 'Battle' references switch flow id more than once: battle.advance_connect", result.Errors);
        Assert.Contains("Phase state 'Battle' references unknown switch flow id: battle.missing", result.Errors);
    }

    [Fact]
    public void SwitchFlowCatalog_Add_WithDuplicateId_KeepsSingleRegistration()
    {
        var catalog = new PhaseSwitchFlowCatalog()
            .Add("battle.advance_connect")
            .Add("battle.advance_connect");

        Assert.Equal(1, catalog.Count);
        Assert.True(catalog.Contains("battle.advance_connect"));
    }

    [Fact]
    public void Validate_WithNullSpec_ReportsErrorAndContinues()
    {
        var catalog = new PhaseFeatureCatalog().Add("hud");
        PhaseStateFeatureSpec?[] specs =
        {
            null,
            new PhaseStateFeatureSpec("Battle").AddFeature("hud")
        };
 
        var result = new PhaseStateFeatureValidator().Validate(specs!, catalog);
 
        Assert.False(result.IsValid);
        Assert.Contains("Phase state spec at index 0 is null.", result.Errors);
    }
 
    [Fact]
    public void Catalog_Add_WithDuplicateFeatureId_KeepsSingleRegistration()
    {
        var catalog = new PhaseFeatureCatalog()
            .Add("hud")
            .Add("hud");

        Assert.Equal(1, catalog.Count);
        Assert.True(catalog.Contains("hud"));
    }

    [Fact]
    public void ActionCatalog_Add_WithDuplicateActionId_KeepsSingleRegistration()
    {
        var catalog = new PhaseActionCatalog()
            .Add("battle.reset")
            .Add("battle.reset");

        Assert.Equal(1, catalog.Count);
        Assert.True(catalog.Contains("battle.reset"));
    }
}
