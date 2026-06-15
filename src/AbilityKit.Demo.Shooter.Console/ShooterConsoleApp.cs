using AbilityKit.Demo.Host.Console;
using AbilityKit.Demo.Shooter.Runtime;
using AbilityKit.Protocol.Shooter;

namespace AbilityKit.Demo.Shooter.Console;

internal sealed class ShooterConsoleApp : IConsoleHostGame
{
    private const int LocalPlayerId = 1;
    private const int TargetFrameRate = 30;

    private readonly ShooterBattleRuntimePort _runtime;
    private readonly IShooterConsoleInputSource _input;
    private readonly ShooterConsoleRenderer _renderer;
    private readonly ConsoleLog _log;
    private bool _paused;
    private bool _quit;
    private float _lastAimX = 1f;
    private float _lastAimY;

    public ShooterConsoleApp(ShooterBattleRuntimePort runtime, IShooterConsoleInputSource input, ShooterConsoleRenderer renderer, ConsoleLog log)
    {
        _runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
        _input = input ?? throw new ArgumentNullException(nameof(input));
        _renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
        _log = log ?? throw new ArgumentNullException(nameof(log));
    }

    public bool Start()
    {
        var start = CreateDefaultStartPayload();
        if (!_runtime.StartGame(in start))
        {
            _log.Error("Shooter console failed to start runtime.");
            return false;
        }

        _renderer.RenderHelp();
        _renderer.Render(_runtime.GetSnapshot(), _runtime.ComputeStateHash(), _paused);
        return true;
    }

    public ConsoleHostFrameResult Tick(float deltaSeconds)
    {
        var input = _input.Poll();
        if (input.Help)
        {
            _renderer.RenderHelp();
        }

        if (input.Pause)
        {
            _paused = !_paused;
        }

        if (input.Quit)
        {
            _quit = true;
            return ConsoleHostFrameResult.Quit();
        }

        if (!_paused)
        {
            var command = input.ToCommand(LocalPlayerId, _lastAimX, _lastAimY);
            _lastAimX = command.AimX;
            _lastAimY = command.AimY;
            _runtime.SubmitInput(_runtime.CurrentFrame, new[] { command, CreateBotCommand(_runtime.CurrentFrame) });
            _runtime.Tick(deltaSeconds);
        }

        _renderer.Render(_runtime.GetSnapshot(), _runtime.ComputeStateHash(), _paused);
        return _quit ? ConsoleHostFrameResult.Quit() : ConsoleHostFrameResult.Continue;
    }

    private static ShooterStartGamePayload CreateDefaultStartPayload()
    {
        return new ShooterStartGamePayload(
            "console-local",
            TargetFrameRate,
            20260615,
            new[]
            {
                new ShooterStartPlayer(1, "P1", -2f, 0f),
                new ShooterStartPlayer(2, "BOT", 2f, 0f)
            });
    }

    private static ShooterPlayerCommand CreateBotCommand(int frame)
    {
        var fire = frame % 45 == 0;
        return new ShooterPlayerCommand(2, 0f, 0f, -1f, 0f, fire);
    }
}
