using System;
using AbilityKit.Demo.Shooter.Runtime;
using AbilityKit.Demo.Shooter.View;
using AbilityKit.Network.Runtime;
using AbilityKit.Protocol.Shooter;
using Xunit;

namespace AbilityKit.Demo.Shooter.Runtime.Tests;

public sealed class ShooterClientAuthoritativeInterpolationSyncControllerTests
{
    private static ShooterGatewaySnapshot RemoteSnapshot(int frame, long serverTicks, float actorX)
    {
        return new ShooterGatewaySnapshot(
            worldId: 9001ul,
            frame: frame,
            timestamp: 0d,
            serverTicks: serverTicks,
            isFullSnapshot: true,
            actors: new[]
            {
                new ShooterGatewayActorSnapshot(actorId: 7, x: actorX, y: 0f, rotation: 0f, velocityX: 0f, velocityY: 0f, hp: 100f, hpMax: 100f, teamId: 1)
            });
    }

    private static ShooterGatewaySnapshot RemoteSnapshot(int frame, long serverTicks, params ShooterGatewayActorSnapshot[] actors)
    {
        return new ShooterGatewaySnapshot(
            worldId: 9001ul,
            frame: frame,
            timestamp: 0d,
            serverTicks: serverTicks,
            isFullSnapshot: true,
            actors: actors);
    }

    private static ShooterGatewayActorSnapshot Actor(int actorId, float x) =>
        new ShooterGatewayActorSnapshot(actorId: actorId, x: x, y: 0f, rotation: 0f, velocityX: 0f, velocityY: 0f, hp: 100f, hpMax: 100f, teamId: 1);

    private static ShooterStartGamePayload SinglePlayerStart() =>
        new ShooterStartGamePayload(
            "authoritative-interpolation-prediction",
            30,
            9001,
            new[] { new ShooterStartPlayer(1, "P1", 0f, 0f) });

    private static ShooterGatewaySnapshot PackedProjectileSnapshot(int frame, long serverTicks, int ownerPlayerId, int bulletId)
    {
        var projectileLifecycle = new ShooterPackedComponentChunk(
            ShooterPackedComponentKinds.EntityLifecycle,
            ShooterPackedEntityKinds.Projectile,
            1,
            new[] { bulletId },
            Array.Empty<float>(),
            Array.Empty<float>(),
            Array.Empty<float>(),
            Array.Empty<float>(),
            Array.Empty<int>(),
            new[] { (byte)(ShooterPackedEntityFlags.Alive | ShooterPackedEntityFlags.Projectile) },
            new[] { ownerPlayerId },
            Array.Empty<int>());
        var packed = new ShooterPackedSnapshotPayload(
            ShooterPackedSnapshotCodec.CurrentVersion,
            9001ul,
            frame,
            serverTicks,
            ShooterPackedSnapshotFlags.Full,
            0u,
            1,
            Array.Empty<byte>(),
            new[] { projectileLifecycle });

        return new ShooterGatewaySnapshot(
            worldId: 9001ul,
            frame: frame,
            timestamp: 0d,
            serverTicks: serverTicks,
            isFullSnapshot: true,
            actors: Array.Empty<ShooterGatewayActorSnapshot>(),
            packedSnapshot: packed);
    }

    [Fact]
    public void ControllerBuffersRemoteSnapshotsAndSeedsTimeline()
    {
        var runtime = new ShooterBattleRuntimePort();
        var presentation = new ShooterPresentationFacade();
        // Snap mode (catchUpRate 0) so the estimate seeds directly to the newest observed server
        // time; soft clock convergence is exercised by the timeline tests.
        var config = new InterpolationConfig(ticksPerSecond: 1000L, interpolationDelayTicks: 100L, bufferCapacity: 16, catchUpRate: 0d);
        var controller = new ShooterClientAuthoritativeInterpolationSyncController(
            runtime,
            presentation,
            tickRate: 30,
            decoder: null,
            gateway: null,
            config);

        Assert.Equal(NetworkSyncModel.AuthoritativeInterpolation, controller.SyncModel);

        var first = controller.BufferRemoteSnapshot(RemoteSnapshot(frame: 1, serverTicks: 1000L, actorX: 0f));
        var second = controller.BufferRemoteSnapshot(RemoteSnapshot(frame: 2, serverTicks: 1100L, actorX: 10f));

        Assert.Equal(ShooterSnapshotApplyResult.AppliedActorSnapshot, first);
        Assert.Equal(ShooterSnapshotApplyResult.AppliedActorSnapshot, second);
        Assert.Equal(2, controller.BufferedRemoteSnapshotCount);
        Assert.Equal(1100L, controller.EstimatedServerTicks);
    }

