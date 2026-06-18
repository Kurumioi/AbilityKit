namespace AbilityKit.Orleans.Hosting;

public sealed record AbilityKitLoggingOptions
{
    public string MinimumLevel { get; init; } = "Information";

    public bool IncludeScopes { get; init; } = true;

    public bool SingleLine { get; init; } = true;

    public string TimestampFormat { get; init; } = "yyyy-MM-dd HH:mm:ss.fff ";

    public string MicrosoftLevel { get; init; } = "Warning";

    public string HostingLifetimeLevel { get; init; } = "Information";

    public string OrleansLevel { get; init; } = "Information";

    public string ApplicationLevel { get; init; } = "Information";
}
