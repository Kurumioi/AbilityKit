namespace AbilityKit.Demo.Host.Console;

/// <summary>
/// 描述固定频率的 Console 宿主循环。
/// </summary>
public readonly struct ConsoleHostOptions
{
    /// <summary>
    /// 初始化 <see cref="ConsoleHostOptions"/> 结构的新实例。
    /// </summary>
    /// <param name="targetFrameRate">循环目标帧率。</param>
    public ConsoleHostOptions(int targetFrameRate)
    {
        TargetFrameRate = targetFrameRate > 0 ? targetFrameRate : 30;
    }

    /// <summary>
    /// 获取默认宿主选项。
    /// </summary>
    public static ConsoleHostOptions Default { get; } = new ConsoleHostOptions(30);

    /// <summary>
    /// 获取目标帧率。
    /// </summary>
    public int TargetFrameRate { get; }

    /// <summary>
    /// 获取以秒为单位的固定增量时间。
    /// </summary>
    public float FixedDeltaSeconds => 1f / TargetFrameRate;

    /// <summary>
    /// 获取目标帧时长。
    /// </summary>
    public TimeSpan FrameDuration => TimeSpan.FromSeconds(1d / TargetFrameRate);
}
