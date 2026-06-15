using AbilityKit.Demo.Host.Console;

namespace AbilityKit.Demo.Shooter.Console;

internal sealed class KeyboardShooterConsoleInputSource : IShooterConsoleInputSource
{
    private readonly IConsoleKeyPoller _keyPoller;
    private float _lastAimX = 1f;
    private float _lastAimY;

    public KeyboardShooterConsoleInputSource(IConsoleKeyPoller keyPoller)
    {
        _keyPoller = keyPoller ?? throw new ArgumentNullException(nameof(keyPoller));
    }

    public ShooterConsoleInputState Poll()
    {
        var moveX = 0f;
        var moveY = 0f;
        var aimX = 0f;
        var aimY = 0f;
        var fire = false;
        var quit = false;
        var pause = false;
        var help = false;

        while (_keyPoller.TryReadKey(out var key))
        {
            switch (key)
            {
                case ConsoleKey.W:
                    moveY += 1f;
                    break;
                case ConsoleKey.S:
                    moveY -= 1f;
                    break;
                case ConsoleKey.A:
                    moveX -= 1f;
                    break;
                case ConsoleKey.D:
                    moveX += 1f;
                    break;
                case ConsoleKey.UpArrow:
                    aimY = 1f;
                    break;
                case ConsoleKey.DownArrow:
                    aimY = -1f;
                    break;
                case ConsoleKey.LeftArrow:
                    aimX = -1f;
                    break;
                case ConsoleKey.RightArrow:
                    aimX = 1f;
                    break;
                case ConsoleKey.Spacebar:
                    fire = true;
                    break;
                case ConsoleKey.P:
                    pause = true;
                    break;
                case ConsoleKey.H:
                    help = true;
                    break;
                case ConsoleKey.Q:
                case ConsoleKey.Escape:
                    quit = true;
                    break;
            }
        }

        Normalize(ref moveX, ref moveY);
        Normalize(ref aimX, ref aimY);
        if (aimX != 0f || aimY != 0f)
        {
            _lastAimX = aimX;
            _lastAimY = aimY;
        }

        return new ShooterConsoleInputState(moveX, moveY, _lastAimX, _lastAimY, fire, quit, pause, help);
    }

    private static void Normalize(ref float x, ref float y)
    {
        var magnitude = MathF.Sqrt((x * x) + (y * y));
        if (magnitude <= 0f)
        {
            return;
        }

        x /= magnitude;
        y /= magnitude;
    }
}
