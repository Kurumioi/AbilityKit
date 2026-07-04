using System.Text.Json;
using AbilityKit.AI.Training.Runner;
using AbilityKit.Demo.Moba.AI;
using Xunit;

namespace AbilityKit.Demo.Moba.Tests.AI;

public sealed class MobaAiTrainingRunnerOutputTests
{
    [Fact]
    public void WriteJsonLines_WithMobaSummary_EmitsRunAndEpisodeRows()
    {
        var runner = new AiTrainingEpisodeRunner(CreateEnvironment, () => new MobaAiForwardSkillPolicy());
        var summary = runner.Run(new AiTrainingRunOptions(episodes: 1, seed: 4100, maxSteps: 2));
        using var writer = new StringWriter();

        AiTrainingReportWriter.WriteJsonLines(summary, writer);

        var output = writer.ToString();
        var records = AiTrainingJsonLinesReader.Read(new StringReader(output));
        Assert.Equal(2, records.Count);
        Assert.Equal(AiTrainingJsonLineType.Run, records[0].Type);
        Assert.Equal(AiTrainingJsonLineType.Episode, records[1].Type);
        var lines = SplitLines(output);
        Assert.Equal(2, lines.Length);
        using var runRow = JsonDocument.Parse(lines[0]);
        using var episodeRow = JsonDocument.Parse(lines[1]);
        Assert.Equal(AiTrainingDataContract.SchemaVersion, runRow.RootElement.GetProperty("schemaVersion").GetInt32());
        Assert.Equal("run", runRow.RootElement.GetProperty("type").GetString());
        Assert.Equal(1, runRow.RootElement.GetProperty("episodes").GetInt32());
        Assert.Equal(2, runRow.RootElement.GetProperty("totalSteps").GetInt32());
        Assert.Equal(AiTrainingDataContract.SchemaVersion, episodeRow.RootElement.GetProperty("schemaVersion").GetInt32());
        Assert.Equal("episode", episodeRow.RootElement.GetProperty("type").GetString());
        Assert.Equal(4100, episodeRow.RootElement.GetProperty("seed").GetInt32());
        Assert.NotEqual(0u, episodeRow.RootElement.GetProperty("finalStateHash").GetUInt32());
    }

    [Fact]
    public void Run_WithMobaRolloutWriter_EmitsStepRows()
    {
        var runner = new AiTrainingEpisodeRunner(CreateEnvironment, () => new MobaAiForwardSkillPolicy());
        using var textWriter = new StringWriter();
        using var rolloutWriter = new AiTrainingRolloutJsonLinesWriter(textWriter, leaveOpen: true);

        var summary = runner.Run(new AiTrainingRunOptions(episodes: 1, seed: 4200, maxSteps: 2), rolloutWriter);

        var output = textWriter.ToString();
        var records = AiTrainingJsonLinesReader.Read(new StringReader(output));
        Assert.Equal(summary.TotalSteps, records.Count);
        Assert.All(records, record => Assert.Equal(AiTrainingJsonLineType.Step, record.Type));
        var lines = SplitLines(output);
        Assert.Equal(summary.TotalSteps, lines.Length);
        Assert.Equal(2, lines.Length);
        using var firstRow = JsonDocument.Parse(lines[0]);
        var root = firstRow.RootElement;
        Assert.Equal(AiTrainingDataContract.SchemaVersion, root.GetProperty("schemaVersion").GetInt32());
        Assert.Equal("step", root.GetProperty("type").GetString());
        Assert.Equal(0, root.GetProperty("episodeIndex").GetInt32());
        Assert.Equal(4200, root.GetProperty("seed").GetInt32());
        Assert.True(root.GetProperty("stepIndex").GetInt32() > 0);
        Assert.True(root.GetProperty("observation").GetArrayLength() > 0);
        Assert.Equal(2, root.GetProperty("continuousAction").GetArrayLength());
        Assert.Equal(1, root.GetProperty("discreteAction").GetArrayLength());
        Assert.NotEqual(0u, root.GetProperty("stateHash").GetUInt32());
    }

    private static string[] SplitLines(string value)
    {
        return value.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
    }

    private static MobaAiTrainingEnvironment CreateEnvironment()
    {
        return new MobaAiTrainingEnvironment(new MobaAiEnvironmentOptions(maxObservedEntities: 6));
    }
}
