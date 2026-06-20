namespace AbilityKit.Orleans.Hosting;

public sealed record AbilityKitServerRuntimeOptions
{
    public bool PreserveWorkingDirectory { get; init; }

    public string? WorkingDirectory { get; init; }

    public int RestartGracePeriodSeconds { get; init; } = 5;
}
