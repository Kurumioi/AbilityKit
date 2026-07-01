using System;
using AbilityKit.Demo.Shooter.View;
using AbilityKit.Protocol.Shooter;
using Xunit;

namespace AbilityKit.Demo.Shooter.Runtime.Tests.Client;

public sealed class ShooterFrameworkSnapshotPipelineTests
{
    [Fact]
    public void FeedGatewaySnapshot_WhenPackedPayload_UsesFramePacketAggregatorAndDispatcher()
    {
        using var pipeline = new ShooterFrameworkSnapshotPipeline();
        var packed = new ShooterPackedSnapshotPayload(
            ShooterPackedSnapshotCodec.CurrentVersion,
            7UL,
            12,
            1200L,
            ShooterPackedSnapshotFlags.Full,
            0x1234u,
            3,
            Array.Empty<byte>(),
            Array.Empty<ShooterPackedComponentChunk>());
        var gateway = new ShooterGatewaySnapshot(
            7UL,
            12,
            1.25d,
            1200L,
            true,
            Array.Empty<ShooterGatewayActorSnapshot>(),
            ShooterOpCodes.Snapshot.PackedState,
            packed,
            null);

        var packet = pipeline.FeedGatewaySnapshot(in gateway);
        var frame = pipeline.BuildSnapshotFrame(12);
        var diagnostics = pipeline.Diagnostics;

        Assert.Equal("shooter:7", packet.WorldId.Value);
        Assert.Equal(12, packet.Frame.Value);
        Assert.True(packet.Snapshot.HasValue);
        Assert.Equal(ShooterOpCodes.Snapshot.PackedState, packet.Snapshot.Value.OpCode);
        Assert.Single(frame.Envelopes);
        Assert.Equal(1, diagnostics.PacketCount);
        Assert.Equal(1, diagnostics.DispatchedSnapshotCount);
        Assert.Equal(1, diagnostics.PackedSnapshotCount);
        Assert.Equal(0, diagnostics.PureStateSnapshotCount);
        Assert.Equal(12, diagnostics.LastFrame);
        Assert.Equal(ShooterOpCodes.Snapshot.PackedState, diagnostics.LastPayloadOpCode);
        Assert.Equal("shooter:7", diagnostics.LastWorldId);
    }

    [Fact]
    public void FeedGatewaySnapshot_WhenPureStatePayload_UsesRegisteredPureStateRoute()
    {
        using var pipeline = new ShooterFrameworkSnapshotPipeline();
        var pureState = new ShooterPureStateSnapshotPayload(
            ShooterPureStateSyncCodec.CurrentVersion,
            9UL,
            21,
            2100L,
            ShooterPureStateSnapshotKinds.FullBaseline,
            0,
            0u,
            0x5678u,
            ShooterPureStateSyncSettings.Default,
            Array.Empty<ShooterPureStateEntityDelta>(),
            Array.Empty<ShooterPureStateVisibilityHint>());
        var gateway = new ShooterGatewaySnapshot(
            9UL,
            21,
            2.5d,
            2100L,
            true,
            Array.Empty<ShooterGatewayActorSnapshot>(),
            ShooterOpCodes.Snapshot.PureState,
            null,
            pureState);

        pipeline.FeedGatewaySnapshot(in gateway);
        var frame = pipeline.BuildSnapshotFrame(21);
        var diagnostics = pipeline.Diagnostics;

        Assert.Single(frame.Envelopes);
        Assert.Equal(1, diagnostics.PacketCount);
        Assert.Equal(1, diagnostics.DispatchedSnapshotCount);
        Assert.Equal(0, diagnostics.PackedSnapshotCount);
        Assert.Equal(1, diagnostics.PureStateSnapshotCount);
        Assert.Equal(21, diagnostics.LastFrame);
        Assert.Equal(ShooterOpCodes.Snapshot.PureState, diagnostics.LastPayloadOpCode);
        Assert.Equal("shooter:9", diagnostics.LastWorldId);
    }
}
