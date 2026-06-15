using AbilityKit.Network.Runtime.Sync;
using Xunit;

namespace AbilityKit.Network.Runtime.Tests;

public sealed class SyncClockTests
{
    private const long TicksPerSecond = 10_000_000L;

    [Fact]
    public void Advance_ProducesSequentialFramesAndElapsedTime()
    {
        var clock = new SyncClock(deltaSeconds: 0.5d);

        var first = clock.Advance();
        var second = clock.Advance();

        Assert.Equal(0, first.LocalFrame);
        Assert.Equal(0d, first.ElapsedSeconds);
        Assert.Equal(1, second.LocalFrame);
        Assert.Equal(0.5d, second.ElapsedSeconds);
        Assert.Equal(2, clock.LocalFrame);
    }

    [Fact]
    public void AnchorFor_DoesNotMutateLocalFrame()
    {
        var clock = new SyncClock(deltaSeconds: 0.1d, timelineTicksPerStep: 100L);

        var anchor = clock.AnchorFor(5);

        Assert.Equal(5, anchor.LocalFrame);
        Assert.Equal(500L, anchor.TimelineTicks);
        Assert.Equal(0, clock.LocalFrame);
    }

    [Fact]
    public void Advance_WithoutServerSample_LeavesAnchorUnstamped()
    {
        var estimator = new ServerClockEstimator(TicksPerSecond);
        var clock = new SyncClock(deltaSeconds: 0.1d, timelineTicksPerStep: TicksPerSecond, serverClock: estimator);

        var anchor = clock.Advance();

        Assert.False(anchor.HasServerTicks);
    }

    [Fact]
    public void Advance_WithServerSample_StampsServerTicks()
    {
        var estimator = new ServerClockEstimator(TicksPerSecond);
        // RTT = 200, midpoint = 1100, server offset = 5000 - 1100 = 3900 ticks.
        estimator.ObserveRoundTrip(clientSendTicks: 1000L, serverReceiveTicks: 5000L, clientReceiveTicks: 1200L);

        var clock = new SyncClock(deltaSeconds: 0.1d, timelineTicksPerStep: TicksPerSecond, serverClock: estimator);

        var anchor = clock.AnchorFor(2);

        Assert.True(anchor.HasServerTicks);
        // timelineTicks = 2 * TicksPerSecond = 20_000_000; + offset 3900.
        Assert.Equal(2L * TicksPerSecond + 3900L, anchor.ServerTicks);
    }

    [Fact]
    public void Reset_RewindsLocalFrameToZero()
    {
        var clock = new SyncClock(deltaSeconds: 0.1d);
        clock.Advance();
        clock.Advance();

        clock.Reset();

        Assert.Equal(0, clock.LocalFrame);
        Assert.Equal(0, clock.Advance().LocalFrame);
    }
}