    [Fact]
    public void ControllerRejectsStaleRemoteSnapshot()
    {
        var runtime = new ShooterBattleRuntimePort();
        var presentation = new ShooterPresentationFacade();
        var controller = new ShooterClientAuthoritativeInterpolationSyncController(
            runtime,
            presentation,
            tickRate: 30,
            decoder: null,
            gateway: null);

        controller.BufferRemoteSnapshot(RemoteSnapshot(frame: 2, serverTicks: 1100L, actorX: 10f));
        var stale = controller.BufferRemoteSnapshot(RemoteSnapshot(frame: 1, serverTicks: 1000L, actorX: 0f));

        Assert.Equal(ShooterSnapshotApplyResult.IgnoredStaleSnapshot, stale);
        Assert.Equal(1, controller.BufferedRemoteSnapshotCount);
    }

    [Fact]
    public void ControllerPublishesInterpolatedRemoteActorBetweenSnapshots()
    {
        var runtime = new ShooterBattleRuntimePort();
        var presentation = new ShooterPresentationFacade();
        // Millisecond timeline, 100ms interpolation delay, so playback sits between the two samples.
        // Snap mode (catchUpRate 0) keeps the playback clock deterministic for the interpolation
        // assertions below; soft convergence is covered separately by the timeline tests.
        var config = new InterpolationConfig(ticksPerSecond: 1000L, interpolationDelayTicks: 100L, bufferCapacity: 16, catchUpRate: 0d);
        var controller = new ShooterClientAuthoritativeInterpolationSyncController(
            runtime,
            presentation,
            tickRate: 30,
            decoder: null,
            gateway: null,
            config);

        ShooterSnapshotViewBatch? lastBatch = null;
        presentation.Snapshots.SnapshotApplied += batch => lastBatch = batch;

        // Two authoritative samples 100ms apart, actor moves 0 -> 10.
        controller.BufferRemoteSnapshot(RemoteSnapshot(frame: 1, serverTicks: 1000L, actorX: 0f));
        controller.BufferRemoteSnapshot(RemoteSnapshot(frame: 2, serverTicks: 1100L, actorX: 10f));

        // EstimatedServerTicks = 1100, playback = 1100 - 100 = 1000 -> alpha 0 (oldest sample).
        controller.Tick(0f);
        Assert.NotNull(lastBatch);
        var key = new ShooterViewEntityKey(ShooterViewEntityKind.Player, 7);
        var atStart = TransformX(lastBatch!.Value, key);
        Assert.Equal(0f, atStart, 3);

        // Advance 50ms: estimated = 1150, playback = 1050 -> halfway between 1000 and 1100, X ~= 5.
        controller.Tick(0.05f);
        var midX = TransformX(lastBatch!.Value, key);
        Assert.Equal(5f, midX, 2);

        Assert.True(controller.HasPublishedRemoteFrame);
    }

    [Fact]
    public void ControllerTracksLocalPredictedPoseForStateSyncMode()
    {
        var runtime = new ShooterBattleRuntimePort();
        var presentation = new ShooterPresentationFacade { ControlledPlayerId = 1 };
        var controller = new ShooterClientAuthoritativeInterpolationSyncController(
            runtime,
            presentation,
            tickRate: 30,
            decoder: null,
            gateway: null);
        var start = SinglePlayerStart();
        Assert.True(controller.StartGame(in start));

        controller.SubmitLocalInput(1, moveX: 1f, moveY: 0f, aimX: 1f, aimY: 0f, fire: false);
        controller.Tick(1f / 30f);

        var prediction = controller.PredictionState;
        Assert.True(prediction.HasPredictedPose);
        Assert.Equal(1, prediction.PlayerId);
        Assert.Equal(1, prediction.PredictedFrame);
        Assert.True(prediction.PredictedX > 0f);
        Assert.Equal(0f, prediction.PredictedY, 3);
    }

