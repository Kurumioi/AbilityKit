using System.Linq;
using AbilityKit.Orleans.Hosting;
using Xunit;

namespace AbilityKit.Orleans.Gateway.Tests;

public sealed class AbilityKitDeploymentOptionsTests : GatewayTestBase
{
    [Fact]
    public void Defaults_should_match_shared_deployment_model()
    {
        var options = new AbilityKitDeploymentOptions();

        Assert.Equal("Shared", options.Role);
        Assert.Empty(options.Groups);
        Assert.Empty(options.Affinity);
        Assert.Equal(1, options.TargetSiloCount);
        Assert.Equal(0, options.MaxRoomsPerSilo);
        Assert.Equal(0, options.MaxBattlesPerSilo);
        Assert.Equal(0, options.MaxSessionsPerGateway);
    }

    [Fact]
    public void Deployment_mode_defaults_should_prefer_shared()
    {
        var options = new AbilityKitDeploymentModeOptions();

        Assert.Equal("Shared", options.Mode);
        Assert.True(options.PreferSharedDuringDevelopment);
        Assert.False(options.ForceDedicatedRolesInProduction);
        Assert.Equal(new[] { "Shared" }, options.EnabledRoles);
    }

    [Fact]
    public void Runtime_profile_defaults_should_be_shared_friendly()
    {
        var options = new AbilityKitSiloRuntimeProfileOptions();

        Assert.Equal("Shared", options.Role);
        Assert.Empty(options.LogicalGroups);
        Assert.Empty(options.PreferredAffinity);
        Assert.False(options.IsExclusive);
        Assert.False(options.IsGateway);
        Assert.Equal(0, options.MaxRooms);
        Assert.Equal(0, options.MaxBattles);
        Assert.Equal(0, options.MaxSessions);
        Assert.Null(options.Notes);
    }

    [Fact]
    public void Placement_defaults_should_keep_gateway_optional()
    {
        var options = new AbilityKitSiloPlacementOptions();

        Assert.Equal("Shared", options.Role);
        Assert.Empty(options.LogicalGroups);
        Assert.Empty(options.PreferredAffinity);
        Assert.False(options.IsExclusive);
        Assert.False(options.IsGateway);
        Assert.Null(options.Notes);
    }
}
