using AbilityKit.Demo.Moba.Testing;
using Xunit;

namespace AbilityKit.Demo.Moba.Tests.Smoke;

public sealed class BattleTestScenarioLibraryTests
{
    [Fact]
    public void Shared_full_battle_script_keeps_console_and_view_runtime_scenario_asset_stable()
    {
        var script = BattleTestScenarioLibrary.CreateFullBattle(randomSeed: 1337);

        Assert.Equal(BattleTestScenarioLibrary.FullBattleName, script.Name);
        Assert.NotEmpty(script.Steps);
        Assert.Equal(48, script.Steps.Count);
        Assert.True(script.TotalDurationTicks > 0);

        Assert.Equal(BattleTestStepKind.Move, script.Steps[0].Kind);
        Assert.Equal(1f, script.Steps[0].Dx);
        Assert.Equal(0f, script.Steps[0].Dz);
        Assert.Equal(10, script.Steps[0].DurationTicks);

        Assert.Contains(script.Steps, step => step.Kind == BattleTestStepKind.Skill && step.Slot == 1);
        Assert.Contains(script.Steps, step => step.Kind == BattleTestStepKind.Skill && step.Slot == 2);
        Assert.Contains(script.Steps, step => step.Kind == BattleTestStepKind.Skill && step.Slot == 3);
        Assert.Equal(BattleTestStepKind.Wait, script.Steps[^1].Kind);
    }

    [Fact]
    public void Shared_random_and_stress_scripts_are_seeded_for_repeatable_headless_tests()
    {
        var randomA = BattleTestScenarioLibrary.CreateRandomMovement(steps: 4, seed: 7);
        var randomB = BattleTestScenarioLibrary.CreateRandomMovement(steps: 4, seed: 7);
        var stressA = BattleTestScenarioLibrary.CreateStressTest(durationTicks: 24, seed: 9);
        var stressB = BattleTestScenarioLibrary.CreateStressTest(durationTicks: 24, seed: 9);

        AssertSameScript(randomA, randomB);
        AssertSameScript(stressA, stressB);
    }

    [Fact]
    public void Shared_skill_cast_script_uses_platform_neutral_step_model()
    {
        var script = BattleTestScenarioLibrary.CreateSkillCast(skillSlot: 2, repeats: 2);

        Assert.Equal(BattleTestScenarioLibrary.SkillCastName, script.Name);
        Assert.Collection(
            script.Steps,
            step =>
            {
                Assert.Equal(BattleTestStepKind.Skill, step.Kind);
                Assert.Equal(2, step.Slot);
                Assert.Equal(1, step.DurationTicks);
            },
            step =>
            {
                Assert.Equal(BattleTestStepKind.Wait, step.Kind);
                Assert.Equal(30, step.DurationTicks);
            },
            step =>
            {
                Assert.Equal(BattleTestStepKind.Skill, step.Kind);
                Assert.Equal(2, step.Slot);
                Assert.Equal(1, step.DurationTicks);
            },
            step =>
            {
                Assert.Equal(BattleTestStepKind.Wait, step.Kind);
                Assert.Equal(30, step.DurationTicks);
            });
    }

    private static void AssertSameScript(BattleTestScript expected, BattleTestScript actual)
    {
        Assert.Equal(expected.Name, actual.Name);
        Assert.Equal(expected.Steps.Count, actual.Steps.Count);
        Assert.Equal(expected.TotalDurationTicks, actual.TotalDurationTicks);

        for (var i = 0; i < expected.Steps.Count; i++)
        {
            Assert.Equal(expected.Steps[i].Kind, actual.Steps[i].Kind);
            Assert.Equal(expected.Steps[i].DurationTicks, actual.Steps[i].DurationTicks);
            Assert.Equal(expected.Steps[i].Dx, actual.Steps[i].Dx);
            Assert.Equal(expected.Steps[i].Dz, actual.Steps[i].Dz);
            Assert.Equal(expected.Steps[i].Slot, actual.Steps[i].Slot);
        }
    }
}
