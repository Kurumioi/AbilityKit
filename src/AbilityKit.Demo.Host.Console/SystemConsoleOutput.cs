namespace AbilityKit.Demo.Host.Console;

/// <summary>
/// Writes platform output to <see cref="System.Console"/> with channel prefixes and colors.
/// </summary>
public sealed class SystemConsoleOutput : IConsoleOutput
{
    private readonly Dictionary<ConsoleOutputChannel, ConsoleColor> _colors = new()
    {
        [ConsoleOutputChannel.System] = ConsoleColor.Cyan,
        [ConsoleOutputChannel.Phase] = ConsoleColor.Green,
        [ConsoleOutputChannel.View] = ConsoleColor.Blue,
        [ConsoleOutputChannel.Input] = ConsoleColor.Magenta,
        [ConsoleOutputChannel.Battle] = ConsoleColor.White,
        [ConsoleOutputChannel.Sync] = ConsoleColor.DarkCyan,
        [ConsoleOutputChannel.Config] = ConsoleColor.DarkGreen,
        [ConsoleOutputChannel.Debug] = ConsoleColor.DarkGray,
        [ConsoleOutputChannel.Warning] = ConsoleColor.Yellow,
        [ConsoleOutputChannel.Error] = ConsoleColor.Red,
        [ConsoleOutputChannel.Trace] = ConsoleColor.DarkMagenta,
    };

    /// <inheritdoc />
    public void Write(ConsoleOutputChannel channel, string message)
    {
        var prefix = GetPrefix(channel);
        if (System.Console.IsOutputRedirected)
        {
            System.Console.WriteLine($"{prefix} {message}");
            return;
        }

        var originalColor = System.Console.ForegroundColor;
        try
        {
            System.Console.ForegroundColor = _colors.TryGetValue(channel, out var color) ? color : ConsoleColor.Gray;
            System.Console.WriteLine($"{prefix} {message}");
        }
        finally
        {
            System.Console.ForegroundColor = originalColor;
        }
    }

    /// <inheritdoc />
    public void WriteFormat(ConsoleOutputChannel channel, string format, params object[] args)
    {
        Write(channel, string.Format(format, args));
    }

    /// <inheritdoc />
    public void Clear()
    {
        if (System.Console.IsOutputRedirected)
        {
            return;
        }

        System.Console.Clear();
    }

    /// <inheritdoc />
    public void WriteSeparator(ConsoleOutputChannel channel = ConsoleOutputChannel.System, char character = '=', int length = 60)
    {
        Write(channel, new string(character, Math.Max(1, length)));
    }

    /// <inheritdoc />
    public void WriteTitle(ConsoleOutputChannel channel, string title, char borderCharacter = '=', int width = 60)
    {
        var safeWidth = Math.Max(title.Length + 4, width);
        var innerWidth = safeWidth - 4;
        var leftPadding = Math.Max(0, (innerWidth - title.Length) / 2);
        var rightPadding = Math.Max(0, innerWidth - title.Length - leftPadding);

        WriteSeparator(channel, borderCharacter, safeWidth);
        Write(channel, "|" + new string(' ', leftPadding) + title + new string(' ', rightPadding) + "|");
        WriteSeparator(channel, borderCharacter, safeWidth);
    }

    private static string GetPrefix(ConsoleOutputChannel channel)
    {
        return channel switch
        {
            ConsoleOutputChannel.System => "[SYS]",
            ConsoleOutputChannel.Phase => "[PHASE]",
            ConsoleOutputChannel.View => "[VIEW]",
            ConsoleOutputChannel.Input => "[INPUT]",
            ConsoleOutputChannel.Battle => "[BATTLE]",
            ConsoleOutputChannel.Sync => "[SYNC]",
            ConsoleOutputChannel.Config => "[CONFIG]",
            ConsoleOutputChannel.Debug => "[DEBUG]",
            ConsoleOutputChannel.Warning => "[WARN]",
            ConsoleOutputChannel.Error => "[ERROR]",
            ConsoleOutputChannel.Trace => "[TRACE]",
            _ => "[??]",
        };
    }
}
