namespace AbilityKit.Orleans.Hosting;

public sealed record AbilityKitDeploymentOptions
{
    public string Role { get; init; } = "Shared";

    public string[] Groups { get; init; } = [];

    public string[] Affinity { get; init; } = [];
}
