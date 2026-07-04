using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AbilityKit.AI.Abstractions;
using AbilityKit.AI.Inference;
using AbilityKit.AI.Training.Runner;
using AbilityKit.Demo.Shooter.AI;
using Xunit;

namespace AbilityKit.AI.Inference.Tests;

public sealed class AiModelPolicyTests
{
    [Fact]
    public void Decide_WithExecutorOutput_CopiesActionsToBuffer()
    {
        var spec = CreatePolicySpec(observationLength: 3, continuousLength: 2, discreteLength: 1);
        using var executor = new DelegateAiModelExecutor(spec, input =>
        {
            Assert.Equal(new[] { 1f, 2f, 3f }, input.Observation.ToArray());
            return new AiModelOutput(new[] { 0.5f, -0.25f }, new[] { 2 });
        });
        var policy = new AiModelPolicy(executor);
        var observation = new AiObservationBuffer(spec.ObservationSpec);
        observation[0] = 1f;
        observation[1] = 2f;
        observation[2] = 3f;
        var action = new AiActionBuffer(spec.ActionSpec);

        policy.Decide(in observation, action);

        Assert.Equal(new[] { 0.5f, -0.25f }, action.Continuous);
        Assert.Equal(new[] { 2 }, action.Discrete);
    }

    [Fact]
    public void Decide_WithMismatchedObservation_Throws()
    {
        var spec = CreatePolicySpec(observationLength: 3, continuousLength: 1, discreteLength: 0);
        using var executor = new DelegateAiModelExecutor(spec, _ => new AiModelOutput(new[] { 1f }, Array.Empty<int>()));
        var policy = new AiModelPolicy(executor);
        var observation = new AiObservationBuffer(new AiObservationSpec("other", 2));
        var action = new AiActionBuffer(spec.ActionSpec);

        Assert.Throws<InvalidOperationException>(() => policy.Decide(in observation, action));
    }

    [Fact]
    public void Decide_WithMismatchedModelOutput_Throws()
    {
        var spec = CreatePolicySpec(observationLength: 2, continuousLength: 2, discreteLength: 1);
        using var executor = new DelegateAiModelExecutor(spec, _ => new AiModelOutput(new[] { 1f }, new[] { 0 }));
        var policy = new AiModelPolicy(executor);
        var observation = new AiObservationBuffer(spec.ObservationSpec);
        var action = new AiActionBuffer(spec.ActionSpec);

        Assert.Throws<InvalidOperationException>(() => policy.Decide(in observation, action));
    }

    [Fact]
    public void Run_WithBehaviorCloningExecutor_ComputesLinearActions()
    {
        var spec = CreatePolicySpec(observationLength: 2, continuousLength: 2, discreteLength: 1);
        using var executor = new BehaviorCloningModelExecutor(
            spec,
            new[]
            {
                new[] { 1f, 2f },
                new[] { 0.5f, -1f },
            },
            new[] { 0.25f, -0.5f },
            new[] { 2 });
        var input = new AiModelInput(spec, new[] { 3f, 4f });

        var output = executor.Run(input);

        Assert.Equal(new[] { 11.25f, -3f }, output.ContinuousAction);
        Assert.Equal(new[] { 2 }, output.DiscreteAction);
    }

