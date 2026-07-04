using AbilityKit.AI.Abstractions;

namespace AbilityKit.AI.Training.Runner;

public sealed class AiTrainingEpisodeRunner
{
    private readonly Func<IAiEnvironment> _environmentFactory;
    private readonly Func<IAiPolicy> _policyFactory;

    public AiTrainingEpisodeRunner(Func<IAiEnvironment> environmentFactory, Func<IAiPolicy> policyFactory)
    {
        _environmentFactory = environmentFactory ?? throw new ArgumentNullException(nameof(environmentFactory));
        _policyFactory = policyFactory ?? throw new ArgumentNullException(nameof(policyFactory));
    }

    public AiTrainingRunSummary Run(in AiTrainingRunOptions options, IAiTrainingRolloutSink? rolloutSink = null)
    {
        var episodes = new AiTrainingEpisodeSummary[options.Episodes];
        for (int i = 0; i < episodes.Length; i++)
        {
            var episodeSeed = unchecked(options.Seed + i);
            episodes[i] = RunEpisode(i, episodeSeed, in options, rolloutSink);
        }

        return new AiTrainingRunSummary(options, episodes);
    }

    private AiTrainingEpisodeSummary RunEpisode(int episodeIndex, int seed, in AiTrainingRunOptions options, IAiTrainingRolloutSink? rolloutSink)
    {
        var environment = _environmentFactory();
        try
        {
            var policy = _policyFactory();
            if (policy.ActionSpec.ContinuousLength != environment.ActionSpec.ContinuousLength ||
                policy.ActionSpec.DiscreteLength != environment.ActionSpec.DiscreteLength)
            {
                throw new InvalidOperationException("AI policy action spec does not match environment action spec.");
            }

            var action = new AiActionBuffer(environment.ActionSpec);
            var step = environment.Reset(new AiEpisodeOptions(seed, options.MaxSteps, options.FixedDeltaSeconds));
            var totalReward = step.Reward;

            while (!step.Done && !step.Truncated)
            {
                action.Clear();
                var observation = step.Observation;
                policy.Decide(in observation, action);
                step = environment.Step(action);
                totalReward += step.Reward;
                var rolloutStep = new AiTrainingRolloutStep(episodeIndex, seed, step, action);
                rolloutSink?.WriteStep(in rolloutStep);
            }

            return new AiTrainingEpisodeSummary(
                episodeIndex,
                seed,
                step.StepIndex,
                totalReward,
                step.Done,
                step.Truncated,
                step.StateHash);
        }
        finally
        {
            (environment as IDisposable)?.Dispose();
        }
    }
}
