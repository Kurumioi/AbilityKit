using AbilityKit.Network.Runtime.Sync;
using Xunit;

namespace AbilityKit.Network.Runtime.Tests;

public sealed class ServerClockEstimatorTests
{
    // 10_000_000 ticks per second mirrors DateTime ticks, a realistic server tick frequency.
    private const long TicksPerSecond = 10_000_000L;

    [Fact]
    public void NoSample_LeavesLocalTicksUnchangedAndAnchorUnstamped()
    {
        var estimator = new ServerClockEstimator(TicksPerSecond);

        Assert.False(estimator.HasSample);
        Assert.Equal(0, estimator.SampleCount);
        Assert.Equal(12345L, estimator.ToServerTicks(12345L));

        var anchor = SyncTimeAnchor.FromLocalFrame(3, 300L, 0.1d);
        var stamped = estimator.StampServerTicks(anchor, localTicks: 999L);

        Assert.False(stamped.HasServerTicks);
        Assert.Equal(anchor, stamped);
    }

    [Fact]
    public void ObserveRoundTrip_EstimatesOffsetFromSymmetricMidpoint()
    {
        var estimator = new ServerClockEstimator(TicksPerSecond);

        // Client sends at 1000, receives at 1200 -> RTT 200, midpoint local time 1100.
        // Server stamped at 5100 -> offset = 5100 - 1100 = 4000.
        bool adopted = estimator.ObserveRoundTrip(clientSendTicks: 1000L, serverReceiveTicks: 5100L, clientReceiveTicks: 1200L);

        Assert.True(adopted);
        Assert.True(estimator.HasSample);
        Assert.Equal(200L, estimator.BestRoundTripTicks);
        Assert.Equal(4000L, estimator.OffsetTicks);
        // Local 2000 -> server 2000 + 4000 = 6000.
        Assert.Equal(6000L, estimator.ToServerTicks(2000L));
    }

    [Fact]
    public void ObserveRoundTrip_KeepsLowestRttSampleAndIgnoresWorseOnes()
    {
        var estimator = new ServerClockEstimator(TicksPerSecond);

        Assert.True(estimator.ObserveRoundTrip(1000L, 5100L, 1200L)); // RTT 200, offset 4000
        // Worse RTT 600 (midpoint 1300, offset 4700) must NOT replace the best estimate.
        Assert.False(estimator.ObserveRoundTrip(1000L, 6000L, 1600L));

        Assert.Equal(2, estimator.SampleCount);
        Assert.Equal(200L, estimator.BestRoundTripTicks);
        Assert.Equal(4000L, estimator.OffsetTicks);

        // A better RTT 100 (midpoint 1050, offset 3950) replaces it.
        Assert.True(estimator.ObserveRoundTrip(1000L, 5000L, 1100L));
        Assert.Equal(3, estimator.SampleCount);
        Assert.Equal(100L, estimator.BestRoundTripTicks);
        Assert.Equal(3950L, estimator.OffsetTicks);
    }

    [Fact]
    public void ObserveRoundTrip_RejectsNegativeRoundTrip()
    {
        var estimator = new ServerClockEstimator(TicksPerSecond);

        bool adopted = estimator.ObserveRoundTrip(clientSendTicks: 2000L, serverReceiveTicks: 5000L, clientReceiveTicks: 1000L);

        Assert.False(adopted);
        Assert.False(estimator.HasSample);
        Assert.Equal(0, estimator.SampleCount);
    }

    [Fact]
    public void OffsetAndRttSeconds_DeriveFromTickFrequency()
    {
        var estimator = new ServerClockEstimator(TicksPerSecond);

        // RTT 0.02s (200000 ticks), offset 0.4s (4000000 ticks).
        estimator.ObserveRoundTrip(clientSendTicks: 0L, serverReceiveTicks: 4_100_000L, clientReceiveTicks: 200_000L);

        Assert.Equal(200_000L, estimator.BestRoundTripTicks);
        Assert.Equal(0.02d, estimator.BestRoundTripSeconds, precision: 6);
        Assert.Equal(4_000_000L, estimator.OffsetTicks);
        Assert.Equal(0.4d, estimator.OffsetSeconds, precision: 6);
    }

    [Fact]
    public void NonPositiveFrequency_FallsBackToOneToAvoidDivideByZero()
    {
        var estimator = new ServerClockEstimator(serverTickFrequency: 0L);

        Assert.Equal(1L, estimator.ServerTickFrequency);

        estimator.ObserveRoundTrip(0L, 50L, 10L); // RTT 10, midpoint 5, offset 45
        Assert.Equal(10L, estimator.BestRoundTripTicks);
        Assert.Equal(10d, estimator.BestRoundTripSeconds, precision: 6);
        Assert.Equal(45L, estimator.OffsetTicks);
    }

    [Fact]
    public void StampServerTicks_AppliesEstimatedOffsetToAnchor()
    {
        var estimator = new ServerClockEstimator(TicksPerSecond);
        estimator.ObserveRoundTrip(1000L, 5100L, 1200L); // offset 4000

        var anchor = SyncTimeAnchor.FromLocalFrame(7, 700L, 0.25d);
        var stamped = estimator.StampServerTicks(anchor, localTicks: 2000L);

        Assert.True(stamped.HasServerTicks);
        Assert.Equal(6000L, stamped.ServerTicks);
        // Other coordinates are preserved.
        Assert.Equal(7, stamped.LocalFrame);
        Assert.Equal(700L, stamped.TimelineTicks);
        Assert.Equal(0.25d, stamped.ElapsedSeconds, precision: 6);
    }

    [Fact]
    public void Reset_ClearsSamplesBackToInitialState()
    {
        var estimator = new ServerClockEstimator(TicksPerSecond);
        estimator.ObserveRoundTrip(1000L, 5100L, 1200L);
        Assert.True(estimator.HasSample);

        estimator.Reset();

        Assert.False(estimator.HasSample);
        Assert.Equal(0, estimator.SampleCount);
        Assert.Equal(0L, estimator.OffsetTicks);
        Assert.Equal(0L, estimator.BestRoundTripTicks);
        Assert.Equal(5555L, estimator.ToServerTicks(5555L));
    }
}
