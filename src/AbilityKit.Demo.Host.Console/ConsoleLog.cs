namespace AbilityKit.Demo.Host.Console;

/// <summary>
/// 面向 Demo Console 宿主的基于通道的日志器。
/// </summary>
public sealed class ConsoleLog
{
    private readonly List<IConsoleLogSink> _sinks = new();
    private readonly object _lock = new();
    private IConsoleOutput _output;

    /// <summary>
    /// 初始化 <see cref="ConsoleLog"/> 类的新实例。
    /// </summary>
    /// <param name="output">主输出目标。</param>
    public ConsoleLog(IConsoleOutput output)
    {
        _output = output ?? throw new ArgumentNullException(nameof(output));
    }

    /// <summary>
    /// 获取主输出目标。
    /// </summary>
    public IConsoleOutput Output => _output;

    /// <summary>
    /// 替换主输出目标。
    /// </summary>
    /// <param name="output">新的输出目标。</param>
    public void SetOutput(IConsoleOutput output)
    {
        _output = output ?? throw new ArgumentNullException(nameof(output));
    }

    /// <summary>
    /// 添加日志接收器。
    /// </summary>
    /// <param name="sink">要添加的接收器。</param>
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
    /// 移除日志接收器。
    /// </summary>
    /// <param name="sink">要移除的接收器。</param>
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
    /// 写入一条日志消息。
    /// </summary>
    /// <param name="channel">输出通道。</param>
    /// <param name="message">消息文本。</param>
    public void Write(ConsoleOutputChannel channel, string message)
    {
        _output.Write(channel, message);
        PublishToSinks(channel, message);
    }

    /// <summary>
    /// 写入一条格式化日志消息。
    /// </summary>
    /// <param name="channel">输出通道。</param>
    /// <param name="format">复合格式字符串。</param>
    /// <param name="args">格式参数。</param>
    public void WriteFormat(ConsoleOutputChannel channel, string format, params object[] args)
    {
        Write(channel, string.Format(format, args));
    }

    /// <summary>写入系统消息。</summary>
    public void System(string message) => Write(ConsoleOutputChannel.System, message);

    /// <summary>写入战斗消息。</summary>
    public void Battle(string message) => Write(ConsoleOutputChannel.Battle, message);

    /// <summary>写入视图消息。</summary>
    public void View(string message) => Write(ConsoleOutputChannel.View, message);

    /// <summary>写入输入消息。</summary>
    public void Input(string message) => Write(ConsoleOutputChannel.Input, message);

    /// <summary>写入警告消息。</summary>
    public void Warn(string message) => Write(ConsoleOutputChannel.Warning, message);

    /// <summary>写入错误消息。</summary>
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
