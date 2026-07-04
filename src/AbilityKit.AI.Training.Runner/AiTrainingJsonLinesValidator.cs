namespace AbilityKit.AI.Training.Runner;

public sealed class AiTrainingJsonLinesValidationSummary
{
    public AiTrainingJsonLinesValidationSummary(int totalRecords, int runRecords, int episodeRecords, int stepRecords)
    {
        TotalRecords = totalRecords;
        RunRecords = runRecords;
        EpisodeRecords = episodeRecords;
        StepRecords = stepRecords;
    }

    public int TotalRecords { get; }

    public int RunRecords { get; }

    public int EpisodeRecords { get; }

    public int StepRecords { get; }
}

public static class AiTrainingJsonLinesValidator
{
    public static AiTrainingJsonLinesValidationSummary ValidateFile(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("Path must not be empty.", nameof(path));

        using var reader = File.OpenText(path);
        return Validate(reader);
    }

    public static AiTrainingJsonLinesValidationSummary Validate(TextReader reader)
    {
        var records = AiTrainingJsonLinesReader.Read(reader);
        var runRecords = 0;
        var episodeRecords = 0;
        var stepRecords = 0;

        foreach (var record in records)
        {
            switch (record.Type)
            {
                case AiTrainingJsonLineType.Run:
                    runRecords++;
                    break;
                case AiTrainingJsonLineType.Episode:
                    episodeRecords++;
                    break;
                case AiTrainingJsonLineType.Step:
                    stepRecords++;
                    break;
                default:
                    throw new InvalidOperationException($"Unsupported AI training JSONL record type '{record.Type}'.");
            }
        }

        return new AiTrainingJsonLinesValidationSummary(records.Count, runRecords, episodeRecords, stepRecords);
    }
}
