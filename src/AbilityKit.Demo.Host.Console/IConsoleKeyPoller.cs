namespace AbilityKit.Demo.Host.Console;

/// <summary>
/// Polls console keys without binding demo input mapping to <see cref="System.Console"/>.
/// </summary>
public interface IConsoleKeyPoller
{
    /// <summary>
    /// Attempts to read the next available console key.
    /// </summary>
    /// <param name="key">Read key when one is available.</param>
    /// <returns><see langword="true"/> when a key was read.</returns>
    bool TryReadKey(out ConsoleKey key);
}
