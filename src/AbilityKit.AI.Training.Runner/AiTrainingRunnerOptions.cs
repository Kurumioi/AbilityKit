namespace AbilityKit.AI.Training.Runner;

internal sealed class AiTrainingRunnerOptions
{
    private AiTrainingRunnerOptions()
    {
    }

    public const string ShooterEnvironment = "shooter";
    public const string MobaEnvironment = "moba";

    public bool IsValid { get; private init; }

    public bool ShowHelp { get; private init; }

    public string Error { get; private init; } = string.Empty;

    public string Environment { get; private init; } = ShooterEnvironment;

    public int Episodes { get; private init; } = 8;

    public int Seed { get; private init; } = 1;

    public int MaxSteps { get; private init; } = 600;

    public float FixedDeltaSeconds { get; private init; } = 1f / 30f;

    public int MaxObservedEnemies { get; private init; } = 8;

    public int MaxObservedProjectiles { get; private init; } = 8;

    public int MaxObservedEntities { get; private init; } = 8;

    public bool EnableEnemyWaves { get; private init; } = true;

    public string RolloutPath { get; private init; } = string.Empty;

    public string ValidatePath { get; private init; } = string.Empty;
 
    public static AiTrainingRunnerOptions Parse(string[] args)
    {
        if (args.Length == 0)
        {
            return new AiTrainingRunnerOptions { IsValid = true };
        }

        var mutable = new MutableOptions();
        for (int i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            if (Is(arg, "--help", "-h", "/?"))
            {
                return new AiTrainingRunnerOptions { IsValid = true, ShowHelp = true };
            }

            if (Is(arg, "--no-enemies"))
            {
                mutable.EnableEnemyWaves = false;
                continue;
            }

            if (!TryReadValue(args, ref i, arg, out var value, out var error))
            {
                return Invalid(error);
            }

            if (Is(arg, "--environment"))
            {
                if (!IsSupportedEnvironment(value)) return Invalid("--environment must be either shooter or moba.");
                mutable.Environment = value.ToLowerInvariant();
                continue;
            }

            if (Is(arg, "--episodes"))
            {
                if (!TryParsePositiveInt(value, out mutable.Episodes)) return Invalid("--episodes must be a positive integer.");
                continue;
            }

            if (Is(arg, "--seed"))
            {
                if (!int.TryParse(value, out mutable.Seed)) return Invalid("--seed must be an integer.");
                continue;
            }

            if (Is(arg, "--max-steps"))
            {
                if (!TryParsePositiveInt(value, out mutable.MaxSteps)) return Invalid("--max-steps must be a positive integer.");
                continue;
            }

            if (Is(arg, "--delta"))
            {
                if (!float.TryParse(value, out mutable.FixedDeltaSeconds) || mutable.FixedDeltaSeconds <= 0f) return Invalid("--delta must be a positive number.");
                continue;
            }

            if (Is(arg, "--max-enemies"))
            {
                if (!TryParseNonNegativeInt(value, out mutable.MaxObservedEnemies)) return Invalid("--max-enemies must be zero or a positive integer.");
                continue;
            }

            if (Is(arg, "--max-projectiles"))
            {
                if (!TryParseNonNegativeInt(value, out mutable.MaxObservedProjectiles)) return Invalid("--max-projectiles must be zero or a positive integer.");
                continue;
            }

            if (Is(arg, "--max-entities"))
            {
                if (!TryParsePositiveInt(value, out mutable.MaxObservedEntities)) return Invalid("--max-entities must be a positive integer.");
                continue;
            }

            if (Is(arg, "--rollout"))
            {
                mutable.RolloutPath = value;
                continue;
            }

            if (Is(arg, "--validate"))
            {
                mutable.ValidatePath = value;
                continue;
            }
 
            return Invalid($"Unknown option: {arg}");
        }

        return new AiTrainingRunnerOptions
        {
            IsValid = true,
            Environment = mutable.Environment,
            Episodes = mutable.Episodes,
            Seed = mutable.Seed,
            MaxSteps = mutable.MaxSteps,
            FixedDeltaSeconds = mutable.FixedDeltaSeconds,
            MaxObservedEnemies = mutable.MaxObservedEnemies,
            MaxObservedProjectiles = mutable.MaxObservedProjectiles,
            MaxObservedEntities = mutable.MaxObservedEntities,
            EnableEnemyWaves = mutable.EnableEnemyWaves,
            RolloutPath = mutable.RolloutPath,
            ValidatePath = mutable.ValidatePath
        };
    }

    public static void WriteUsage(TextWriter writer)
    {
        writer.WriteLine("AbilityKit AI Training Runner");
        writer.WriteLine("Usage: dotnet run --project src/AbilityKit.AI.Training.Runner -- [options]");
        writer.WriteLine("Options:");
        writer.WriteLine("  --environment <name>    Training environment: shooter or moba. Default: shooter");
        writer.WriteLine("  --episodes <n>          Episode count. Default: 8");
        writer.WriteLine("  --seed <n>              Base seed. Default: 1");
        writer.WriteLine("  --max-steps <n>         Max steps per episode. Default: 600");
        writer.WriteLine("  --delta <seconds>       Fixed simulation delta. Default: 0.0333333");
        writer.WriteLine("  --max-enemies <n>       Observation enemy slots. Default: 8");
        writer.WriteLine("  --max-projectiles <n>   Observation projectile slots. Default: 8");
        writer.WriteLine("  --max-entities <n>      MOBA observation entity slots. Default: 8");
        writer.WriteLine("  --no-enemies            Disable Shooter enemy waves for smoke runs.");
        writer.WriteLine("  --rollout <path>        Write per-step rollout JSONL to a file.");
        writer.WriteLine("  --validate <path>       Validate an AI training JSONL file and print a summary.");
        writer.WriteLine("  --help                  Show usage.");
    }

    private static AiTrainingRunnerOptions Invalid(string error) => new AiTrainingRunnerOptions { IsValid = false, Error = error };

    private static bool Is(string value, params string[] names)
    {
        for (int i = 0; i < names.Length; i++)
        {
            if (string.Equals(value, names[i], StringComparison.OrdinalIgnoreCase)) return true;
        }

        return false;
    }

    private static bool IsSupportedEnvironment(string value) =>
        Is(value, ShooterEnvironment) || Is(value, MobaEnvironment);

    private static bool TryReadValue(string[] args, ref int index, string arg, out string value, out string error)
    {
        value = string.Empty;
        error = string.Empty;
        if (index + 1 >= args.Length || args[index + 1].StartsWith("--", StringComparison.Ordinal))
        {
            error = $"Missing value for option: {arg}";
            return false;
        }

        index++;
        value = args[index];
        return true;
    }

    private static bool TryParsePositiveInt(string value, out int result) => int.TryParse(value, out result) && result > 0;

    private static bool TryParseNonNegativeInt(string value, out int result) => int.TryParse(value, out result) && result >= 0;

    private sealed class MutableOptions
    {
        public string Environment = ShooterEnvironment;
        public int Episodes = 8;
        public int Seed = 1;
        public int MaxSteps = 600;
        public float FixedDeltaSeconds = 1f / 30f;
        public int MaxObservedEnemies = 8;
        public int MaxObservedProjectiles = 8;
        public int MaxObservedEntities = 8;
        public bool EnableEnemyWaves = true;
        public string RolloutPath = string.Empty;
        public string ValidatePath = string.Empty;
    }
}
