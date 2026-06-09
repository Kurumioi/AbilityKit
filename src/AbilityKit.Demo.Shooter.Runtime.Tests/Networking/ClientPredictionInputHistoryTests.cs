using System.Collections.Generic;
using AbilityKit.Ability.Host.Extensions.Client.FrameSync;
using Xunit;

namespace AbilityKit.Demo.Shooter.Runtime.Tests.Networking;

public sealed class ClientPredictionInputHistoryTests
{
    [Fact]
    public void RecordMergesInputsOnSameFrameAndReplaysUntilTargetFrame()
    {
        var history = new ClientPredictionInputHistory<int>();
        history.Record(0, new[] { 1 });
        history.Record(0, new[] { 2, 3 });
        history.Record(2, new[] { 4 });
        var currentFrame = 0;
        var submitted = new List<int>();

        var result = history.ReplayTo(
            targetFrame: 3,
            getCurrentFrame: () => currentFrame,
            submit: (frame, inputs) =>
            {
                submitted.Add(frame * 10 + inputs.Length);
                return inputs.Length;
            },
            stepFrame: () =>
            {
                currentFrame++;
                return true;
            });

        Assert.True(result.Completed);
        Assert.Equal(3, result.ReplayTicks);
        Assert.Equal(3, result.FinalFrame);
        Assert.Equal(new[] { 3, 21 }, submitted);
    }

    [Fact]
    public void TrimBeforeRemovesConfirmedInputFrames()
    {
        var history = new ClientPredictionInputHistory<int>();
        history.Record(1, new[] { 1 });
        history.Record(2, new[] { 2 });
        history.Record(3, new[] { 3 });

        history.TrimBefore(3);

        Assert.Equal(1, history.Count);
        Assert.Equal(1, history.SubmitFrame(3, (_, inputs) => inputs.Length));
        Assert.Equal(0, history.SubmitFrame(2, (_, inputs) => inputs.Length));
    }
}
