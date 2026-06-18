namespace AbilityKit.Orleans.Hosting;

public sealed record AbilityKitStorageOptions
{
    public string Provider { get; init; } = "None";

    public string? ConnectionStringName { get; init; }

    public string? ConnectionString { get; init; }

    public bool Required { get; init; }
}
