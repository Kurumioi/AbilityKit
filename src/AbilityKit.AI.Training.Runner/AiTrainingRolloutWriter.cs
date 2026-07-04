using System.Text.Json;
using System.Text.Json.Serialization;
using AbilityKit.AI.Abstractions;

namespace AbilityKit.AI.Training.Runner;

public readonly struct AiTrainingRolloutStep
{
    public AiTrainingRolloutStep(
        int episodeIndex,
        int seed,
        AiStepResult step,
        AiActionBuffer action)
    {
        EpisodeIndex = episodeIndex < 0 ? 0 : episodeIndex;
        Seed = seed;
        StepIndex = step.StepIndex;
        Observation = Copy(step.Observation.Values);
        ContinuousAction = Copy(action.Continuous);
        DiscreteAction = Copy(action.Discrete);
        Reward = step.Reward;
        Done = step.Done;
        Truncated = step.Truncated;
        StateHash = step.StateHash;
    }

    public int EpisodeIndex { get; }

    public int Seed { get; }

    public int StepIndex { get; }

    public float[] Observation { get; }

    public float[] ContinuousAction { get; }

    public int[] DiscreteAction { get; }

    public float Reward { get; }

    public bool Done { get; }

    public bool Truncated { get; }

    public uint StateHash { get; }

    private static float[] Copy(float[] source)
    {
        if (source.Length == 0) return Array.Empty<float>();

        var copy = new float[source.Length];
        Array.Copy(source, copy, source.Length);
        return copy;
    }

    private static int[] Copy(int[] source)
    {
        if (source.Length == 0) return Array.Empty<int>();

        var copy = new int[source.Length];
        Array.Copy(source, copy, source.Length);
        return copy;
    }
}

public interface IAiTrainingRolloutSink
{
    void WriteStep(in AiTrainingRolloutStep step);
}

public sealed class AiTrainingRolloutJsonLinesWriter : IAiTrainingRolloutSink, IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
    {
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never
    };

    private readonly TextWriter _writer;
    private readonly bool _leaveOpen;

    public AiTrainingRolloutJsonLinesWriter(TextWriter writer, bool leaveOpen = false)
    {
        _writer = writer ?? throw new ArgumentNullException(nameof(writer));
        _leaveOpen = leaveOpen;
    }

    public void WriteStep(in AiTrainingRolloutStep step)
    {
        _writer.WriteLine(JsonSerializer.Serialize(new
        {
            schemaVersion = AiTrainingDataContract.SchemaVersion,
            type = "step",
            episodeIndex = step.EpisodeIndex,
            seed = step.Seed,
            stepIndex = step.StepIndex,
            observation = step.Observation,
            continuousAction = step.ContinuousAction,
            discreteAction = step.DiscreteAction,
            reward = step.Reward,
            done = step.Done,
            truncated = step.Truncated,
            stateHash = step.StateHash
        }, JsonOptions));
    }

    public void Dispose()
    {
        if (!_leaveOpen)
        {
            _writer.Dispose();
        }
    }
}
