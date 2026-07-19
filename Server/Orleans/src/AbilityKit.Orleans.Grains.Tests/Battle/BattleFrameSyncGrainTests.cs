using AbilityKit.Orleans.Contracts.FrameSync;
using AbilityKit.Orleans.Grains.FrameSync;
using Xunit;

namespace AbilityKit.Orleans.Grains.Tests.Battle;

public sealed class BattleFrameSyncGrainTests
{
    [Theory]
    [InlineData(0ul, 10, 100ul, 10)]
    [InlineData(100ul, 10, 0ul, 10)]
    [InlineData(100ul, 10, 101ul, 10)]
    public void ValidateSubmission_rejects_world_mismatch(
        ulong authoritativeWorldId,
        int serverFrame,
        ulong requestedWorldId,
        int requestedFrame)
    {
        Assert.Equal(
            FrameInputSubmitReason.WorldMismatch,
            BattleFrameSyncGrain.ValidateSubmission(
                authoritativeWorldId,
                serverFrame,
                requestedWorldId,
                requestedFrame));
    }

    [Fact]
    public void ValidateSubmission_rejects_negative_frame()
    {
        Assert.Equal(
            FrameInputSubmitReason.NegativeFrame,
            BattleFrameSyncGrain.ValidateSubmission(100, 10, 100, -1));
    }

    [Fact]
    public void ValidateSubmission_rejects_processed_frame()
    {
        Assert.Equal(
            FrameInputSubmitReason.FrameAlreadyProcessed,
            BattleFrameSyncGrain.ValidateSubmission(100, 10, 100, 9));
    }

    [Fact]
    public void ValidateSubmission_rejects_frame_more_than_120_ahead()
    {
        Assert.Equal(
            FrameInputSubmitReason.FrameTooFarAhead,
            BattleFrameSyncGrain.ValidateSubmission(100, 10, 100, 131));
    }

    [Theory]
    [InlineData(10)]
    [InlineData(130)]
    public void ValidateSubmission_accepts_current_and_maximum_future_frame(int requestedFrame)
    {
        Assert.Equal(
            FrameInputSubmitReason.None,
            BattleFrameSyncGrain.ValidateSubmission(100, 10, 100, requestedFrame));
    }
}
