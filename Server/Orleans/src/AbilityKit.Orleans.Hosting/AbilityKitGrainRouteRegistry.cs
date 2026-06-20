namespace AbilityKit.Orleans.Hosting;

public static class AbilityKitGrainRouteRegistry
{
    public static readonly AbilityKitGrainRouteDefinition[] Definitions =
    [
        new AbilityKitGrainRouteDefinition
        {
            GrainType = "SessionGrain",
            RouteGroup = AbilityKitGrainRouteGroups.Session,
            PreferredSiloRoles = ["Session"],
            RequiredLogicalGroups = ["session"],
            RequireExclusiveSilo = true
        },
        new AbilityKitGrainRouteDefinition
        {
            GrainType = "RoomGrain",
            RouteGroup = AbilityKitGrainRouteGroups.Room,
            PreferredSiloRoles = ["Room"],
            RequiredLogicalGroups = ["room"],
            RequireExclusiveSilo = false
        },
        new AbilityKitGrainRouteDefinition
        {
            GrainType = "BattleGrain",
            RouteGroup = AbilityKitGrainRouteGroups.Battle,
            PreferredSiloRoles = ["Battle"],
            RequiredLogicalGroups = ["battle"],
            RequireExclusiveSilo = false
        }
    ];
}
