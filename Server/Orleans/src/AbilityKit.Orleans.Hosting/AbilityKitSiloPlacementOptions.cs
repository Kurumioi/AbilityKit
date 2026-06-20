namespace AbilityKit.Orleans.Hosting;

public sealed record AbilityKitSiloPlacementOptions
{
    public string Role { get; init; } = "Shared";

    public string[] LogicalGroups { get; init; } = [];

    public string[] PreferredAffinity { get; init; } = [];

    public bool IsExclusive { get; init; }

    public bool IsGateway { get; init; }

    public string? Notes { get; init; }
}
