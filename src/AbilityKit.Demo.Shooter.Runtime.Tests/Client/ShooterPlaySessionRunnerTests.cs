using System.Collections.Generic;
using AbilityKit.Demo.Shooter.Runtime;
using AbilityKit.Demo.Shooter.View;
using AbilityKit.Demo.Shooter.View.Hosting;
using AbilityKit.Demo.Shooter.View.PlayMode;
using AbilityKit.Network.Runtime;
using AbilityKit.Network.Runtime.Conditioning;
using Xunit;

namespace AbilityKit.Demo.Shooter.Runtime.Tests.Client;

public sealed class ShooterPlaySessionRunnerTests
{
    [Fact]
    public void StartNormalizesOptionsAndPublishesSessionChange()
    {
        var input = new ScriptedInputSource(new ShooterHostFrameInput(1f, 0f, 0f, 1f, false));
        var view = new RecordingViewSink();
        using var runner = new ShooterPlaySessionRunner(input, view);
        var changes = new List<bool>();
        runner.SessionChanged += session => changes.Add(session != null);

        var session = runner.Start(new ShooterPlayModeSessionOptions(
            NetworkSyncModel.PredictRollback,
            tickRate: 0,
            playerCount: 0,
            randomSeed: 7,
            controlledPlayerId: 8,
            enableAuthoritativeWorld: true,
            latencyMs: -10,
            jitterMs: -20,
            packetLossRate: 2f,
            reorderRate: -1f,
            bandwidthKbps: -64,
            worldScale: 0f,
            networkName: "runner-test"));

        Assert.Same(session, runner.Session);
        Assert.True(runner.IsRunning);
        Assert.Equal(1, runner.Options.PlayerCount);
        Assert.Equal(1, runner.Options.ControlledPlayerId);
        Assert.Equal(1f, runner.Options.WorldScale);
        Assert.Equal(0, runner.Options.LatencyMs);
        Assert.Equal(0, runner.Options.JitterMs);
        Assert.Equal(1f, runner.Options.PacketLossRate);
        Assert.Equal(0f, runner.Options.ReorderRate);
        Assert.Equal(0, runner.Options.BandwidthKbps);
        Assert.Equal(new[] { true }, changes);
        Assert.Equal(1, view.ClearCount);
    }

    [Fact]
    public void TickUsesFixedStepInputAndRendersLatestSnapshot()
    {
        var input = new ScriptedInputSource(
            new ShooterHostFrameInput(1f, 0f, 0f, 1f, false),
            new ShooterHostFrameInput(0f, 1f, 0f, 1f, true));
        var view = new RecordingViewSink();
        using var runner = new ShooterPlaySessionRunner(input, view);
        runner.Start(new ShooterPlayModeSessionOptions(
            NetworkSyncModel.PredictRollback,
            tickRate: 10,
            playerCount: 2,
            randomSeed: 13,
            controlledPlayerId: 2,
            enableAuthoritativeWorld: true,
            latencyMs: 0,
            jitterMs: 0,
            packetLossRate: 0f,
            reorderRate: 0f,
            bandwidthKbps: 0,
            worldScale: 2f,
            networkName: "runner-test"));

        runner.Tick(0.05f);
        Assert.Equal(0, input.ReadCount);
        Assert.Single(view.Frames);

        runner.Tick(0.15f);

        Assert.Equal(2, input.ReadCount);
        Assert.Equal(new[] { 2, 2 }, input.ControlledPlayerIds);
        Assert.Equal(2, runner.Session!.Runtime.CurrentFrame);
        Assert.Equal(2, view.Frames.Count);
        Assert.Equal(2, view.Frames[^1].ControlledPlayerId);
        Assert.Equal(2f, view.Frames[^1].WorldScale);
        Assert.NotEmpty(view.Frames[^1].ClientBatch.EntityChanges);
        Assert.NotEmpty(view.Frames[^1].ClientBatch.TransformChanges);
        var carrierStats = Assert.IsType<NetworkConditioningStats>(view.Frames[^1].CarrierNetworkStats);
        Assert.Equal(2, carrierStats.InboundReceived);
        Assert.Equal(2, carrierStats.InboundDelivered);
        Assert.Equal(ShooterSnapshotApplyResult.AppliedPackedSnapshot, view.Frames[^1].LastCarrierSnapshotApplyResult);
        Assert.Equal(2, view.Frames[^1].LastCarrierTimeAnchor.LocalFrame);
        Assert.True(view.Frames[^1].LastCarrierTimeAnchor.HasAuthoritativeFrame);
        Assert.Equal(2, view.Frames[^1].LastCarrierTimeAnchor.AuthoritativeFrame);
        var lagCompensationTelemetry = Assert.IsType<ShooterLagCompensationTelemetry>(view.Frames[^1].LagCompensationTelemetry);
        Assert.Equal(2, lagCompensationTelemetry.CapturedFrameCount);
        Assert.Equal(1, lagCompensationTelemetry.OldestFrame);
        Assert.Equal(2, lagCompensationTelemetry.LatestFrame);
    }

    [Fact]
    public void StopClearsViewAndPublishesNullSession()
    {
        var input = new ScriptedInputSource(new ShooterHostFrameInput(0f, 0f, 0f, 1f, false));
        var view = new RecordingViewSink();
        using var runner = new ShooterPlaySessionRunner(input, view);
        var changes = new List<bool>();
        runner.SessionChanged += session => changes.Add(session != null);
        runner.Start(ShooterPlayModeSessionOptions.Default);

        runner.Stop();

        Assert.False(runner.IsRunning);
        Assert.Null(runner.Session);
        Assert.Equal(2, view.ClearCount);
        Assert.Equal(new[] { true, false }, changes);
    }

    [Fact]
    public void ApplyNetworkCanRunBeforeAndAfterStart()
    {
        var input = new ScriptedInputSource(new ShooterHostFrameInput(0f, 0f, 0f, 1f, false));
        var view = new RecordingViewSink();
        using var runner = new ShooterPlaySessionRunner(input, view);

        runner.ApplyNetwork(NetworkConditionProfile.PoorWifi);
        runner.Start(ShooterPlayModeSessionOptions.Default);
        runner.ApplyNetwork(NetworkConditionProfile.Lan);
        runner.Tick(1f / runner.Options.TickRate);

        Assert.True(runner.IsRunning);
        Assert.Equal(1, input.ReadCount);
        Assert.NotEmpty(view.Frames);
    }

    private sealed class ScriptedInputSource : IShooterHostInputSource
    {
        private readonly Queue<ShooterHostFrameInput> _inputs;

        public ScriptedInputSource(params ShooterHostFrameInput[] inputs)
        {
            _inputs = new Queue<ShooterHostFrameInput>(inputs);
        }

        public int ReadCount { get; private set; }
        public List<int> ControlledPlayerIds { get; } = new();

        public ShooterHostFrameInput ReadInput(int controlledPlayerId)
        {
            ReadCount++;
            ControlledPlayerIds.Add(controlledPlayerId);
            return _inputs.Count > 0
                ? _inputs.Dequeue()
                : new ShooterHostFrameInput(0f, 0f, 0f, 1f, false);
        }
    }

    private sealed class RecordingViewSink : IShooterHostViewSink
    {
        public List<ShooterHostPresentationFrame> Frames { get; } = new();
        public int ClearCount { get; private set; }

        public void Render(in ShooterHostPresentationFrame frame)
        {
            Frames.Add(frame);
        }

        public void Clear()
        {
            ClearCount++;
            Frames.Clear();
        }
    }
}
