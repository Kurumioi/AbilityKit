namespace AbilityKit.Demo.Host.Console;

/// <summary>
/// Receives console log messages for files, test probes, or external collectors.
/// </summary>
public interface IConsoleLogSink
{
    /// <summary>
    /// Gets the sink name.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Receives one log message.
    /// </summary>
    /// <param name="channel">Output channel.</param>
    /// <param name="message">Message text.</param>
    void Log(ConsoleOutputChannel channel, string message);
}
