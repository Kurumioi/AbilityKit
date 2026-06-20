namespace AbilityKit.Orleans.Hosting;

public sealed record AbilityKitGrainRouteDefinition
{
    public string GrainType { get; init; } = string.Empty;

    public string RouteGroup { get; init; } = string.Empty;

    public string[] PreferredSiloRoles { get; init; } = [];

    public string[] RequiredLogicalGroups { get; init; } = [];

    public bool RequireExclusiveSilo { get; init; }
}
