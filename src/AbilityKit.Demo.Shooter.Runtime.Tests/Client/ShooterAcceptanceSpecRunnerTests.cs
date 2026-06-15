using System.Linq;
using AbilityKit.Demo.Shooter.View;
using AbilityKit.Protocol.Shooter;
using Xunit;

namespace AbilityKit.Demo.Shooter.Runtime.Tests;

public sealed class ShooterAcceptanceSpecRunnerTests
{
    [Fact]
    public void BasicCombatSpecProducesStablePureCSharpResult()
    {
        var runner = new ShooterAcceptanceSpecRunner();

        var result = runner.Run(ShooterAcceptanceSpecs.BasicCombat);

        Assert.Equal("basic-combat", result.SpecId);
        Assert.Equal(6, result.Frame);
        Assert.Equal(result.Frame, result.Snapshot.Frame);
        Assert.Equal(2, result.Snapshot.Players.Length);
        Assert.Empty(result.Snapshot.Bullets);
        Assert.Equal(2, result.PackedSnapshot.EntityCount);
        Assert.Equal(result.StateHash, result.PackedSnapshot.StateHash);

        var p1 = Assert.Single(result.Snapshot.Players, p => p.PlayerId == 1);
        var p2 = Assert.Single(result.Snapshot.Players, p => p.PlayerId == 2);
        Assert.Equal(0.8333333f, p1.X, precision: 5);
        Assert.Equal(0f, p1.Y);
        Assert.Equal(1, p1.Score);
        Assert.Equal(ShooterGameplay.DefaultPlayerHp, p1.Hp);
        Assert.True(p1.Alive);
        Assert.Equal(-0.0666667f, p2.X, precision: 5);
        Assert.Equal(0f, p2.Y);
        Assert.Equal(0, p2.Score);
        Assert.Equal(ShooterGameplay.DefaultPlayerHp - 1, p2.Hp);
        Assert.True(p2.Alive);

        Assert.Empty(result.Snapshot.Events);
        Assert.Equal(2, result.Events.Count);
        Assert.Equal((int)ShooterEventType.Fire, result.Events[0].EventType);
        Assert.Equal((int)ShooterEventType.Hit, result.Events[1].EventType);
        Assert.Equal(1, result.Events[1].SourcePlayerId);
        Assert.Equal(2, result.Events[1].TargetPlayerId);
        Assert.Equal(1, result.Events[1].Value);
    }

    [Fact]
    public void BasicCombatSpecIsReproducibleAcrossRuns()
    {
        var runner = new ShooterAcceptanceSpecRunner();

        var first = runner.Run(ShooterAcceptanceSpecs.BasicCombat);
        var second = runner.Run(ShooterAcceptanceSpecs.BasicCombat);

        Assert.Equal(first.Frame, second.Frame);
        Assert.Equal(first.StateHash, second.StateHash);
        Assert.Equal(first.PackedSnapshot.StateHash, second.PackedSnapshot.StateHash);
        Assert.Equal(first.Snapshot.Players.Select(p => (p.PlayerId, p.X, p.Y, p.Hp, p.Score, p.Alive)),
            second.Snapshot.Players.Select(p => (p.PlayerId, p.X, p.Y, p.Hp, p.Score, p.Alive)));
        Assert.Equal(first.Events.Select(e => (e.EventType, e.SourcePlayerId, e.TargetPlayerId, e.BulletId, e.Value)),
            second.Events.Select(e => (e.EventType, e.SourcePlayerId, e.TargetPlayerId, e.BulletId, e.Value)));
    }
}
