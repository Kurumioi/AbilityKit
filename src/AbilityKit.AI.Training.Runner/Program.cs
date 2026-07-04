using AbilityKit.Demo.Moba.AI;
using AbilityKit.Demo.Shooter.AI;

namespace AbilityKit.AI.Training.Runner;

internal static class Program
{
    private static int Main(string[] args)
    {
        var options = AiTrainingRunnerOptions.Parse(args);
        if (!options.IsValid)
        {
            Console.Error.WriteLine(options.Error);
            AiTrainingRunnerOptions.WriteUsage(Console.Error);
            return 2;
        }

        if (options.ShowHelp)
        {
            AiTrainingRunnerOptions.WriteUsage(Console.Out);
            return 0;
        }

        if (!string.IsNullOrWhiteSpace(options.ValidatePath))
        {
            return ValidateJsonLines(options.ValidatePath, Console.Out, Console.Error);
        }

        var runner = CreateRunner(options);

        var originalOut = Console.Out;
        AiTrainingRunSummary summary;
        using var rolloutWriter = CreateRolloutWriter(options.RolloutPath);
        try
        {
            Console.SetOut(TextWriter.Null);
            summary = runner.Run(
                new AiTrainingRunOptions(
                    options.Episodes,
                    options.Seed,
                    options.MaxSteps,
                    options.FixedDeltaSeconds),
                rolloutWriter);
        }
        finally
        {
            Console.SetOut(originalOut);
        }

        AiTrainingReportWriter.WriteJsonLines(summary, originalOut);
        return 0;
    }

    private static int ValidateJsonLines(string path, TextWriter output, TextWriter error)
    {
        try
        {
            var summary = AiTrainingJsonLinesValidator.ValidateFile(path);
            output.WriteLine($"valid=true totalRecords={summary.TotalRecords} runRecords={summary.RunRecords} episodeRecords={summary.EpisodeRecords} stepRecords={summary.StepRecords}");
            return 0;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or FormatException or ArgumentException)
        {
            error.WriteLine($"AI training JSONL validation failed: {ex.Message}");
            return 3;
        }
    }

    private static AiTrainingEpisodeRunner CreateRunner(AiTrainingRunnerOptions options)
    {
        if (string.Equals(options.Environment, AiTrainingRunnerOptions.MobaEnvironment, StringComparison.OrdinalIgnoreCase))
        {
            return new AiTrainingEpisodeRunner(
                () => new MobaAiTrainingEnvironment(new MobaAiEnvironmentOptions(maxObservedEntities: options.MaxObservedEntities)),
                () => new MobaAiForwardSkillPolicy());
        }

        return new AiTrainingEpisodeRunner(
            () => new ShooterAiTrainingEnvironment(new ShooterAiEnvironmentOptions(
                controlledPlayerId: 1,
                maxObservedEnemies: options.MaxObservedEnemies,
                maxObservedProjectiles: options.MaxObservedProjectiles,
                enableEnemyWaves: options.EnableEnemyWaves)),
            () => new ShooterAiForwardFirePolicy());
    }

    private static AiTrainingRolloutJsonLinesWriter? CreateRolloutWriter(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return null;

        var directory = Path.GetDirectoryName(Path.GetFullPath(path));
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        return new AiTrainingRolloutJsonLinesWriter(new StreamWriter(path, append: false));
    }
}
