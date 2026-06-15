namespace AbilityKit.Demo.Host.Console;

/// <summary>
/// Result returned from one hosted console frame.
/// </summary>
public readonly struct ConsoleHostFrameResult
{
    private ConsoleHostFrameResult(bool shouldQuit, int exitCode)
    {
        ShouldQuit = shouldQuit;
        ExitCode = exitCode;
    }

    /// <summary>
    /// Gets a result that continues the host loop.
    /// </summary>
    public static ConsoleHostFrameResult Continue { get; } = new ConsoleHostFrameResult(false, 0);

    /// <summary>
    /// Creates a result that exits the host loop.
    /// </summary>
    /// <param name="exitCode">Process exit code.</param>
    /// <returns>Exit frame result.</returns>
    public static ConsoleHostFrameResult Quit(int exitCode = 0)
    {
        return new ConsoleHostFrameResult(true, exitCode);
    }

    /// <summary>
    /// Gets a value indicating whether the host should exit.
    /// </summary>
    public bool ShouldQuit { get; }

    /// <summary>
    /// Gets the exit code used when <see cref="ShouldQuit"/> is <see langword="true"/>.
    /// </summary>
    public int ExitCode { get; }
}
