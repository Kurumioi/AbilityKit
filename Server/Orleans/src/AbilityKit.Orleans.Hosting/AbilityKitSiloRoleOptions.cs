namespace AbilityKit.Orleans.Hosting;

public sealed record AbilityKitSiloRoleOptions
{
    public string Role { get; init; } = "Shared";

    public bool IsGateway { get; init; }

    public bool IsExclusive { get; init; }

    public string[] LogicalGroups { get; init; } = [];
}
