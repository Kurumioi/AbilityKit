using AbilityKit.Demo.Shooter.Runtime;
using AbilityKit.Protocol.Shooter;
using Xunit;

namespace AbilityKit.Demo.Shooter.Runtime.Tests.Application.Runtime;

public sealed class ShooterEntityLimitOptionsTests
{
    [Fact]
    public void DefaultLimitAllowsTenThousandEntities()
    {
        var limits = ShooterEntityLimitOptions.Default;

        Assert.Equal(10000, limits.MaxEntityCount);
        Assert.Equal(10000, limits.ClampRequestedCount(12000));
    }

    [Fact]
    public void CustomLimitCapsStartPlayers()
    {
        var runtime = new ShooterBattleRuntimePort(new ShooterEntityLimitOptions(2));
        var start = new ShooterStartGamePayload(
            "entity-limit",
            30,
            1234,
            new[]
            {
                new ShooterStartPlayer(1, "P1", 0f, 0f),
                new ShooterStartPlayer(2, "P2", 1f, 0f),
                new ShooterStartPlayer(3, "P3", 2f, 0f)
            });

        Assert.True(runtime.StartGame(in start));

        var snapshot = runtime.GetSnapshot();
        var packed = runtime.ExportPackedSnapshot(77ul, isFullSnapshot: true, authorityOverride: true);
        Assert.Equal(2, snapshot.Players.Length);
        Assert.Equal(2, packed.EntityCount);
        Assert.True(runtime.TryGetPlayer(1, out _));
        Assert.True(runtime.TryGetPlayer(2, out _));
        Assert.False(runtime.TryGetPlayer(3, out _));
    }

    [Fact]
    public void InvalidLimitFallsBackToDefault()
    {
        var limits = new ShooterEntityLimitOptions(0);

        Assert.Equal(ShooterEntityLimitOptions.DefaultMaxEntityCount, limits.MaxEntityCount);
    }
}
