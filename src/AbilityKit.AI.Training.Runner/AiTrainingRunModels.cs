using AbilityKit.AI.Abstractions;

namespace AbilityKit.AI.Training.Runner;

public readonly struct AiTrainingRunOptions
{
    public AiTrainingRunOptions(int episodes, int seed, int maxSteps, float fixedDeltaSeconds = 1f / 30f)
    {
        Episodes = episodes < 1 ? 1 : episodes;
        Seed = seed;
        MaxSteps = maxSteps < 1 ? 1 : maxSteps;
        FixedDeltaSeconds = fixedDeltaSeconds > 0f ? fixedDeltaSeconds : 1f / 30f;
    }

    public int Episodes { get; }

    public int Seed { get; }

    public int MaxSteps { get; }

    public float FixedDeltaSeconds { get; }
}

public readonly struct AiTrainingEpisodeSummary
{
    public AiTrainingEpisodeSummary(int episodeIndex, int seed, int steps, float totalReward, bool done, bool truncated, uint finalStateHash)
    {
        EpisodeIndex = episodeIndex < 0 ? 0 : episodeIndex;
        Seed = seed;
        Steps = steps < 0 ? 0 : steps;
        TotalReward = totalReward;
        Done = done;
        Truncated = truncated;
        FinalStateHash = finalStateHash;
    }

    public int EpisodeIndex { get; }

    public int Seed { get; }

    public int Steps { get; }

    public float TotalReward { get; }

    public bool Done { get; }

    public bool Truncated { get; }

    public uint FinalStateHash { get; }
}

public sealed class AiTrainingRunSummary
{
    private readonly AiTrainingEpisodeSummary[] _episodes;

    public AiTrainingRunSummary(AiTrainingRunOptions options, AiTrainingEpisodeSummary[] episodes)
    {
        Options = options;
        _episodes = episodes ?? Array.Empty<AiTrainingEpisodeSummary>();
        EpisodeCount = _episodes.Length;
        TotalSteps = 0;
        TotalReward = 0f;
        CompletedEpisodes = 0;
        TruncatedEpisodes = 0;

        for (int i = 0; i < _episodes.Length; i++)
        {
            var episode = _episodes[i];
            TotalSteps += episode.Steps;
            TotalReward += episode.TotalReward;
            if (episode.Done) CompletedEpisodes++;
            if (episode.Truncated) TruncatedEpisodes++;
        }
    }

    public AiTrainingRunOptions Options { get; }

    public IReadOnlyList<AiTrainingEpisodeSummary> Episodes => _episodes;

    public int EpisodeCount { get; }

    public int TotalSteps { get; }

    public float TotalReward { get; }

    public int CompletedEpisodes { get; }

    public int TruncatedEpisodes { get; }

    public float AverageReward => EpisodeCount == 0 ? 0f : TotalReward / EpisodeCount;

    public float AverageSteps => EpisodeCount == 0 ? 0f : (float)TotalSteps / EpisodeCount;
}
