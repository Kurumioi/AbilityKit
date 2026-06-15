namespace AbilityKit.Demo.Host.Console;

/// <summary>
/// Platform-independent console output abstraction.
/// </summary>
public interface IConsoleOutput
{
    /// <summary>
    /// Writes one line to a logical output channel.
    /// </summary>
    /// <param name="channel">Output channel.</param>
    /// <param name="message">Message text.</param>
    void Write(ConsoleOutputChannel channel, string message);

    /// <summary>
    /// Writes formatted text to a logical output channel.
    /// </summary>
    /// <param name="channel">Output channel.</param>
    /// <param name="format">Composite format string.</param>
    /// <param name="args">Format arguments.</param>
    void WriteFormat(ConsoleOutputChannel channel, string format, params object[] args);

    /// <summary>
    /// Clears the output surface when supported.
    /// </summary>
    void Clear();

    /// <summary>
    /// Writes a separator line.
    /// </summary>
    /// <param name="channel">Output channel.</param>
    /// <param name="character">Separator character.</param>
    /// <param name="length">Separator length.</param>
    void WriteSeparator(ConsoleOutputChannel channel = ConsoleOutputChannel.System, char character = '=', int length = 60);

    /// <summary>
    /// Writes a bordered title.
    /// </summary>
    /// <param name="channel">Output channel.</param>
    /// <param name="title">Title text.</param>
    /// <param name="borderCharacter">Border character.</param>
    /// <param name="width">Title width.</param>
    void WriteTitle(ConsoleOutputChannel channel, string title, char borderCharacter = '=', int width = 60);
}
