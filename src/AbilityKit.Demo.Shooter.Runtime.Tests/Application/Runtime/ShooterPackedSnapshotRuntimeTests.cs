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

    [Fact]
    public void DeltaSnapshotPreservesExistingEntitiesAndUpdatesComponents()
    {
        // Full import establishes the baseline world.
        var source = new ShooterBattleRuntimePort();
        var start = new ShooterStartGamePayload(
            "delta-import",
            30,
            4200,
            new[]
            {
                new ShooterStartPlayer(1, "P1", 0f, 0f),
                new ShooterStartPlayer(2, "P2", 5f, 0f)
            });
        Assert.True(source.StartGame(in start));

        // Advance a few frames so entities move.
        source.SubmitInput(0, new[] { new ShooterPlayerCommand(1, 1f, 0f, 1f, 0f, false) });
        Assert.True(source.Tick(1f / 30f));
        var fullSnapshot = source.ExportPackedSnapshot(99ul, isFullSnapshot: true, authorityOverride: true);
        Assert.True((fullSnapshot.SnapshotFlags & ShooterPackedSnapshotFlags.Full) != 0);

        var target = new ShooterBattleRuntimePort();
        Assert.True(target.ImportPackedSnapshot(in fullSnapshot));
        var initialCount = target.ExportPackedSnapshot(99ul).EntityCount;

        // Advance the source further, then export a delta snapshot (not full).
        source.SubmitInput(0, new[] { new ShooterPlayerCommand(1, 0f, 1f, 0f, 1f, false) });
        Assert.True(source.Tick(1f / 30f));
        var deltaSnapshot = source.ExportPackedSnapshot(99ul, isFullSnapshot: false, authorityOverride: true);
        Assert.True((deltaSnapshot.SnapshotFlags & ShooterPackedSnapshotFlags.Delta) != 0);

        // Delta import must not reset the world.
        Assert.True(target.ImportPackedSnapshot(in deltaSnapshot));

        // Entity count must be preserved (delta doesn't remove unmentioned entities).
        var restored = target.ExportPackedSnapshot(99ul);
        Assert.Equal(initialCount, restored.EntityCount);
        Assert.Equal(deltaSnapshot.Frame, target.CurrentFrame);
    }

    [Fact]
    public void DeltaSnapshotImportPreservesEntityCountAfterMultipleDeltas()
    {
        var source = new ShooterBattleRuntimePort();
        var start = new ShooterStartGamePayload(
            "delta-chain",
            30,
            4300,
            new[]
            {
                new ShooterStartPlayer(1, "P1", 0f, 0f),
                new ShooterStartPlayer(2, "P2", 3f, 0f)
            });
        Assert.True(source.StartGame(in start));

        source.SubmitInput(0, new[] { new ShooterPlayerCommand(1, 1f, 0f, 1f, 0f, true) });
        Assert.True(source.Tick(1f / 30f));

        var target = new ShooterBattleRuntimePort();
        var fullSnapshot = source.ExportPackedSnapshot(98ul, isFullSnapshot: true, authorityOverride: true);
        Assert.True(target.ImportPackedSnapshot(in fullSnapshot));

        var baselineEntityCount = target.ExportPackedSnapshot(98ul).EntityCount;
        Assert.True(baselineEntityCount > 0);

        // Apply several delta snapshots without any full reset between them.
        for (var i = 0; i < 3; i++)
        {
            source.SubmitInput(0, new[] { new ShooterPlayerCommand(1, 0f, 1f, 0f, 1f, false) });
            Assert.True(source.Tick(1f / 30f));
            var delta = source.ExportPackedSnapshot(98ul, isFullSnapshot: false, authorityOverride: true);
            Assert.True(target.ImportPackedSnapshot(in delta));
        }

        Assert.Equal(baselineEntityCount, target.ExportPackedSnapshot(98ul).EntityCount);
        Assert.Equal(source.CurrentFrame, target.CurrentFrame);
    }
}
