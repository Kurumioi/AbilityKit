namespace AbilityKit.Orleans.Hosting;

public sealed record AbilityKitStorageOptions
{
    public string Provider { get; init; } = "None";

    public string SessionStateProvider { get; init; } = "InMemory";

    public string RoomStateProvider { get; init; } = "InMemory";

    public string? ConnectionStringName { get; init; }

    public string? ConnectionString { get; init; }

    public bool Required { get; init; }
}