    [Fact]
    public void LoadArtifact_WithPythonModelJson_LoadsExecutablePolicy()
    {
        var directory = Directory.CreateTempSubdirectory();
        try
        {
            var modelPath = Path.Combine(directory.FullName, "model.json");
            var metadataPath = Path.Combine(directory.FullName, "metadata.json");
            File.WriteAllText(modelPath, """
            {
              "modelType": "abilitykit.behavior_cloning.linear.v1",
              "observationLength": 2,
              "continuousActionLength": 2,
              "discreteActionLength": 1,
              "continuousWeights": [[1.0, 2.0], [0.5, -1.0]],
              "continuousBias": [0.25, -0.5],
              "discreteDefaults": [2],
              "sampleCount": 3,
              "meanSquaredError": 0.01
            }
            """, Encoding.UTF8);
            File.WriteAllText(metadataPath, CreateMetadataJson(modelPath, environment: "shooter"), Encoding.UTF8);

            using var executor = BehaviorCloningModelExecutor.LoadArtifact(metadataPath, modelPath, expectedEnvironment: "shooter");
            var output = executor.Run(new AiModelInput(executor.Spec, new[] { 3f, 4f }));

            Assert.Equal(2, executor.Spec.ObservationSpec.Length);
            Assert.Equal(new[] { 11.25f, -3f }, output.ContinuousAction);
            Assert.Equal(new[] { 2 }, output.DiscreteAction);
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }

    [Fact]
    public void LoadArtifact_WithHashMismatch_Throws()
    {
        var directory = Directory.CreateTempSubdirectory();
        try
        {
            var modelPath = Path.Combine(directory.FullName, "model.json");
            var metadataPath = Path.Combine(directory.FullName, "metadata.json");
            File.WriteAllText(modelPath, """
            {
              "modelType": "abilitykit.behavior_cloning.linear.v1",
              "observationLength": 1,
              "continuousActionLength": 1,
              "discreteActionLength": 0,
              "continuousWeights": [[1.0]],
              "continuousBias": [0.0],
              "discreteDefaults": [],
              "sampleCount": 1,
              "meanSquaredError": 0.0
            }
            """, Encoding.UTF8);
            File.WriteAllText(metadataPath, CreateMetadataJson(modelPath, environment: "shooter", observationLength: 1, continuousActionLength: 1, discreteActionLength: 0), Encoding.UTF8);
            File.WriteAllText(modelPath, "{}", Encoding.UTF8);

            Assert.Throws<InvalidDataException>(() => BehaviorCloningModelExecutor.LoadArtifact(metadataPath, modelPath));
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }

    [Fact]
    public void Run_WithShooterEnvironmentAndModelPolicy_ProducesEpisodeSummary()
    {
        var runner = new AiTrainingEpisodeRunner(
            () => new ShooterAiTrainingEnvironment(new ShooterAiEnvironmentOptions(
                controlledPlayerId: 1,
                maxObservedEnemies: 2,
                maxObservedProjectiles: 2,
                enableEnemyWaves: false)),
            CreateShooterModelPolicy);

        var summary = runner.Run(new AiTrainingRunOptions(episodes: 1, seed: 12, maxSteps: 3));

        Assert.Equal(1, summary.EpisodeCount);
        Assert.Equal(3, summary.TotalSteps);
        Assert.Equal(1, summary.TruncatedEpisodes);
    }

    private static IAiPolicy CreateShooterModelPolicy()
    {
        var environment = new ShooterAiTrainingEnvironment(new ShooterAiEnvironmentOptions(
            controlledPlayerId: 1,
            maxObservedEnemies: 2,
            maxObservedProjectiles: 2,
            enableEnemyWaves: false));
        var spec = AiModelPolicySpec.FromEnvironment(environment);
        return new AiModelPolicy(new DelegateAiModelExecutor(spec, _ => new AiModelOutput(
            new[] { 0f, 1f, 0f, 1f },
            new[] { 0, 1 })));
    }

    private static string CreateMetadataJson(
        string modelPath,
        string environment,
        int observationLength = 2,
        int continuousActionLength = 2,
        int discreteActionLength = 1)
    {
        var metadata = new
        {
            schemaVersion = 1,
            artifactType = BehaviorCloningModelExecutor.SupportedArtifactType,
            environment,
            modelType = BehaviorCloningModelExecutor.SupportedModelType,
            dataSchemaVersion = 1,
            observationLength,
            continuousActionLength,
            discreteActionLength,
            sampleCount = 3,
            sourceDatasetPath = "dataset.json",
            modelPath,
            modelSha256 = ComputeSha256(modelPath),
            createdUtc = "2026-01-01T00:00:00Z",
            training = new { algorithm = "behavior_cloning_linear", dependencyProfile = "python-stdlib" },
            metrics = new { meanSquaredError = 0.01, totalReward = 1.0 },
        };
        return JsonSerializer.Serialize(metadata, new JsonSerializerOptions { WriteIndented = true });
    }

    private static string ComputeSha256(string path)
    {
        using var stream = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
    }

    private static AiModelPolicySpec CreatePolicySpec(int observationLength, int continuousLength, int discreteLength)
    {
        return new AiModelPolicySpec(
            new AiObservationSpec("obs", observationLength),
            new AiActionSpec("act", continuousLength, discreteLength),
            new AiModelTensorSpec("observation", observationLength),
            new AiModelTensorSpec("continuous_action", continuousLength),
            new AiModelTensorSpec("discrete_action", discreteLength, AiSpaceValueType.Int32));
    }
}
