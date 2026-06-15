using System;
using System.Collections.Generic;
using System.Linq;
using AbilityKit.Network.Runtime;
using AbilityKit.Network.Runtime.Sync;
using Xunit;

namespace AbilityKit.Network.Runtime.Tests;

public sealed class NetworkSyncProfileRegistryTests
{
    [Fact]
    public void Registry_CoversEveryKnownCompatibilityModel()
    {
        var registered = NetworkSyncProfileRegistry.Models().ToArray();
        var declared = Enum.GetValues<NetworkSyncModel>();

        Assert.Equal(declared.Length, NetworkSyncProfileRegistry.Count);
        Assert.Equal(declared.OrderBy(m => m), registered.OrderBy(m => m));
    }

    [Fact]
    public void Models_AreEnumeratedInEnumOrder()
    {
        var registered = NetworkSyncProfileRegistry.Models().ToArray();
        var ordered = registered.OrderBy(m => (int)m).ToArray();

        Assert.Equal(ordered, registered);
    }

    [Fact]
    public void Resolve_MapsEveryModelToProfileWithSameCompatibilityModel()
    {
        foreach (var model in Enum.GetValues<NetworkSyncModel>())
        {
            var profile = NetworkSyncProfileRegistry.Resolve(model);

            Assert.Equal(model, profile.CompatibilityModel);
        }
    }

    [Fact]
    public void Resolve_AgreesWithLegacyFromCompatibilityModel()
    {
        foreach (var model in Enum.GetValues<NetworkSyncModel>())
        {
            Assert.Equal(NetworkSyncProfiles.FromCompatibilityModel(model), NetworkSyncProfileRegistry.Resolve(model));
        }
    }

    [Fact]
    public void Resolve_ThrowsForUnknownModel()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            NetworkSyncProfileRegistry.Resolve((NetworkSyncModel)999));
    }

    [Fact]
    public void TryResolve_ReturnsTrueAndProfileForKnownModel()
    {
        var resolved = NetworkSyncProfileRegistry.TryResolve(NetworkSyncModel.FastReconnect, out var profile);

        Assert.True(resolved);
        Assert.Equal(NetworkSyncProfiles.FastReconnect, profile);
    }

    [Fact]
    public void TryResolve_ReturnsFalseAndUnspecifiedForUnknownModel()
    {
        var resolved = NetworkSyncProfileRegistry.TryResolve((NetworkSyncModel)999, out var profile);

        Assert.False(resolved);
        Assert.Equal(NetworkSyncProfiles.Unspecified, profile);
    }

    [Fact]
    public void GetName_MatchesEnumMemberName()
    {
        foreach (var model in Enum.GetValues<NetworkSyncModel>())
        {
            Assert.Equal(model.ToString(), NetworkSyncProfileRegistry.GetName(model));
        }
    }

    [Fact]
    public void GetName_ThrowsForUnknownModel()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            NetworkSyncProfileRegistry.GetName((NetworkSyncModel)999));
    }

    [Fact]
    public void Profiles_MatchResolveForEveryModel()
    {
        var models = NetworkSyncProfileRegistry.Models().ToArray();
        var profiles = NetworkSyncProfileRegistry.Profiles().ToArray();

        Assert.Equal(models.Length, profiles.Length);
        for (var i = 0; i < models.Length; i++)
        {
            Assert.Equal(NetworkSyncProfileRegistry.Resolve(models[i]), profiles[i]);
        }
    }

    [Fact]
    public void Profiles_HaveDistinctCompatibilityModels()
    {
        var compatibilityModels = NetworkSyncProfileRegistry.Profiles()
            .Select(p => p.CompatibilityModel)
            .ToArray();

        Assert.Equal(compatibilityModels.Length, compatibilityModels.Distinct().Count());
    }
}
