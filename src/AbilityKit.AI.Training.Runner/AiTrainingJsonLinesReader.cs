using System.Text.Json;

namespace AbilityKit.AI.Training.Runner;

public enum AiTrainingJsonLineType
{
    Run,
    Episode,
    Step
}

public sealed class AiTrainingJsonLineRecord
{
    public AiTrainingJsonLineRecord(int lineNumber, int schemaVersion, AiTrainingJsonLineType type, JsonElement payload)
    {
        LineNumber = lineNumber;
        SchemaVersion = schemaVersion;
        Type = type;
        Payload = payload;
    }

    public int LineNumber { get; }

    public int SchemaVersion { get; }

    public AiTrainingJsonLineType Type { get; }

    public JsonElement Payload { get; }
}

public static class AiTrainingJsonLinesReader
{
    public static IReadOnlyList<AiTrainingJsonLineRecord> Read(TextReader reader)
    {
        if (reader == null) throw new ArgumentNullException(nameof(reader));

        var records = new List<AiTrainingJsonLineRecord>();
        string? line;
        var lineNumber = 0;
        while ((line = reader.ReadLine()) != null)
        {
            lineNumber++;
            if (string.IsNullOrWhiteSpace(line)) continue;

            records.Add(ReadLine(line, lineNumber));
        }

        return records;
    }

    private static AiTrainingJsonLineRecord ReadLine(string line, int lineNumber)
    {
        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(line);
        }
        catch (JsonException ex)
        {
            throw Invalid(lineNumber, "Line is not valid JSON.", ex);
        }

        using (document)
        {
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                throw Invalid(lineNumber, "Line root must be a JSON object.");
            }

            var schemaVersion = GetRequiredInt(root, "schemaVersion", lineNumber);
            if (schemaVersion != AiTrainingDataContract.SchemaVersion)
            {
                throw Invalid(lineNumber, $"Unsupported schemaVersion {schemaVersion}.");
            }

            var typeText = GetRequiredString(root, "type", lineNumber);
            var type = ParseType(typeText, lineNumber);
            ValidateByType(root, type, lineNumber);

            return new AiTrainingJsonLineRecord(lineNumber, schemaVersion, type, root.Clone());
        }
    }

    private static AiTrainingJsonLineType ParseType(string type, int lineNumber)
    {
        return type switch
        {
            "run" => AiTrainingJsonLineType.Run,
            "episode" => AiTrainingJsonLineType.Episode,
            "step" => AiTrainingJsonLineType.Step,
            _ => throw Invalid(lineNumber, $"Unsupported row type '{type}'.")
        };
    }

    private static void ValidateByType(JsonElement root, AiTrainingJsonLineType type, int lineNumber)
    {
        switch (type)
        {
            case AiTrainingJsonLineType.Run:
                RequireNumbers(root, lineNumber, "episodes", "totalSteps", "totalReward", "averageReward", "averageSteps", "completedEpisodes", "truncatedEpisodes", "seed", "maxSteps", "fixedDeltaSeconds");
                break;
            case AiTrainingJsonLineType.Episode:
                RequireNumbers(root, lineNumber, "episodeIndex", "seed", "steps", "totalReward", "finalStateHash");
                RequireBooleans(root, lineNumber, "done", "truncated");
                break;
            case AiTrainingJsonLineType.Step:
                RequireNumbers(root, lineNumber, "episodeIndex", "seed", "stepIndex", "reward", "stateHash");
                RequireBooleans(root, lineNumber, "done", "truncated");
                RequireArrays(root, lineNumber, "observation", "continuousAction", "discreteAction");
                break;
            default:
                throw Invalid(lineNumber, $"Unsupported row type '{type}'.");
        }
    }

    private static int GetRequiredInt(JsonElement root, string propertyName, int lineNumber)
    {
        if (!root.TryGetProperty(propertyName, out var value) || value.ValueKind != JsonValueKind.Number || !value.TryGetInt32(out var result))
        {
            throw Invalid(lineNumber, $"Property '{propertyName}' must be an integer.");
        }

        return result;
    }

    private static string GetRequiredString(JsonElement root, string propertyName, int lineNumber)
    {
        if (!root.TryGetProperty(propertyName, out var value) || value.ValueKind != JsonValueKind.String)
        {
            throw Invalid(lineNumber, $"Property '{propertyName}' must be a string.");
        }

        var result = value.GetString();
        if (string.IsNullOrWhiteSpace(result))
        {
            throw Invalid(lineNumber, $"Property '{propertyName}' must not be empty.");
        }

        return result;
    }

    private static void RequireNumbers(JsonElement root, int lineNumber, params string[] propertyNames)
    {
        foreach (var propertyName in propertyNames)
        {
            if (!root.TryGetProperty(propertyName, out var value) || value.ValueKind != JsonValueKind.Number)
            {
                throw Invalid(lineNumber, $"Property '{propertyName}' must be a number.");
            }
        }
    }

    private static void RequireBooleans(JsonElement root, int lineNumber, params string[] propertyNames)
    {
        foreach (var propertyName in propertyNames)
        {
            if (!root.TryGetProperty(propertyName, out var value) || value.ValueKind is not JsonValueKind.True and not JsonValueKind.False)
            {
                throw Invalid(lineNumber, $"Property '{propertyName}' must be a boolean.");
            }
        }
    }

    private static void RequireArrays(JsonElement root, int lineNumber, params string[] propertyNames)
    {
        foreach (var propertyName in propertyNames)
        {
            if (!root.TryGetProperty(propertyName, out var value) || value.ValueKind != JsonValueKind.Array)
            {
                throw Invalid(lineNumber, $"Property '{propertyName}' must be an array.");
            }
        }
    }

    private static FormatException Invalid(int lineNumber, string message, Exception? innerException = null)
    {
        return new FormatException($"AI training JSONL line {lineNumber}: {message}", innerException);
    }
}
