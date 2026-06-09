using AbilityKit.Demo.Shooter.Runtime;
using AbilityKit.Protocol.Room;
using AbilityKit.Protocol.Shooter;
using Xunit;

namespace AbilityKit.Demo.Shooter.Runtime.Tests;

public sealed class ShooterPackedSnapshotRuntimeTests
{
    [Fact]
    public void PackedSnapshotRoundTripRestoresHashAndEntities()
    {
        var source = new ShooterBattleRuntimePort();
        var start = new ShooterStartGamePayload(
            "smoke",
            30,
            1234,
            new[]
            {
                new ShooterStartPlayer(1, "P1", 0f, 0f),
                new ShooterStartPlayer(2, "P2", 3f, 0f)
            });

        Assert.True(source.StartGame(in start));
        source.SubmitInput(0, new[] { new ShooterPlayerCommand(1, 0f, 0f, 1f, 0f, true) });
        Assert.True(source.Tick(1f / 30f));

        var sourceSnapshot = source.ExportPackedSnapshot(42ul, isFullSnapshot: true, authorityOverride: true);
        var sourceHash = source.ComputeStateHash();
        var bytes = source.ExportPackedSnapshotBytes(42ul, isFullSnapshot: true, authorityOverride: true);

        Assert.NotEmpty(bytes);
        Assert.Equal(sourceHash, sourceSnapshot.StateHash);
        Assert.Equal(3, sourceSnapshot.EntityCount);
        Assert.True((sourceSnapshot.SnapshotFlags & ShooterPackedSnapshotFlags.AuthorityOverride) != 0);

        var target = new ShooterBattleRuntimePort();
        Assert.True(target.ImportPackedSnapshotBytes(bytes));

        var restoredSnapshot = target.ExportPackedSnapshot(42ul, isFullSnapshot: true, authorityOverride: true);
        Assert.Equal(source.CurrentFrame, target.CurrentFrame);
        Assert.Equal(sourceHash, target.ComputeStateHash());
        Assert.Equal(sourceSnapshot.EntityCount, restoredSnapshot.EntityCount);
        Assert.Equal(sourceSnapshot.Frame, restoredSnapshot.Frame);
    }

    [Fact]
    public void RoomWireSnapshotPushPreservesPackedShooterPayload()
    {
        var runtime = new ShooterBattleRuntimePort();
        var start = new ShooterStartGamePayload(
            "wire-smoke",
            30,
            5678,
            new[]
            {
                new ShooterStartPlayer(1, "P1", -1f, 0f),
                new ShooterStartPlayer(2, "P2", 2f, 0f)
            });

        Assert.True(runtime.StartGame(in start));
        runtime.SubmitInput(0, new[] { new ShooterPlayerCommand(1, 1f, 0f, 1f, 0f, true) });
        Assert.True(runtime.Tick(1f / 30f));

        var packed = runtime.ExportPackedSnapshot(9001ul, isFullSnapshot: true, authorityOverride: true);
        var packedBytes = ShooterPackedSnapshotCodec.Serialize(in packed);
        var wire = new WireStateSyncSnapshotPush
        {
            WorldId = packed.WorldId,
            Frame = packed.Frame,
            Timestamp = 123.5,
            IsFullSnapshot = true,
            Actors = null,
            PayloadOpCode = ShooterOpCodes.Snapshot.PackedState,
            Payload = packedBytes
        };

        var wireBytes = WireRoomGatewayBinary.Serialize(in wire);
        var restoredWire = WireRoomGatewayBinary.Deserialize<WireStateSyncSnapshotPush>(wireBytes);

        Assert.Equal(wire.WorldId, restoredWire.WorldId);
        Assert.Equal(wire.Frame, restoredWire.Frame);
        Assert.True(restoredWire.IsFullSnapshot);
        Assert.Equal(ShooterOpCodes.Snapshot.PackedState, restoredWire.PayloadOpCode);
        Assert.NotNull(restoredWire.Payload);
        Assert.Equal(packedBytes, restoredWire.Payload);

        var restoredPacked = ShooterPackedSnapshotCodec.Deserialize(restoredWire.Payload!);
        Assert.Equal(packed.WorldId, restoredPacked.WorldId);
        Assert.Equal(packed.Frame, restoredPacked.Frame);
        Assert.Equal(packed.EntityCount, restoredPacked.EntityCount);
        Assert.Equal(packed.StateHash, restoredPacked.StateHash);
    }
}
