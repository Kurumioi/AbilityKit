namespace AbilityKit.Demo.Host.Console;

/// <summary>
/// Provides game-specific behavior to a fixed-step console host.
/// </summary>
public interface IConsoleHostGame
{
    /// <summary>
    /// Starts game-specific state before the loop begins.
    /// </summary>
    /// <returns><see langword="true"/> when the game can enter the host loop.</returns>
    bool Start();

    /// <summary>
    /// Executes one host frame.
    /// </summary>
    /// <param name="deltaSeconds">Fixed delta time in seconds.</param>
    /// <returns>Frame control result.</returns>
    ConsoleHostFrameResult Tick(float deltaSeconds);
}
