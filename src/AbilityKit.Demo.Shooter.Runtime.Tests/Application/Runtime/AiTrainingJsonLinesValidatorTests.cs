using AbilityKit.AI.Training.Runner;
using Xunit;

namespace AbilityKit.Demo.Shooter.Runtime.Tests.Application.Runtime;

public sealed class AiTrainingJsonLinesValidatorTests
{
    [Fact]
    public void Validate_WithMixedRows_ReturnsRecordCounts()
    {
        using var reader = new StringReader(string.Join(Environment.NewLine,
            "{\"schemaVersion\":1,\"type\":\"run\",\"episodes\":1,\"totalSteps\":2,\"totalReward\":1.5,\"averageReward\":1.5,\"averageSteps\":2,\"completedEpisodes\":1,\"truncatedEpisodes\":0,\"seed\":10,\"maxSteps\":8,\"fixedDeltaSeconds\":0.033333335}",
            "{\"schemaVersion\":1,\"type\":\"episode\",\"episodeIndex\":0,\"seed\":10,\"steps\":2,\"totalReward\":1.5,\"done\":true,\"truncated\":false,\"finalStateHash\":1234}",
            "{\"schemaVersion\":1,\"type\":\"step\",\"episodeIndex\":0,\"seed\":10,\"stepIndex\":1,\"observation\":[0,1],\"continuousAction\":[0.5],\"discreteAction\":[1],\"reward\":0.25,\"done\":false,\"truncated\":false,\"stateHash\":5678}",
            "{\"schemaVersion\":1,\"type\":\"step\",\"episodeIndex\":0,\"seed\":10,\"stepIndex\":2,\"observation\":[1,0],\"continuousAction\":[0],\"discreteAction\":[0],\"reward\":0,\"done\":true,\"truncated\":false,\"stateHash\":5679}"));

        var summary = AiTrainingJsonLinesValidator.Validate(reader);

        Assert.Equal(4, summary.TotalRecords);
        Assert.Equal(1, summary.RunRecords);
        Assert.Equal(1, summary.EpisodeRecords);
        Assert.Equal(2, summary.StepRecords);
    }

    [Fact]
    public void Validate_WithInvalidRows_ThrowsFormatException()
    {
        using var reader = new StringReader("{\"schemaVersion\":1,\"type\":\"step\",\"episodeIndex\":0}");

        var exception = Assert.Throws<FormatException>(() => AiTrainingJsonLinesValidator.Validate(reader));

        Assert.Contains("seed", exception.Message, StringComparison.Ordinal);
    }
}
