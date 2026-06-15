namespace AbilityKit.Demo.Host.Console;

/// <summary>
/// Describes a fixed-rate console host loop.
/// </summary>
public readonly struct ConsoleHostOptions
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ConsoleHostOptions"/> struct.
    /// </summary>
    /// <param name="targetFrameRate">Target frame rate for the loop.</param>
    public ConsoleHostOptions(int targetFrameRate)
    {
        TargetFrameRate = targetFrameRate > 0 ? targetFrameRate : 30;
    }

    /// <summary>
    /// Gets default host options.
    /// </summary>
    public static ConsoleHostOptions Default { get; } = new ConsoleHostOptions(30);

    /// <summary>
    /// Gets the target frame rate.
    /// </summary>
    public int TargetFrameRate { get; }

    /// <summary>
    /// Gets the fixed delta time in seconds.
    /// </summary>
    public float FixedDeltaSeconds => 1f / TargetFrameRate;

    /// <summary>
    /// Gets the target frame duration.
    /// </summary>
    public TimeSpan FrameDuration => TimeSpan.FromSeconds(1d / TargetFrameRate);
}
