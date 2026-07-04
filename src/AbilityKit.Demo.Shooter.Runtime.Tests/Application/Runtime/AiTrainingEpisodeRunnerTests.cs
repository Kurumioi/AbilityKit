using System.Text.Json;
using AbilityKit.AI.Training.Runner;
using AbilityKit.Demo.Shooter.AI;
using Xunit;

namespace AbilityKit.Demo.Shooter.Runtime.Tests.Application.Runtime;

public sealed class AiTrainingEpisodeRunnerTests
{
    [Fact]
    public void Run_WithShooterForwardFirePolicy_ProducesEpisodeSummaries()
    {
        var runner = CreateShooterRunner(enableEnemyWaves: false);

        var summary = runner.Run(new AiTrainingRunOptions(episodes: 3, seed: 10, maxSteps: 4));

        Assert.Equal(3, summary.EpisodeCount);
        Assert.Equal(12, summary.TotalSteps);
        Assert.Equal(4f, summary.AverageSteps);
        Assert.Equal(3, summary.TruncatedEpisodes);
        Assert.All(summary.Episodes, episode => Assert.NotEqual(0u, episode.FinalStateHash));
        Assert.Collection(
            summary.Episodes,
            first => Assert.Equal(10, first.Seed),
            second => Assert.Equal(11, second.Seed),
            third => Assert.Equal(12, third.Seed));
    }

    [Fact]
    public void Run_WithSameSeedsAndPolicy_IsDeterministic()
    {
        var left = CreateShooterRunner(enableEnemyWaves: true).Run(new AiTrainingRunOptions(episodes: 2, seed: 42, maxSteps: 16));
        var right = CreateShooterRunner(enableEnemyWaves: true).Run(new AiTrainingRunOptions(episodes: 2, seed: 42, maxSteps: 16));

        Assert.Equal(left.EpisodeCount, right.EpisodeCount);
        for (int i = 0; i < left.EpisodeCount; i++)
        {
            Assert.Equal(left.Episodes[i].FinalStateHash, right.Episodes[i].FinalStateHash);
            Assert.Equal(left.Episodes[i].Steps, right.Episodes[i].Steps);
            Assert.Equal(left.Episodes[i].TotalReward, right.Episodes[i].TotalReward);
        }
    }

    [Fact]
    public void WriteJsonLines_EmitsRunAndEpisodeRows()
    {
        var runner = CreateShooterRunner(enableEnemyWaves: false);
        var summary = runner.Run(new AiTrainingRunOptions(episodes: 2, seed: 5, maxSteps: 2));
        using var writer = new StringWriter();

        AiTrainingReportWriter.WriteJsonLines(summary, writer);

        var output = writer.ToString();
        var records = AiTrainingJsonLinesReader.Read(new StringReader(output));
        Assert.Equal(3, records.Count);
        Assert.Equal(AiTrainingJsonLineType.Run, records[0].Type);
        Assert.Equal(AiTrainingJsonLineType.Episode, records[1].Type);
        Assert.Equal(AiTrainingJsonLineType.Episode, records[2].Type);
        var lines = output.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(3, lines.Length);
        using var run = JsonDocument.Parse(lines[0]);
        using var episode = JsonDocument.Parse(lines[1]);
        Assert.Equal(AiTrainingDataContract.SchemaVersion, run.RootElement.GetProperty("schemaVersion").GetInt32());
        Assert.Equal("run", run.RootElement.GetProperty("type").GetString());
        Assert.Equal(2, run.RootElement.GetProperty("episodes").GetInt32());
        Assert.Equal(AiTrainingDataContract.SchemaVersion, episode.RootElement.GetProperty("schemaVersion").GetInt32());
        Assert.Equal("episode", episode.RootElement.GetProperty("type").GetString());
        Assert.Equal(5, episode.RootElement.GetProperty("seed").GetInt32());
    }

    [Fact]
    public void Run_WithRolloutWriter_EmitsStepRows()
    {
        var runner = CreateShooterRunner(enableEnemyWaves: false);
        using var writer = new StringWriter();
        using var rolloutWriter = new AiTrainingRolloutJsonLinesWriter(writer, leaveOpen: true);

        var summary = runner.Run(new AiTrainingRunOptions(episodes: 1, seed: 7, maxSteps: 3), rolloutWriter);

        var output = writer.ToString();
        var records = AiTrainingJsonLinesReader.Read(new StringReader(output));
        Assert.Equal(summary.TotalSteps, records.Count);
        Assert.All(records, record => Assert.Equal(AiTrainingJsonLineType.Step, record.Type));
        var lines = output.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(summary.TotalSteps, lines.Length);
        using var first = JsonDocument.Parse(lines[0]);
        var root = first.RootElement;
        Assert.Equal(AiTrainingDataContract.SchemaVersion, root.GetProperty("schemaVersion").GetInt32());
        Assert.Equal("step", root.GetProperty("type").GetString());
        Assert.Equal(0, root.GetProperty("episodeIndex").GetInt32());
        Assert.Equal(7, root.GetProperty("seed").GetInt32());
        Assert.Equal(1, root.GetProperty("stepIndex").GetInt32());
        Assert.True(root.GetProperty("observation").GetArrayLength() > 0);
        Assert.Equal(4, root.GetProperty("continuousAction").GetArrayLength());
        Assert.Equal(2, root.GetProperty("discreteAction").GetArrayLength());
        Assert.True(root.TryGetProperty("reward", out _));
        Assert.True(root.TryGetProperty("done", out _));
        Assert.True(root.TryGetProperty("truncated", out _));
        Assert.True(root.TryGetProperty("stateHash", out _));
    }

    private static AiTrainingEpisodeRunner CreateShooterRunner(bool enableEnemyWaves)
    {
        return new AiTrainingEpisodeRunner(
            () => new ShooterAiTrainingEnvironment(new ShooterAiEnvironmentOptions(
                controlledPlayerId: 1,
                maxObservedEnemies: 2,
                maxObservedProjectiles: 2,
                enableEnemyWaves: enableEnemyWaves)),
            () => new ShooterAiForwardFirePolicy());
    }
}
