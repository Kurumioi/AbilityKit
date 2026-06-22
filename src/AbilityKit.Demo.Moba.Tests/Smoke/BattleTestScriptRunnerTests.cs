using System.Collections.Generic;
using AbilityKit.Demo.Moba.Testing;
using Xunit;

namespace AbilityKit.Demo.Moba.Tests.Smoke;

public sealed class BattleTestScriptRunnerTests
{
    [Fact]
    public void Runner_executes_each_step_for_its_duration_and_reports_completion()
    {
        var script = new BattleTestScript(
            "runner-smoke",
            new[]
            {
                BattleTestStep.Move(1f, 0f, 2),
                BattleTestStep.Skill(2, 1),
                BattleTestStep.Wait(3),
                BattleTestStep.Idle(1)
            });

        var driver = new RecordingDriver();
        var runner = new BattleTestScriptRunner();

        var result = runner.Run(script, driver);

        Assert.True(result.Completed);
        Assert.Equal(script.Name, result.ScriptName);
        Assert.Equal(script.Steps.Count, result.StepCount);
        Assert.Equal(7, result.TickCount);
        Assert.Equal(7, driver.AppliedSteps.Count);
        Assert.Equal(7, driver.TickCount);
        Assert.Same(script.Steps[0], driver.AppliedSteps[0]);
        Assert.Same(script.Steps[0], driver.AppliedSteps[1]);
        Assert.Same(script.Steps[1], driver.AppliedSteps[2]);
        Assert.Same(script.Steps[2], driver.AppliedSteps[3]);
        Assert.Same(script.Steps[2], driver.AppliedSteps[4]);
        Assert.Same(script.Steps[2], driver.AppliedSteps[5]);
        Assert.Same(script.Steps[3], driver.AppliedSteps[6]);
    }

    [Fact]
    public void Runner_returns_failure_result_when_driver_throws()
    {
        var script = new BattleTestScript("runner-failure", new[] { BattleTestStep.Idle(1) });
        var runner = new BattleTestScriptRunner();
        var driver = new ThrowingDriver();

        var result = runner.Run(script, driver);

        Assert.False(result.Completed);
        Assert.Equal(script.Name, result.ScriptName);
        Assert.Equal(0, result.TickCount);
        Assert.Contains("boom", result.ErrorMessage);
    }

    [Fact]
    public void Shared_scenario_contract_produces_reusable_scripts()
    {
        IBattleTestScenario scenario = new FullBattleScenario { RandomSeed = 1337 };

        var script = scenario.CreateScript();

        Assert.Equal(BattleTestScenarioLibrary.FullBattleName, script.Name);
        Assert.NotEmpty(script.Steps);
    }

    private sealed class RecordingDriver : IBattleTestScriptDriver
    {
        public List<BattleTestStep> AppliedSteps { get; } = new();
        public int TickCount { get; private set; }

        public void Apply(BattleTestStep step)
        {
            AppliedSteps.Add(step);
        }

        public void Tick()
        {
            TickCount++;
        }
    }

    private sealed class ThrowingDriver : IBattleTestScriptDriver
    {
        public void Apply(BattleTestStep step)
        {
            throw new System.InvalidOperationException("boom");
        }

        public void Tick()
        {
        }
    }
}
