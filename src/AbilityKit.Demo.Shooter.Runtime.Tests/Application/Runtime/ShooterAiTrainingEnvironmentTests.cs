using AbilityKit.AI.Abstractions;
using AbilityKit.Demo.Shooter.AI;
using AbilityKit.Protocol.Shooter;
using Xunit;

namespace AbilityKit.Demo.Shooter.Runtime.Tests.Application.Runtime;

public sealed class ShooterAiTrainingEnvironmentTests
{
    [Fact]
    public void Reset_StartsRuntimeAndBuildsStableObservation()
    {
        var environment = new ShooterAiTrainingEnvironment(new ShooterAiEnvironmentOptions(controlledPlayerId: 1, maxObservedEnemies: 2, maxObservedProjectiles: 2));

        var result = environment.Reset(new AiEpisodeOptions(seed: 123, maxSteps: 8));

        Assert.False(result.Done);
        Assert.False(result.Truncated);
        Assert.Equal(8 + 2 * 5 + 2 * 5 + 4, result.Observation.Length);
        Assert.True(environment.Runtime.IsStarted);
        Assert.Equal(0, environment.Runtime.CurrentFrame);
        Assert.NotEqual(0u, result.StateHash);
    }

    [Fact]
    public void ActionMapper_MapsContinuousAndDiscreteBranchesToShooterCommand()
    {
        var mapper = new ShooterAiActionMapper(playerId: 7);
        var action = new AiActionBuffer(ShooterAiActionMapper.ActionSpec);
        action.Continuous[0] = 2f;
        action.Continuous[1] = -2f;
        action.Continuous[2] = 0f;
        action.Continuous[3] = -1f;
        action.Discrete[0] = 1;
        action.Discrete[1] = ShooterPlayerAttackSlots.Spread;

        var command = mapper.ToCommand(action);

        Assert.Equal(7, command.PlayerId);
        Assert.Equal(1f, command.MoveX);
        Assert.Equal(-1f, command.MoveY);
        Assert.Equal(0f, command.AimX);
        Assert.Equal(-1f, command.AimY);
        Assert.True(command.Fire);
        Assert.Equal(ShooterPlayerAttackSlots.Spread, command.AttackSlot);
    }

    [Fact]
    public void Step_AdvancesFrameAndReportsTruncationAtEpisodeLimit()
    {
        var environment = new ShooterAiTrainingEnvironment(new ShooterAiEnvironmentOptions(controlledPlayerId: 1, maxObservedEnemies: 1, maxObservedProjectiles: 1, enableEnemyWaves: false));
        environment.Reset(new AiEpisodeOptions(seed: 7, maxSteps: 1));
        var action = new AiActionBuffer(environment.ActionSpec);
        action.Continuous[2] = 1f;
        action.Discrete[0] = 1;

        var result = environment.Step(action);

        Assert.Equal(1, result.StepIndex);
        Assert.Equal(1, environment.Runtime.CurrentFrame);
        Assert.True(result.Truncated || result.Done);
        Assert.NotEqual(0u, result.StateHash);
    }

    [Fact]
    public void Episode_WithSameSeedAndPolicy_IsDeterministic()
    {
        var leftHash = RunScriptedEpisode(seed: 99);
        var rightHash = RunScriptedEpisode(seed: 99);

        Assert.Equal(leftHash, rightHash);
    }

    private static uint RunScriptedEpisode(int seed)
    {
        var environment = new ShooterAiTrainingEnvironment(new ShooterAiEnvironmentOptions(controlledPlayerId: 1, maxObservedEnemies: 2, maxObservedProjectiles: 2));
        var policy = new ShooterAiForwardFirePolicy();
        var action = new AiActionBuffer(environment.ActionSpec);
        var step = environment.Reset(new AiEpisodeOptions(seed, maxSteps: 8));

        for (var i = 0; i < 8 && !step.Done; i++)
        {
            var observation = step.Observation;
            policy.Decide(in observation, action);
            step = environment.Step(action);
        }

        return step.StateHash;
    }
}
