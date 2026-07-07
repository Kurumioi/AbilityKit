namespace AbilityKit.Demo.Host.Console;

/// <summary>
/// 平台无关的 Console 输出抽象。
/// </summary>
public interface IConsoleOutput
{
    /// <summary>
    /// 向逻辑输出通道写入一行。
    /// </summary>
    /// <param name="channel">输出通道。</param>
    /// <param name="message">消息文本。</param>
    void Write(ConsoleOutputChannel channel, string message);

    /// <summary>
    /// 向逻辑输出通道写入格式化文本。
    /// </summary>
    /// <param name="channel">输出通道。</param>
    /// <param name="format">复合格式字符串。</param>
    /// <param name="args">格式参数。</param>
    void WriteFormat(ConsoleOutputChannel channel, string format, params object[] args);

    /// <summary>
    /// 在支持时清空输出表面。
    /// </summary>
    void Clear();

    /// <summary>
    /// 写入分隔线。
    /// </summary>
    /// <param name="channel">输出通道。</param>
    /// <param name="character">分隔字符。</param>
    /// <param name="length">分隔线长度。</param>
    void WriteSeparator(ConsoleOutputChannel channel = ConsoleOutputChannel.System, char character = '=', int length = 60);

    /// <summary>
    /// 写入带边框的标题。
    /// </summary>
    /// <param name="channel">输出通道。</param>
    /// <param name="title">标题文本。</param>
    /// <param name="borderCharacter">边框字符。</param>
    /// <param name="width">标题宽度。</param>
    void WriteTitle(ConsoleOutputChannel channel, string title, char borderCharacter = '=', int width = 60);
}
