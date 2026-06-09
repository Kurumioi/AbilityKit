using AbilityKit.Ability.Host.Extensions.Server.BattleHost;
using Xunit;

namespace AbilityKit.Demo.Shooter.Runtime.Tests.Networking;

public sealed class BattleInputFrameSchedulerTests
{
    [Fact]
    public void ScheduleRemapsLateInputToCurrentFramePlusInputDelay()
    {
        var result = BattleInputFrameScheduler.Schedule(
            requestedFrame: 8,
            currentFrame: 12,
            inputDelayFrames: 2,
            BattleInputFrameSchedulerOptions.Default);

        Assert.True(result.Accepted);
        Assert.Equal(8, result.RequestedFrame);
        Assert.Equal(14, result.AcceptedFrame);
        Assert.Equal(12, result.CurrentFrame);
        Assert.Equal(2, result.InputDelayFrames);
        Assert.Equal(BattleInputAcceptStatus.RemappedLate, result.Status);
    }

    [Fact]
    public void ScheduleRemapsEarlierThanInputDelayToEarliestAcceptedFrame()
    {
        var result = BattleInputFrameScheduler.Schedule(
            requestedFrame: 13,
            currentFrame: 12,
            inputDelayFrames: 3,
            BattleInputFrameSchedulerOptions.Default);

        Assert.True(result.Accepted);
        Assert.Equal(15, result.AcceptedFrame);
        Assert.Equal(BattleInputAcceptStatus.RemappedTooEarly, result.Status);
    }

    [Fact]
    public void ScheduleRejectsInputThatIsTooFarAhead()
    {
        var options = new BattleInputFrameSchedulerOptions(
            remapLateInputs: true,
            remapTooEarlyInputs: true,
            maxFutureLeadFrames: 5);

        var result = BattleInputFrameScheduler.Schedule(
            requestedFrame: 18,
            currentFrame: 12,
            inputDelayFrames: 0,
            options);

        Assert.False(result.Accepted);
        Assert.Equal(18, result.AcceptedFrame);
        Assert.Equal(BattleInputAcceptStatus.RejectedTooFarFuture, result.Status);
    }
}
