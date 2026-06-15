namespace AbilityKit.Demo.Host.Console;

/// <summary>
/// Provides shared console platform services to demo adapters.
/// </summary>
public sealed class ConsolePlatform
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ConsolePlatform"/> class.
    /// </summary>
    public ConsolePlatform(IConsoleOutput? output = null, IConsoleKeyPoller? input = null, IConsoleRenderer? renderer = null, ConsoleLog? log = null)
    {
        Output = output ?? new SystemConsoleOutput();
        Input = input ?? new SystemConsoleKeyPoller();
        Renderer = renderer ?? new BufferedConsoleRenderer(Output);
        Log = log ?? new ConsoleLog(Output);
    }

    /// <summary>Gets the output service.</summary>
    public IConsoleOutput Output { get; }

    /// <summary>Gets the key input service.</summary>
    public IConsoleKeyPoller Input { get; }

    /// <summary>Gets the renderer service.</summary>
    public IConsoleRenderer Renderer { get; }

    /// <summary>Gets the log service.</summary>
    public ConsoleLog Log { get; }
}
