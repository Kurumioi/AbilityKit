using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using AbilityKit.AI.Abstractions;

namespace AbilityKit.AI.Inference;

/// <summary>
/// 执行由离线训练工具导出的线性行为克隆模型。
/// </summary>
public sealed class BehaviorCloningModelExecutor : IAiModelExecutor
{
    /// <summary>
    /// 获取当前支持的行为克隆模型类型。
    /// </summary>
    public const string SupportedModelType = "abilitykit.behavior_cloning.linear.v1";

    /// <summary>
    /// 获取当前支持的模型产物清单类型。
    /// </summary>
    public const string SupportedArtifactType = "abilitykit.ai.model-artifact.v1";

    private readonly float[][] _continuousWeights;
    private readonly float[] _continuousBias;
    private readonly int[] _discreteDefaults;

    /// <summary>
    /// 创建线性行为克隆模型执行器。
    /// </summary>
    public BehaviorCloningModelExecutor(
        AiModelPolicySpec spec,
        float[][] continuousWeights,
        float[] continuousBias,
        int[] discreteDefaults)
    {
        Spec = spec;
        _continuousWeights = ValidateWeights(continuousWeights, spec.ActionSpec.ContinuousLength, spec.ObservationSpec.Length);
        _continuousBias = ValidateVector(continuousBias, spec.ActionSpec.ContinuousLength, nameof(continuousBias));
        _discreteDefaults = ValidateVector(discreteDefaults, spec.ActionSpec.DiscreteLength, nameof(discreteDefaults));
    }

    /// <inheritdoc />
    public AiModelPolicySpec Spec { get; }

    /// <summary>
    /// 从模型 JSON 和 metadata JSON 加载执行器，并校验文件 hash 与维度契约。
    /// </summary>
    public static BehaviorCloningModelExecutor LoadArtifact(string metadataPath, string modelPath, string? expectedEnvironment = null)
    {
        if (string.IsNullOrWhiteSpace(metadataPath)) throw new ArgumentException("Metadata path must not be empty.", nameof(metadataPath));
        if (string.IsNullOrWhiteSpace(modelPath)) throw new ArgumentException("Model path must not be empty.", nameof(modelPath));

        using var metadataDocument = JsonDocument.Parse(File.ReadAllText(metadataPath));
        using var modelDocument = JsonDocument.Parse(File.ReadAllText(modelPath));
        var metadata = metadataDocument.RootElement;
        var model = modelDocument.RootElement;

        var artifactType = RequireString(metadata, "artifactType");
        if (!string.Equals(artifactType, SupportedArtifactType, StringComparison.Ordinal))
        {
            throw new InvalidDataException("Unsupported AI model artifact type.");
        }

        var modelType = RequireString(metadata, "modelType");
        if (!string.Equals(modelType, SupportedModelType, StringComparison.Ordinal))
        {
            throw new InvalidDataException("Unsupported AI model type.");
        }

        var environment = RequireString(metadata, "environment");
        if (!string.IsNullOrWhiteSpace(expectedEnvironment) && !string.Equals(environment, expectedEnvironment, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException("AI model artifact environment does not match the expected environment.");
        }

        if (RequireInt(metadata, "schemaVersion") != 1 || RequireInt(metadata, "dataSchemaVersion") != 1)
        {
            throw new InvalidDataException("Unsupported AI model artifact schema version.");
        }

        var expectedHash = RequireString(metadata, "modelSha256");
        var actualHash = ComputeSha256(modelPath);
        if (!string.Equals(expectedHash, actualHash, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException("AI model artifact hash does not match the model file.");
        }

        if (!string.Equals(RequireString(model, "modelType"), SupportedModelType, StringComparison.Ordinal))
        {
            throw new InvalidDataException("AI model file type does not match the supported behavior cloning format.");
        }

        var observationLength = RequireInt(metadata, "observationLength");
        var continuousActionLength = RequireInt(metadata, "continuousActionLength");
        var discreteActionLength = RequireInt(metadata, "discreteActionLength");
        if (RequireInt(model, "observationLength") != observationLength ||
            RequireInt(model, "continuousActionLength") != continuousActionLength ||
            RequireInt(model, "discreteActionLength") != discreteActionLength)
        {
            throw new InvalidDataException("AI model dimensions do not match metadata.");
        }

        var spec = new AiModelPolicySpec(
            new AiObservationSpec(environment, observationLength),
            new AiActionSpec(environment, continuousActionLength, discreteActionLength),
            new AiModelTensorSpec("observation", observationLength),
            new AiModelTensorSpec("continuous_action", continuousActionLength),
            new AiModelTensorSpec("discrete_action", discreteActionLength, AiSpaceValueType.Int32));

        return new BehaviorCloningModelExecutor(
            spec,
            ReadFloatMatrix(model, "continuousWeights"),
            ReadFloatArray(model, "continuousBias"),
            ReadIntArray(model, "discreteDefaults"));
    }

    /// <inheritdoc />
    public AiModelOutput Run(AiModelInput input)
    {
        if (input == null) throw new ArgumentNullException(nameof(input));
        if (input.Spec.ObservationSpec.Length != Spec.ObservationSpec.Length ||
            input.Spec.ActionSpec.ContinuousLength != Spec.ActionSpec.ContinuousLength ||
            input.Spec.ActionSpec.DiscreteLength != Spec.ActionSpec.DiscreteLength)
        {
            throw new ArgumentException("AI model input spec does not match this behavior cloning executor.", nameof(input));
        }

        var observation = input.Observation.Span;
        var continuous = new float[_continuousBias.Length];
        for (var outputIndex = 0; outputIndex < continuous.Length; outputIndex++)
        {
            var value = _continuousBias[outputIndex];
            var weights = _continuousWeights[outputIndex];
            for (var inputIndex = 0; inputIndex < observation.Length; inputIndex++)
            {
                value += weights[inputIndex] * observation[inputIndex];
            }

            continuous[outputIndex] = value;
        }

        var discrete = new int[_discreteDefaults.Length];
        Array.Copy(_discreteDefaults, discrete, discrete.Length);
        return new AiModelOutput(continuous, discrete);
    }

    /// <inheritdoc />
    public void Dispose()
    {
    }

    private static float[][] ValidateWeights(float[][] weights, int continuousActionLength, int observationLength)
    {
        if (weights == null) throw new ArgumentNullException(nameof(weights));
        if (weights.Length != continuousActionLength)
        {
            throw new ArgumentException("Continuous weight row count does not match action length.", nameof(weights));
        }

        var copy = new float[weights.Length][];
        for (var rowIndex = 0; rowIndex < weights.Length; rowIndex++)
        {
            copy[rowIndex] = ValidateVector(weights[rowIndex], observationLength, nameof(weights));
        }

        return copy;
    }

    private static float[] ValidateVector(float[] vector, int expectedLength, string parameterName)
    {
        if (vector == null) throw new ArgumentNullException(parameterName);
        if (vector.Length != expectedLength)
        {
            throw new ArgumentException("Vector length does not match the model spec.", parameterName);
        }

        return vector.ToArray();
    }

    private static int[] ValidateVector(int[] vector, int expectedLength, string parameterName)
    {
        if (vector == null) throw new ArgumentNullException(parameterName);
        if (vector.Length != expectedLength)
        {
            throw new ArgumentException("Vector length does not match the model spec.", parameterName);
        }

        return vector.ToArray();
    }

    private static string ComputeSha256(string path)
    {
        using var stream = File.OpenRead(path);
        var hash = SHA256.HashData(stream);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static string RequireString(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property) || property.ValueKind != JsonValueKind.String)
        {
            throw new InvalidDataException(string.Create(CultureInfo.InvariantCulture, $"AI model artifact field '{propertyName}' must be a string."));
        }

        return property.GetString()!;
    }

    private static int RequireInt(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property) || property.ValueKind != JsonValueKind.Number || !property.TryGetInt32(out var value))
        {
            throw new InvalidDataException(string.Create(CultureInfo.InvariantCulture, $"AI model artifact field '{propertyName}' must be an integer."));
        }

