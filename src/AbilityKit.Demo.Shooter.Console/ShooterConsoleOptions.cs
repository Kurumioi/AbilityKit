using AbilityKit.Network.Runtime;

namespace AbilityKit.Demo.Shooter.Console;

internal enum ShooterConsoleMode
{
    Menu,
    Local,
    Spec,
    Acceptance,
    Matrix,
    RemoteSmoke
}

internal sealed class ShooterConsoleOptions
{
    public ShooterConsoleMode Mode { get; private init; } = ShooterConsoleMode.Menu;

    public string SpecId { get; private init; } = "basic-combat";

    public string SyncId { get; private init; } = "predict-rollback";

    public string NetworkId { get; private init; } = "ideal";

    public int Frames { get; private init; } = 120;

    public int Seed { get; private init; }

    public bool Authoritative { get; private init; } = true;

    public bool Render { get; private init; }

    public bool Help { get; private init; }

    public bool Menu { get; private init; }

    public string? Error { get; private init; }

    public bool IsValid => string.IsNullOrEmpty(Error);

    public static ShooterConsoleOptions Parse(string[] args)
    {
        if (args.Length == 0)
        {
            return new ShooterConsoleOptions { Mode = ShooterConsoleMode.Menu, Menu = true };
        }

        var options = new MutableOptions();
        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            if (Is(arg, "--help", "-h", "/?"))
            {
                options.Help = true;
                options.Mode = ShooterConsoleMode.Menu;
                continue;
            }

            if (Is(arg, "--menu"))
            {
                options.Menu = true;
                options.Mode = ShooterConsoleMode.Menu;
                continue;
            }

            if (Is(arg, "--render"))
            {
                options.Render = true;
                continue;
            }

            if (Is(arg, "--headless", "--no-render"))
            {
                options.Render = false;
                continue;
            }

            if (Is(arg, "--authoritative"))
            {
                options.Authoritative = true;
                continue;
            }

            if (Is(arg, "--no-authoritative"))
            {
                options.Authoritative = false;
                continue;
            }

            if (!TryReadValue(args, ref i, arg, out var value, out var error))
            {
                return FromError(error);
            }

            switch (NormalizeKey(arg))
            {
                case "mode":
                    if (!TryParseMode(value, out options.Mode)) return FromError($"Unknown mode '{value}'.");
                    break;
                case "spec":
                    options.SpecId = value;
                    break;
                case "sync":
                    options.SyncId = value;
                    break;
                case "network":
                    options.NetworkId = value;
                    break;
                case "frames":
                    if (!int.TryParse(value, out options.Frames) || options.Frames <= 0) return FromError("--frames must be a positive integer.");
                    break;
                case "seed":
                    if (!int.TryParse(value, out options.Seed)) return FromError("--seed must be an integer.");
                    break;
                default:
                    return FromError($"Unknown argument '{arg}'.");
            }
        }

        if (options.Help || options.Menu)
        {
            options.Mode = ShooterConsoleMode.Menu;
        }

        return options.ToImmutable();
    }

    public static string ToSyncId(NetworkSyncModel model)
    {
        return model switch
        {
            NetworkSyncModel.PredictRollback => "predict-rollback",
            NetworkSyncModel.AuthoritativeInterpolation => "authoritative-interpolation",
            NetworkSyncModel.HybridHeroPrediction => "hybrid-hero-prediction",
            _ => model.ToString().ToLowerInvariant()
        };
    }

    private static ShooterConsoleOptions FromError(string error)
    {
        return new ShooterConsoleOptions { Error = error, Mode = ShooterConsoleMode.Menu };
    }

    private static bool Is(string value, params string[] names)
    {
        for (var i = 0; i < names.Length; i++)
        {
            if (string.Equals(value, names[i], StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static bool TryReadValue(string[] args, ref int index, string arg, out string value, out string error)
    {
        value = string.Empty;
        error = string.Empty;

        var separator = arg.IndexOf('=', StringComparison.Ordinal);
        if (separator >= 0)
        {
            value = arg[(separator + 1)..];
            return !string.IsNullOrWhiteSpace(value) || Fail($"Argument '{arg}' requires a value.", out error);
        }

        if (!arg.StartsWith("--", StringComparison.Ordinal))
        {
            return Fail($"Unknown argument '{arg}'.", out error);
        }

        if (index + 1 >= args.Length || args[index + 1].StartsWith("--", StringComparison.Ordinal))
        {
            return Fail($"Argument '{arg}' requires a value.", out error);
        }

        index++;
        value = args[index];
        return true;
    }

    private static bool Fail(string message, out string error)
    {
        error = message;
        return false;
    }

    private static string NormalizeKey(string arg)
    {
        var key = arg;
        var separator = key.IndexOf('=', StringComparison.Ordinal);
        if (separator >= 0)
        {
            key = key[..separator];
        }

        return key.TrimStart('-').ToLowerInvariant();
    }

    private static bool TryParseMode(string value, out ShooterConsoleMode mode)
    {
        switch (value.Trim().ToLowerInvariant())
        {
            case "menu":
            case "help":
                mode = ShooterConsoleMode.Menu;
                return true;
            case "local":
            case "play":
                mode = ShooterConsoleMode.Local;
                return true;
            case "spec":
                mode = ShooterConsoleMode.Spec;
                return true;
            case "acceptance":
            case "session":
                mode = ShooterConsoleMode.Acceptance;
                return true;
            case "matrix":
                mode = ShooterConsoleMode.Matrix;
                return true;
            case "remote-smoke":
            case "remote":
            case "gateway-smoke":
                mode = ShooterConsoleMode.RemoteSmoke;
                return true;
            default:
                mode = ShooterConsoleMode.Menu;
                return false;
        }
    }

    private sealed class MutableOptions
    {
        public ShooterConsoleMode Mode = ShooterConsoleMode.Menu;
        public string SpecId = "basic-combat";
        public string SyncId = "predict-rollback";
        public string NetworkId = "ideal";
        public int Frames = 120;
        public int Seed;
        public bool Authoritative = true;
        public bool Render;
        public bool Help;
        public bool Menu;

        public ShooterConsoleOptions ToImmutable()
        {
            return new ShooterConsoleOptions
            {
                Mode = Mode,
                SpecId = SpecId,
                SyncId = SyncId,
                NetworkId = NetworkId,
                Frames = Frames,
                Seed = Seed,
                Authoritative = Authoritative,
                Render = Render,
                Help = Help,
                Menu = Menu
            };
        }
    }
}
