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
    public void PackedSnapshotRoundTripRestoresExplosiveProjectileMetadata()
    {
        var source = new ShooterBattleRuntimePort();
        var start = new ShooterStartGamePayload(
            "explosive-packed-roundtrip",
            30,
            1235,
            new[] { new ShooterStartPlayer(1, "P1", 0f, 0f) });

        Assert.True(source.StartGame(in start));
        source.SubmitInput(0, new[] { new ShooterPlayerCommand(1, 0f, 0f, 1f, 0f, true, ShooterPlayerAttackSlots.Spread) });
        Assert.True(source.Tick(0f));
        var sourcePacked = source.ExportPackedSnapshot(42ul, isFullSnapshot: true, authorityOverride: true);
        var sourceLifetime = FindPackedChunk(sourcePacked, ShooterPackedComponentKinds.ProjectileLifetime, ShooterPackedEntityKinds.Projectile);
        Assert.NotNull(sourceLifetime);
        Assert.True(sourceLifetime.Value.ValueX[0] > 0f);
        Assert.Equal(1f, sourceLifetime.Value.ValueY[0]);

        var bytes = source.ExportPackedSnapshotBytes(42ul, isFullSnapshot: true, authorityOverride: true);
        var target = new ShooterBattleRuntimePort();

        Assert.True(target.ImportPackedSnapshotBytes(bytes));
        var restoredPacked = target.ExportPackedSnapshot(42ul, isFullSnapshot: true, authorityOverride: true);
        var restoredLifetime = FindPackedChunk(restoredPacked, ShooterPackedComponentKinds.ProjectileLifetime, ShooterPackedEntityKinds.Projectile);
        Assert.NotNull(restoredLifetime);
        Assert.Equal(sourceLifetime.Value.ValueX[0], restoredLifetime.Value.ValueX[0], 5);
        Assert.Equal(sourceLifetime.Value.ValueY[0], restoredLifetime.Value.ValueY[0]);
        Assert.Equal(source.ComputeStateHash(), target.ComputeStateHash());
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
    public void DeltaSnapshotImportRemovesProjectileWhenLifecycleMarksDespawned()
    {
        var source = new ShooterBattleRuntimePort();
        var start = new ShooterStartGamePayload(
            "delta-despawn-import",
            30,
            4250,
            new[] { new ShooterStartPlayer(1, "P1", 0f, 0f) });
        Assert.True(source.StartGame(in start));
        source.SubmitInput(0, new[] { new ShooterPlayerCommand(1, 0f, 0f, 1f, 0f, true) });
        Assert.True(source.Tick(1f / 30f));
        var fullSnapshot = source.ExportPackedSnapshot(99ul, isFullSnapshot: true, authorityOverride: true);
        var projectileLifecycle = FindPackedChunk(fullSnapshot, ShooterPackedComponentKinds.EntityLifecycle, ShooterPackedEntityKinds.Projectile);
        Assert.NotNull(projectileLifecycle);
        Assert.Equal(1, projectileLifecycle.Value.Count);
        var projectileId = projectileLifecycle.Value.EntityIds[0];

        var target = new ShooterBattleRuntimePort();
        Assert.True(target.ImportPackedSnapshot(in fullSnapshot));
        Assert.Equal(fullSnapshot.EntityCount, target.ExportPackedSnapshot(99ul).EntityCount);

        var despawnDelta = new ShooterPackedSnapshotPayload(
            ShooterPackedSnapshotCodec.CurrentVersion,
            worldId: 99ul,
            frame: fullSnapshot.Frame + 1,
            serverTick: fullSnapshot.ServerTick + 1,
            snapshotFlags: ShooterPackedSnapshotFlags.Delta,
            stateHash: 0u,
            entityCount: 1,
            extensionPayload: Array.Empty<byte>(),
            componentChunks: new[]
            {
                new ShooterPackedComponentChunk(
                    ShooterPackedComponentKinds.EntityLifecycle,
                    ShooterPackedEntityKinds.Projectile,
                    count: 1,
                    entityIds: new[] { projectileId },
                    valueX: Array.Empty<float>(),
                    valueY: Array.Empty<float>(),
                    valueZ: Array.Empty<float>(),
                    valueW: Array.Empty<float>(),
                    intValues: Array.Empty<int>(),
                    flags: new[] { (byte)(ShooterPackedEntityFlags.Projectile | ShooterPackedEntityFlags.Despawned) },
                    ownerIds: Array.Empty<int>(),
                    aux: Array.Empty<int>())
            });

        Assert.True(target.ImportPackedSnapshot(in despawnDelta));

        var restored = target.ExportPackedSnapshot(99ul, isFullSnapshot: true, authorityOverride: true);
        var restoredProjectileLifecycle = FindPackedChunk(restored, ShooterPackedComponentKinds.EntityLifecycle, ShooterPackedEntityKinds.Projectile);
        Assert.True(!restoredProjectileLifecycle.HasValue || restoredProjectileLifecycle.Value.Count == 0);
        Assert.Equal(fullSnapshot.EntityCount - 1, restored.EntityCount);
        Assert.Equal(despawnDelta.Frame, target.CurrentFrame);
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

    private static ShooterPackedComponentChunk? FindPackedChunk(in ShooterPackedSnapshotPayload snapshot, int componentKind, int entityKind)
    {
        for (int i = 0; i < snapshot.ComponentChunks.Length; i++)
        {
            var chunk = snapshot.ComponentChunks[i];
            if (chunk.ComponentKind == componentKind && chunk.EntityKind == entityKind)
            {
                return chunk;
            }
        }

        return null;
    }
}
