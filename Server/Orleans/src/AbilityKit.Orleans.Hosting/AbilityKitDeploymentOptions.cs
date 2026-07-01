namespace AbilityKit.Orleans.Hosting;

public sealed record AbilityKitDeploymentOptions
{
    public string Role { get; init; } = "Shared";

    public string[] Groups { get; init; } = [];

    public string[] Affinity { get; init; } = [];

    public int TargetSiloCount { get; init; } = 1;

    public int MaxRoomsPerSilo { get; init; } = 0;

    public int MaxBattlesPerSilo { get; init; } = 0;

    public int MaxSessionsPerGateway { get; init; } = 0;
}