        return value;
    }

    private static float[] ReadFloatArray(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property) || property.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidDataException(string.Create(CultureInfo.InvariantCulture, $"AI model field '{propertyName}' must be an array."));
        }

        return ReadFloatArrayValue(property, propertyName);
    }

    private static float[] ReadFloatArrayValue(JsonElement property, string propertyName)
    {
        var values = new float[property.GetArrayLength()];
        var index = 0;
        foreach (var item in property.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Number || !item.TryGetSingle(out values[index]))
            {
                throw new InvalidDataException(string.Create(CultureInfo.InvariantCulture, $"AI model field '{propertyName}' must contain numbers."));
            }

            index++;
        }

        return values;
    }

    private static int[] ReadIntArray(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property) || property.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidDataException(string.Create(CultureInfo.InvariantCulture, $"AI model field '{propertyName}' must be an array."));
        }

        var values = new int[property.GetArrayLength()];
        var index = 0;
        foreach (var item in property.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Number || !item.TryGetInt32(out values[index]))
            {
                throw new InvalidDataException(string.Create(CultureInfo.InvariantCulture, $"AI model field '{propertyName}' must contain integers."));
            }

            index++;
        }

        return values;
    }

    private static float[][] ReadFloatMatrix(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property) || property.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidDataException(string.Create(CultureInfo.InvariantCulture, $"AI model field '{propertyName}' must be a matrix."));
        }

        var values = new float[property.GetArrayLength()][];
        var index = 0;
        foreach (var row in property.EnumerateArray())
        {
            if (row.ValueKind != JsonValueKind.Array)
            {
                throw new InvalidDataException(string.Create(CultureInfo.InvariantCulture, $"AI model field '{propertyName}' must contain arrays."));
            }

            values[index] = ReadFloatArrayValue(row, propertyName);
            index++;
        }

        return values;
    }
}
