using AbilityKit.AI.Training.Runner;
using Xunit;

namespace AbilityKit.Demo.Shooter.Runtime.Tests.Application.Runtime;

public sealed class AiTrainingJsonLinesReaderTests
{
    [Fact]
    public void Read_WithReportAndRolloutRows_ReturnsTypedRecords()
    {
        using var reader = new StringReader(string.Join(Environment.NewLine,
            "{\"schemaVersion\":1,\"type\":\"run\",\"episodes\":1,\"totalSteps\":2,\"totalReward\":1.5,\"averageReward\":1.5,\"averageSteps\":2,\"completedEpisodes\":1,\"truncatedEpisodes\":0,\"seed\":10,\"maxSteps\":8,\"fixedDeltaSeconds\":0.033333335}",
            "{\"schemaVersion\":1,\"type\":\"episode\",\"episodeIndex\":0,\"seed\":10,\"steps\":2,\"totalReward\":1.5,\"done\":true,\"truncated\":false,\"finalStateHash\":1234}",
            "{\"schemaVersion\":1,\"type\":\"step\",\"episodeIndex\":0,\"seed\":10,\"stepIndex\":1,\"observation\":[0,1],\"continuousAction\":[0.5],\"discreteAction\":[1],\"reward\":0.25,\"done\":false,\"truncated\":false,\"stateHash\":5678}"));

        var records = AiTrainingJsonLinesReader.Read(reader);

        Assert.Equal(3, records.Count);
        Assert.Collection(
            records,
            run =>
            {
                Assert.Equal(1, run.LineNumber);
                Assert.Equal(AiTrainingDataContract.SchemaVersion, run.SchemaVersion);
                Assert.Equal(AiTrainingJsonLineType.Run, run.Type);
            },
            episode =>
            {
                Assert.Equal(2, episode.LineNumber);
                Assert.Equal(AiTrainingJsonLineType.Episode, episode.Type);
            },
            step =>
            {
                Assert.Equal(3, step.LineNumber);
                Assert.Equal(AiTrainingJsonLineType.Step, step.Type);
                Assert.Equal(2, step.Payload.GetProperty("observation").GetArrayLength());
            });
    }

    [Fact]
    public void Read_WithMissingSchemaVersion_ThrowsFormatException()
    {
        using var reader = new StringReader("{\"type\":\"step\",\"episodeIndex\":0}");

        var exception = Assert.Throws<FormatException>(() => AiTrainingJsonLinesReader.Read(reader));

        Assert.Contains("line 1", exception.Message, StringComparison.Ordinal);
        Assert.Contains("schemaVersion", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Read_WithUnsupportedSchemaVersion_ThrowsFormatException()
    {
        using var reader = new StringReader("{\"schemaVersion\":99,\"type\":\"run\"}");

        var exception = Assert.Throws<FormatException>(() => AiTrainingJsonLinesReader.Read(reader));

        Assert.Contains("Unsupported schemaVersion 99", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Read_WithUnknownType_ThrowsFormatException()
    {
        using var reader = new StringReader("{\"schemaVersion\":1,\"type\":\"metadata\"}");

        var exception = Assert.Throws<FormatException>(() => AiTrainingJsonLinesReader.Read(reader));

        Assert.Contains("Unsupported row type", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Read_WithInvalidStepShape_ThrowsFormatException()
    {
        using var reader = new StringReader("{\"schemaVersion\":1,\"type\":\"step\",\"episodeIndex\":0,\"seed\":10,\"stepIndex\":1,\"observation\":0,\"continuousAction\":[],\"discreteAction\":[],\"reward\":0,\"done\":false,\"truncated\":false,\"stateHash\":1}");

        var exception = Assert.Throws<FormatException>(() => AiTrainingJsonLinesReader.Read(reader));

        Assert.Contains("observation", exception.Message, StringComparison.Ordinal);
        Assert.Contains("array", exception.Message, StringComparison.Ordinal);
    }
}
