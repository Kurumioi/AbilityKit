using System.Text.Json;
using System.Text.Json.Serialization;

namespace AbilityKit.AI.Training.Runner;

public static class AiTrainingReportWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
    {
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never
    };

    public static void WriteJsonLines(AiTrainingRunSummary summary, TextWriter writer)
    {
        if (summary == null) throw new ArgumentNullException(nameof(summary));
        if (writer == null) throw new ArgumentNullException(nameof(writer));

        writer.WriteLine(JsonSerializer.Serialize(new
        {
            schemaVersion = AiTrainingDataContract.SchemaVersion,
            type = "run",
            episodes = summary.EpisodeCount,
            totalSteps = summary.TotalSteps,
            totalReward = summary.TotalReward,
            averageReward = summary.AverageReward,
            averageSteps = summary.AverageSteps,
            completedEpisodes = summary.CompletedEpisodes,
            truncatedEpisodes = summary.TruncatedEpisodes,
            seed = summary.Options.Seed,
            maxSteps = summary.Options.MaxSteps,
            fixedDeltaSeconds = summary.Options.FixedDeltaSeconds
        }, JsonOptions));

        foreach (var episode in summary.Episodes)
        {
            writer.WriteLine(JsonSerializer.Serialize(new
            {
                schemaVersion = AiTrainingDataContract.SchemaVersion,
                type = "episode",
                episodeIndex = episode.EpisodeIndex,
                seed = episode.Seed,
                steps = episode.Steps,
                totalReward = episode.TotalReward,
                done = episode.Done,
                truncated = episode.Truncated,
                finalStateHash = episode.FinalStateHash
            }, JsonOptions));
        }
    }
}
