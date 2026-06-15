namespace AbilityKit.Demo.Host.Console;

/// <summary>
/// Reads keyboard input from the process console.
/// </summary>
public sealed class SystemConsoleKeyPoller : IConsoleKeyPoller
{
    /// <inheritdoc />
    public bool TryReadKey(out ConsoleKey key)
    {
        key = default;
        if (System.Console.IsInputRedirected || !System.Console.KeyAvailable)
        {
            return false;
        }

        key = System.Console.ReadKey(intercept: true).Key;
        return true;
    }
}
