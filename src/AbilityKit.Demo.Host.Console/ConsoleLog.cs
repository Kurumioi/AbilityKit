namespace AbilityKit.Demo.Host.Console;

/// <summary>
/// Channel-based logger for demo console hosts.
/// </summary>
public sealed class ConsoleLog
{
    private readonly List<IConsoleLogSink> _sinks = new();
    private readonly object _lock = new();
    private IConsoleOutput _output;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConsoleLog"/> class.
    /// </summary>
    /// <param name="output">Primary output target.</param>
    public ConsoleLog(IConsoleOutput output)
    {
        _output = output ?? throw new ArgumentNullException(nameof(output));
    }

    /// <summary>
    /// Gets the primary output target.
    /// </summary>
    public IConsoleOutput Output => _output;

    /// <summary>
    /// Replaces the primary output target.
    /// </summary>
    /// <param name="output">New output target.</param>
    public void SetOutput(IConsoleOutput output)
    {
        _output = output ?? throw new ArgumentNullException(nameof(output));
    }

    /// <summary>
    /// Adds a log sink.
    /// </summary>
    /// <param name="sink">Sink to add.</param>
    public void AddSink(IConsoleLogSink sink)
    {
        if (sink == null)
        {
            return;
        }

        lock (_lock)
        {
            if (!_sinks.Contains(sink))
            {
                _sinks.Add(sink);
            }
        }
    }

    /// <summary>
    /// Removes a log sink.
    /// </summary>
    /// <param name="sink">Sink to remove.</param>
    public void RemoveSink(IConsoleLogSink sink)
    {
        if (sink == null)
        {
            return;
        }

        lock (_lock)
        {
            _sinks.Remove(sink);
        }
    }

    /// <summary>
    /// Writes one log message.
    /// </summary>
    /// <param name="channel">Output channel.</param>
    /// <param name="message">Message text.</param>
    public void Write(ConsoleOutputChannel channel, string message)
    {
        _output.Write(channel, message);
        PublishToSinks(channel, message);
    }

    /// <summary>
    /// Writes one formatted log message.
    /// </summary>
    /// <param name="channel">Output channel.</param>
    /// <param name="format">Composite format string.</param>
    /// <param name="args">Format arguments.</param>
    public void WriteFormat(ConsoleOutputChannel channel, string format, params object[] args)
    {
        Write(channel, string.Format(format, args));
    }

    /// <summary>Writes a system message.</summary>
    public void System(string message) => Write(ConsoleOutputChannel.System, message);

    /// <summary>Writes a battle message.</summary>
    public void Battle(string message) => Write(ConsoleOutputChannel.Battle, message);

    /// <summary>Writes a view message.</summary>
    public void View(string message) => Write(ConsoleOutputChannel.View, message);

    /// <summary>Writes an input message.</summary>
    public void Input(string message) => Write(ConsoleOutputChannel.Input, message);

    /// <summary>Writes a warning message.</summary>
    public void Warn(string message) => Write(ConsoleOutputChannel.Warning, message);

    /// <summary>Writes an error message.</summary>
    public void Error(string message) => Write(ConsoleOutputChannel.Error, message);

    private void PublishToSinks(ConsoleOutputChannel channel, string message)
    {
        IConsoleLogSink[] sinks;
        lock (_lock)
        {
            sinks = _sinks.ToArray();
        }

        foreach (var sink in sinks)
        {
            try
            {
                sink.Log(channel, message);
            }
            catch
            {
            }
        }
    }
}
