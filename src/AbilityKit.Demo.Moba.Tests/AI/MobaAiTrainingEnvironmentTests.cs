using AbilityKit.AI.Abstractions;
using AbilityKit.AI.Inference;
using AbilityKit.AI.Training.Runner;
using AbilityKit.Demo.Moba.AI;
using Xunit;

namespace AbilityKit.Demo.Moba.Tests.AI;

public sealed class MobaAiTrainingEnvironmentTests
{
    [Fact]
    public void Reset_StartsRuntimeAndBuildsObservation()
    {
        using var environment = CreateEnvironment();

        var reset = environment.Reset(new AiEpisodeOptions(seed: 1234, maxSteps: 4));

        Assert.False(reset.Done);
        Assert.False(reset.Truncated);
        Assert.Equal(0, reset.StepIndex);
        Assert.NotEqual(0u, reset.StateHash);
        Assert.Equal(environment.ObservationSpec.Length, reset.Observation.Length);
        Assert.NotNull(environment.Bootstrapper);
        Assert.NotNull(environment.Runtime);
        Assert.True(environment.Bootstrapper.RuntimeInputPortReady);
        Assert.Equal("InMatch", environment.Bootstrapper.Flow.CurrentPhase);
        Assert.Contains(reset.Observation.Values, value => Math.Abs(value) > 0.0001f);
    }

    [Fact]
    public void ActionMapper_MapsContinuousMoveAndDiscreteSkillToConsoleContext()
    {
        using var environment = CreateEnvironment();
        environment.Reset(new AiEpisodeOptions(seed: 1234, maxSteps: 4));
        var action = new AiActionBuffer(environment.ActionSpec);
        action.Continuous[0] = 2f;
        action.Continuous[1] = -2f;
        action.Discrete[0] = 9;

        new MobaAiActionMapper().Apply(action, environment.Bootstrapper!);

        var context = environment.Bootstrapper!.Context;
        Assert.True(context.HudHasMove);
        Assert.Equal(1f, context.HudMoveDx);
        Assert.Equal(-1f, context.HudMoveDz);
        Assert.Equal(3, context.HudSkillClickSlot);
    }

    [Fact]
    public void Step_AdvancesFrameAndReportsTruncationAtEpisodeLimit()
    {
        using var environment = CreateEnvironment();
        var reset = environment.Reset(new AiEpisodeOptions(seed: 1234, maxSteps: 1));
        var action = new AiActionBuffer(environment.ActionSpec);
        var observation = reset.Observation;
        new MobaAiForwardSkillPolicy().Decide(in observation, action);

        var step = environment.Step(action);

        Assert.Equal(1, step.StepIndex);
        Assert.True(step.Truncated || step.Done);
        Assert.NotEqual(0u, step.StateHash);
        Assert.NotEqual(reset.StateHash, step.StateHash);
        Assert.True(environment.Bootstrapper!.Context.LastFrame > 0);
    }

    [Fact]
    public void Episode_WithSameSeedAndPolicy_IsDeterministic()
    {
        var first = RunEpisode(seed: 2024, maxSteps: 2);
        var second = RunEpisode(seed: 2024, maxSteps: 2);

        Assert.Equal(first.Result.StepIndex, second.Result.StepIndex);
        Assert.Equal(first.Result.Done, second.Result.Done);
        Assert.Equal(first.Result.Truncated, second.Result.Truncated);
        Assert.True(
            first.Result.StateHash == second.Result.StateHash,
            $"Expected hash {first.Result.StateHash}, actual hash {second.Result.StateHash}. First: {first.Signature}. Second: {second.Signature}");
    }

    [Fact]
    public void TrainingRunner_CanRunMobaEnvironmentEpisodes()
    {
        var runner = new AiTrainingEpisodeRunner(CreateEnvironment, () => new MobaAiForwardSkillPolicy());

        var summary = runner.Run(new AiTrainingRunOptions(episodes: 2, seed: 3000, maxSteps: 2));

        Assert.Equal(2, summary.EpisodeCount);
        Assert.Equal(4, summary.TotalSteps);
        Assert.Equal(2, summary.TruncatedEpisodes + summary.CompletedEpisodes);
        Assert.All(summary.Episodes, episode => Assert.NotEqual(0u, episode.FinalStateHash));
    }

    [Fact]
    public void ModelPolicy_CanDriveMobaEnvironmentThroughSharedInferenceBoundary()
    {
        using var environment = CreateEnvironment();
        var reset = environment.Reset(new AiEpisodeOptions(seed: 1234, maxSteps: 2));
        var spec = AiModelPolicySpec.FromEnvironment(environment);
        var executor = new DelegateAiModelExecutor(
            spec,
            _ => new AiModelOutput(
                new[] { 0.5f, 0f },
                new[] { 1 }));
        var policy = new AiModelPolicy(executor);
        var action = new AiActionBuffer(environment.ActionSpec);
        var observation = reset.Observation;

        policy.Decide(in observation, action);
        var step = environment.Step(action);

        Assert.Equal(0.5f, action.Continuous[0]);
        Assert.Equal(0f, action.Continuous[1]);
        Assert.Equal(1, action.Discrete[0]);
        Assert.Equal(1, step.StepIndex);
        Assert.NotEqual(0u, step.StateHash);
    }

    private static EpisodeSnapshot RunEpisode(int seed, int maxSteps)
    {
        using var environment = CreateEnvironment();
        var policy = new MobaAiForwardSkillPolicy();
        var step = environment.Reset(new AiEpisodeOptions(seed, maxSteps));
        var action = new AiActionBuffer(environment.ActionSpec);
        while (!step.Done && !step.Truncated)
        {
            action.Clear();
            var observation = step.Observation;
            policy.Decide(in observation, action);
            step = environment.Step(action);
        }

        return new EpisodeSnapshot(step, CreateStateSignature(environment));
    }

    private static string CreateStateSignature(MobaAiTrainingEnvironment environment)
    {
        var frame = environment.Bootstrapper?.Context.LastFrame ?? -1;
        var parts = new List<string>
        {
            $"frame={frame}",
            $"local={environment.LocalActorId}",
        };

        foreach (var state in environment.CurrentEntityStates)
        {
            parts.Add($"id={state.EntityId},team={state.TeamId},x={Quantize(state.X)},y={Quantize(state.Y)},z={Quantize(state.Z)},hp={Quantize(state.Hp)},max={Quantize(state.HpMax)},dead={state.IsDead},loadout={state.HasSkillLoadout},skills={state.ActiveSkillCount}");
        }

        return string.Join(" | ", parts);
    }

    private static int Quantize(float value) => (int)MathF.Round(value * 1000f);

    private readonly record struct EpisodeSnapshot(AiStepResult Result, string Signature);

    private static MobaAiTrainingEnvironment CreateEnvironment()
    {
        return new MobaAiTrainingEnvironment(new MobaAiEnvironmentOptions(maxObservedEntities: 6));
    }
}