    [Fact]
    public void ControllerDoesNotApplyLocalFireUntilAuthoritativeProjectileArrives()
    {
        var runtime = new ShooterBattleRuntimePort();
        var presentation = new ShooterPresentationFacade { ControlledPlayerId = 1 };
        var config = new InterpolationConfig(ticksPerSecond: 1000L, interpolationDelayTicks: 0L, bufferCapacity: 16, catchUpRate: 0d);
        var controller = new ShooterClientAuthoritativeInterpolationSyncController(
            runtime,
            presentation,
            tickRate: 30,
            decoder: null,
            gateway: null,
            config);
        var start = SinglePlayerStart();
        Assert.True(controller.StartGame(in start));
        controller.SubmitLocalInput(1, moveX: 0f, moveY: 0f, aimX: 1f, aimY: 0f, fire: true);
        controller.Tick(1f / 30f);

        var localPrediction = controller.PredictionState;
        Assert.Equal(ShooterStateSyncPredictedAction.None, localPrediction.Action);
        Assert.Equal(0, localPrediction.ActionPlayerId);
        Assert.False(localPrediction.NeedsActionCatchUp);

        controller.BufferRemoteSnapshot(PackedProjectileSnapshot(frame: 1, serverTicks: 1000L, ownerPlayerId: 1, bulletId: 77));
        controller.Tick(1f / 30f);

        var authoritativePrediction = controller.PredictionState;
        Assert.Equal(ShooterStateSyncPredictedAction.Fire, authoritativePrediction.Action);
        Assert.Equal(1, authoritativePrediction.ActionPlayerId);
        Assert.Equal(1, authoritativePrediction.ActionSourceFrame);
        Assert.Equal(1000L, authoritativePrediction.ActionSourceServerTicks);
        Assert.True(authoritativePrediction.NeedsActionCatchUp);
        Assert.Equal(77, authoritativePrediction.ActionBulletId);
        Assert.True(authoritativePrediction.ActionCatchUpFrames >= 0);
    }

    [Fact]
    public void ControllerDoesNotPublishRemoteFrameWithoutSnapshots()
    {
        var runtime = new ShooterBattleRuntimePort();
        var presentation = new ShooterPresentationFacade();
        var controller = new ShooterClientAuthoritativeInterpolationSyncController(
            runtime,
            presentation,
            tickRate: 30,
            decoder: null,
            gateway: null);

        controller.Tick(0.1f);

        Assert.False(controller.HasPublishedRemoteFrame);
    }

    [Fact]
    public void ControllerHoldsDespawningActorThroughInBetweenFrame()
    {
        var runtime = new ShooterBattleRuntimePort();
        var presentation = new ShooterPresentationFacade();
        var config = new InterpolationConfig(ticksPerSecond: 1000L, interpolationDelayTicks: 100L, bufferCapacity: 16, catchUpRate: 0d);
        var controller = new ShooterClientAuthoritativeInterpolationSyncController(
            runtime, presentation, tickRate: 30, decoder: null, gateway: null, config);

        ShooterSnapshotViewBatch? lastBatch = null;
        presentation.Snapshots.SnapshotApplied += batch => lastBatch = batch;

        // Actor 8 exists in the earlier sample but despawns in the later one.
        controller.BufferRemoteSnapshot(RemoteSnapshot(frame: 1, serverTicks: 1000L, Actor(7, 0f), Actor(8, 100f)));
        controller.BufferRemoteSnapshot(RemoteSnapshot(frame: 2, serverTicks: 1100L, Actor(7, 10f)));

        // playback = 1050 -> mid-interpolation. Actor 8 (despawned in 'to') must still be present,
        // holding its last pose rather than popping out mid-frame.
        controller.Tick(0.05f);

        var despawningKey = new ShooterViewEntityKey(ShooterViewEntityKind.Player, 8);
        var heldX = TransformX(lastBatch!.Value, despawningKey);
        Assert.Equal(100f, heldX, 3);
    }

    [Fact]
    public void ControllerFlagsStarvedPlaybackWhenBufferRunsDry()
    {
        var runtime = new ShooterBattleRuntimePort();
        var presentation = new ShooterPresentationFacade();
        // 50ms extrapolation tolerance, snap clock for deterministic playback timing.
        var config = new InterpolationConfig(
            ticksPerSecond: 1000L, interpolationDelayTicks: 0L, bufferCapacity: 16, catchUpRate: 0d, maxExtrapolationTicks: 50L);
        var controller = new ShooterClientAuthoritativeInterpolationSyncController(
            runtime, presentation, tickRate: 30, decoder: null, gateway: null, config);

        controller.BufferRemoteSnapshot(RemoteSnapshot(frame: 1, serverTicks: 1000L, actorX: 0f));

        // No interpolation delay: playback already sits on the newest sample, within tolerance.
        controller.Tick(0f);
        Assert.True(controller.HasPublishedRemoteFrame);
        Assert.False(controller.IsRemotePlaybackStarved);

        // Advance 100ms with no new snapshots: playback runs 100 ticks past newest (> 50ms tolerance).
        controller.Tick(0.1f);
        Assert.True(controller.IsRemotePlaybackStarved);
    }

    private static float TransformX(in ShooterSnapshotViewBatch batch, ShooterViewEntityKey key)
    {
        foreach (var change in batch.TransformChanges)
        {
            if (change.Key.Equals(key))
            {
                return change.X;
            }
        }

        throw new Xunit.Sdk.XunitException($"Transform change for {key.Kind}:{key.EntityId} not found in batch.");
    }
}
