namespace AbilityKit.Demo.Host.Console;

/// <summary>
/// Runs a game-specific console adapter at a fixed timestep.
/// </summary>
public sealed class FixedStepConsoleHost
{
    private readonly IConsoleHostGame _game;
    private readonly ConsoleHostOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="FixedStepConsoleHost"/> class.
    /// </summary>
    /// <param name="game">Game-specific console adapter.</param>
    /// <param name="options">Host loop options.</param>
    public FixedStepConsoleHost(IConsoleHostGame game, ConsoleHostOptions options)
    {
        _game = game ?? throw new ArgumentNullException(nameof(game));
        _options = options.TargetFrameRate > 0 ? options : ConsoleHostOptions.Default;
    }

    /// <summary>
    /// Runs the fixed-step console loop.
    /// </summary>
    /// <returns>Process exit code.</returns>
    public int Run()
    {
        if (!_game.Start())
        {
            return 1;
        }

        while (true)
        {
            var frameStartedAt = DateTime.UtcNow;
            var result = _game.Tick(_options.FixedDeltaSeconds);
            if (result.ShouldQuit)
            {
                return result.ExitCode;
            }

            SleepRemainder(frameStartedAt, _options.FrameDuration);
        }
    }

    private static void SleepRemainder(DateTime frameStartedAt, TimeSpan frameDuration)
    {
        var elapsed = DateTime.UtcNow - frameStartedAt;
        var remaining = frameDuration - elapsed;
        if (remaining > TimeSpan.Zero)
        {
            Thread.Sleep(remaining);
        }
    }
}
