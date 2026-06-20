using System.Collections.Generic;
using System.Linq;
using AbilityKit.Orleans.Hosting;
using Xunit;

namespace AbilityKit.Orleans.Gateway.Tests;

public sealed class AbilityKitGrainRouteRegistryTests
{
    [Fact]
    public void Registry_should_include_session_room_and_battle_routes()
    {
        var definitions = AbilityKitGrainRouteRegistry.Definitions;

        Assert.Contains(definitions, definition => definition.GrainType == "SessionGrain" && definition.RouteGroup == AbilityKitGrainRouteGroups.Session);
        Assert.Contains(definitions, definition => definition.GrainType == "RoomGrain" && definition.RouteGroup == AbilityKitGrainRouteGroups.Room);
        Assert.Contains(definitions, definition => definition.GrainType == "BattleGrain" && definition.RouteGroup == AbilityKitGrainRouteGroups.Battle);
    }

    [Fact]
    public void Session_route_should_require_exclusive_silo_and_session_group()
    {
        var definition = AbilityKitGrainRouteRegistry.Definitions.Single(item => item.GrainType == "SessionGrain");

        Assert.True(definition.RequireExclusiveSilo);
        Assert.Equal(new[] { "Session" }, definition.PreferredSiloRoles);
        Assert.Equal(new[] { "session" }, definition.RequiredLogicalGroups);
    }

    [Fact]
    public void Dedicated_grain_routes_should_be_grouped_by_domain()
    {
        var groups = AbilityKitGrainRouteRegistry.Definitions.Select(definition => definition.RouteGroup).ToArray();

        Assert.Contains(AbilityKitGrainRouteGroups.Session, groups);
        Assert.Contains(AbilityKitGrainRouteGroups.Room, groups);
        Assert.Contains(AbilityKitGrainRouteGroups.Battle, groups);
    }
}
