using AbilityKit.Demo.Shooter.View;
using Xunit;

namespace AbilityKit.Demo.Shooter.Runtime.Tests.Client;

public sealed class ShooterTimeAnchorCoordinatorTests
{
    [Fact]
    public void AdvanceLocalUsesTickRateAsElapsedSeconds()
    {
        var coordinator = ShooterTimeAnchorCoordinator.CreateLocal(30);

        var first = coordinator.AdvanceLocal();
        var second = coordinator.AdvanceLocal();

        Assert.Equal(0, first.LocalFrame);
        Assert.Equal(0L, first.TimelineTicks);
        Assert.Equal(0d, first.ElapsedSeconds);
        Assert.Equal(1, second.LocalFrame);
        Assert.Equal(1L, second.TimelineTicks);
        Assert.Equal(1d / 30d, second.ElapsedSeconds, precision: 6);
        Assert.Equal(second, coordinator.LastLocalAnchor);
    }

    [Fact]
    public void ResetRestartsLocalAnchorTimeline()
    {
        var coordinator = ShooterTimeAnchorCoordinator.CreateLocal(30);
        coordinator.AdvanceLocal();
        coordinator.AdvanceLocal();

        coordinator.Reset(60);
        var anchor = coordinator.AdvanceLocal();

        Assert.Equal(0, anchor.LocalFrame);
        Assert.Equal(0L, anchor.TimelineTicks);
        Assert.Equal(0d, anchor.ElapsedSeconds);
        Assert.Equal(anchor, coordinator.LastLocalAnchor);
    }

    [Fact]
    public void ProjectRemoteCreatesAuthoritativeServerStampedAnchor()
    {
        var worldStartAnchor = new ShooterGatewayWorldStartAnchor(200000L, 10000000L, 30, 1d / 30d);

        var projection = ShooterTimeAnchorCoordinator.ProjectRemote(in worldStartAnchor, 1200000L);

        Assert.True(projection.AnchorValid);
        Assert.Equal(1200000L, projection.ServerNowTicks);
        Assert.True(projection.ServerNowTicks > 0);
        Assert.Equal(33, projection.TargetFrame);
        Assert.True(projection.TargetFrame >= 0);
        Assert.Equal(3, projection.CatchUpFrames);
        Assert.True(projection.CatchUpFrames >= 0);
        Assert.Equal(0.1d, projection.ElapsedSeconds, precision: 6);
        Assert.True(projection.ElapsedSeconds >= 0d);
        Assert.Equal(33, projection.TimeAnchor.LocalFrame);
        Assert.Equal(3L, projection.TimeAnchor.TimelineTicks);
        Assert.True(projection.TimeAnchor.HasAuthoritativeFrame);
        Assert.Equal(33, projection.TimeAnchor.AuthoritativeFrame);
        Assert.True(projection.TimeAnchor.HasServerTicks);
        Assert.Equal(1200000L, projection.TimeAnchor.ServerTicks);
    }

    [Fact]
    public void ProjectRemoteReturnsDefaultForInvalidAnchor()
    {
        var projection = ShooterTimeAnchorCoordinator.ProjectRemote(default, 1200000L);

        Assert.False(projection.AnchorValid);
        Assert.Equal(0L, projection.ServerNowTicks);
        Assert.Equal(0, projection.TargetFrame);
        Assert.Equal(0, projection.CatchUpFrames);
        Assert.Equal(0d, projection.ElapsedSeconds);
        Assert.False(projection.TimeAnchor.HasServerTicks);
    }

    [Fact]
    public void ProjectRemoteReturnsDefaultForInvalidServerTicks()
    {
        var worldStartAnchor = new ShooterGatewayWorldStartAnchor(200000L, 10000000L, 30, 1d / 30d);

        var projection = ShooterTimeAnchorCoordinator.ProjectRemote(in worldStartAnchor, 0L);

        Assert.False(projection.AnchorValid);
        Assert.Equal(0L, projection.ServerNowTicks);
        Assert.Equal(0, projection.TargetFrame);
        Assert.Equal(0, projection.CatchUpFrames);
        Assert.False(projection.TimeAnchor.HasServerTicks);
    }
}
