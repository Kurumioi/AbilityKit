using System;
using AbilityKit.Demo.Shooter.Runtime;
using AbilityKit.Demo.Shooter.View;
using AbilityKit.Network.Runtime;
using AbilityKit.Network.Runtime.Sync;
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

    private static ShooterGatewaySnapshot PackedPlayerSnapshot(
        int frame,
        float x,
        float y = 0f,
        float aimX = 1f,
        float aimY = 0f,
        bool isFull = true,
        ulong worldId = 9001ul,
        int hp = 100,
        int score = 0,
        bool alive = true,
        uint flags = 0u)
    {
        var entityFlags = alive
            ? (byte)(ShooterPackedEntityFlags.Alive | ShooterPackedEntityFlags.Player)
            : ShooterPackedEntityFlags.Player;
        var chunks = new[]
        {
            new ShooterPackedComponentChunk(
                ShooterPackedComponentKinds.EntityLifecycle,
                ShooterPackedEntityKinds.Player,
                1,
                new[] { 1 },
                Array.Empty<float>(), Array.Empty<float>(), Array.Empty<float>(), Array.Empty<float>(),
                Array.Empty<int>(), new[] { entityFlags }, Array.Empty<int>(), Array.Empty<int>()),
            new ShooterPackedComponentChunk(
                ShooterPackedComponentKinds.Transform,
                ShooterPackedEntityKinds.Player,
                1,
                new[] { 1 },
                new[] { x }, new[] { y }, new[] { aimX }, new[] { aimY },
                Array.Empty<int>(), Array.Empty<byte>(), Array.Empty<int>(), Array.Empty<int>()),
            new ShooterPackedComponentChunk(
                ShooterPackedComponentKinds.Health,
                ShooterPackedEntityKinds.Player,
                1,
                new[] { 1 },
                Array.Empty<float>(), Array.Empty<float>(), Array.Empty<float>(), Array.Empty<float>(),
                new[] { hp }, Array.Empty<byte>(), Array.Empty<int>(), Array.Empty<int>()),
            new ShooterPackedComponentChunk(
                ShooterPackedComponentKinds.Score,
                ShooterPackedEntityKinds.Player,
                1,
                new[] { 1 },
                Array.Empty<float>(), Array.Empty<float>(), Array.Empty<float>(), Array.Empty<float>(),
                new[] { score }, Array.Empty<byte>(), Array.Empty<int>(), Array.Empty<int>())
        };
        var snapshotFlags = flags | (isFull ? ShooterPackedSnapshotFlags.Full : 0u);
        var packed = new ShooterPackedSnapshotPayload(
            ShooterPackedSnapshotCodec.CurrentVersion,
            worldId,
            frame,
            frame * 100L,
            snapshotFlags,
            123u,
            1,
            Array.Empty<byte>(),
            chunks);
        return new ShooterGatewaySnapshot(
            worldId, frame, 0d, frame * 100L, isFull,
            Array.Empty<ShooterGatewayActorSnapshot>(),
            packedSnapshot: packed);
    }

    private static ShooterGatewaySnapshot PureStatePlayerSnapshot(
        int frame,
        float x,
        float y = 0f,
        float aimX = 1f,
        float aimY = 0f,
        bool isFull = true,
        ulong worldId = 9001ul,
        int hp = 100,
        int score = 0,
        bool alive = true)
    {
        var entityFlags = alive
            ? (byte)(ShooterPureStateEntityFlags.Alive | ShooterPureStateEntityFlags.Visible)
            : ShooterPureStateEntityFlags.Visible;
        var pureState = new ShooterPureStateSnapshotPayload(
            ShooterPureStateSyncCodec.CurrentVersion,
            worldId,
            frame,
            frame * 100L,
            isFull ? ShooterPureStateSnapshotKinds.FullBaseline : ShooterPureStateSnapshotKinds.Delta,
            isFull ? 0 : frame - 1,
            0u,
            123u,
            ShooterPureStateSyncSettings.Default,
            new[]
            {
                new ShooterPureStateEntityDelta(
                    1,
                    ShooterPackedEntityKinds.Player,
                    ShooterPureStateEntityLayers.KeyInteraction,
                    isFull ? ShooterPureStateDeltaKinds.Spawn : ShooterPureStateDeltaKinds.Update,
                    1,
                    (int)(x * 1000f),
                    (int)(y * 1000f),
                    (int)(aimX * 1000f),
                    (int)(aimY * 1000f),
                    hp,
                    score,
                    0,
                    entityFlags)
            },
            Array.Empty<ShooterPureStateVisibilityHint>());
        return new ShooterGatewaySnapshot(
            worldId, frame, 0d, frame * 100L, isFull,
            Array.Empty<ShooterGatewayActorSnapshot>(),
            pureStateSnapshot: pureState);
    }

    private static ShooterClientAuthoritativeInterpolationSyncController StartedController(
        ShooterBattleRuntimePort runtime,
        ShooterPresentationFacade presentation)
    {
        presentation.ControlledPlayerId = 1;
        var controller = new ShooterClientAuthoritativeInterpolationSyncController(
            runtime, presentation, tickRate: 30, decoder: null, gateway: null);
        var start = SinglePlayerStart();
        Assert.True(controller.StartGame(in start));
        return controller;
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

    [Fact]
    public void LocalSmallPositionErrorKeepsPredictionButRestoresAuthoritativeState()
    {
        var runtime = new ShooterBattleRuntimePort();
        var controller = StartedController(runtime, new ShooterPresentationFacade());

        controller.BufferRemoteSnapshot(PackedPlayerSnapshot(
            frame: 1,
            x: 0.04f,
            aimX: 0f,
            aimY: 1f,
            isFull: false,
            hp: 73,
            score: 9));

        Assert.True(runtime.TryGetPlayer(1, out var player));
        Assert.Equal(0f, player.X, 3);
        Assert.Equal(0f, player.AimX, 3);
        Assert.Equal(1f, player.AimY, 3);
        Assert.Equal(73, player.Hp);
        Assert.Equal(9, player.Score);
    }

    [Fact]
    public void LocalMediumErrorUsesBoundedCorrectionAndLargeErrorSnaps()
    {
        var runtime = new ShooterBattleRuntimePort();
        var controller = StartedController(runtime, new ShooterPresentationFacade());

        controller.BufferRemoteSnapshot(PackedPlayerSnapshot(frame: 1, x: 0.4f, isFull: false));
        Assert.True(runtime.TryGetPlayer(1, out var bounded));
        Assert.Equal(0.25f, bounded.X, 3);

        controller.BufferRemoteSnapshot(PackedPlayerSnapshot(frame: 2, x: 1f, isFull: false));
        Assert.True(runtime.TryGetPlayer(1, out var snapped));
        Assert.Equal(1f, snapped.X, 3);
    }

    [Fact]
    public void LocalReconciliationReplaysUnacknowledgedInputWithoutTickingWorld()
    {
        var runtime = new ShooterBattleRuntimePort();
        var controller = StartedController(runtime, new ShooterPresentationFacade());

        controller.SubmitLocalInput(1, moveX: 1f, moveY: 0f, aimX: 1f, aimY: 0f, fire: false);
        controller.BufferRemoteSnapshot(PackedPlayerSnapshot(frame: 1, x: 0f));

        Assert.True(runtime.TryGetPlayer(1, out var player));
        Assert.Equal(ShooterBattleTuning.PlayerSpeed / 30f, player.X, 3);
        IClientSyncStrategy<ShooterPlayerCommand, ShooterRemoteSnapshotSample> strategy = controller;
        var report = strategy.GetReconciliationReport();
        Assert.Equal(SyncReconciliationReason.LocalAuthorityCorrection, report.Reason);
        Assert.Equal(1, report.ReplayTicks);
    }

    [Fact]
    public void StaleSnapshotCannotPullLocalPlayerBack()
    {
        var runtime = new ShooterBattleRuntimePort();
        var controller = StartedController(runtime, new ShooterPresentationFacade());

        controller.BufferRemoteSnapshot(PackedPlayerSnapshot(frame: 2, x: 2f));
        var result = controller.BufferRemoteSnapshot(PackedPlayerSnapshot(frame: 1, x: 9f));

        Assert.Equal(ShooterSnapshotApplyResult.IgnoredStaleSnapshot, result);
        Assert.True(runtime.TryGetPlayer(1, out var player));
        Assert.Equal(2f, player.X, 3);
    }

    [Fact]
    public void ControlledPlayerIsExcludedFromRemoteInterpolation()
    {
        var runtime = new ShooterBattleRuntimePort();
        var presentation = new ShooterPresentationFacade { ControlledPlayerId = 1 };
        var config = new InterpolationConfig(1000L, 0L, 16, 0d);
        var controller = new ShooterClientAuthoritativeInterpolationSyncController(
            runtime, presentation, tickRate: 30, decoder: null, gateway: null, config);
        ShooterSnapshotViewBatch? lastBatch = null;
        presentation.Snapshots.SnapshotApplied += batch => lastBatch = batch;

        controller.BufferRemoteSnapshot(RemoteSnapshot(1, 1000L, Actor(1, 20f), Actor(7, 7f)));
        controller.Tick(0f);

        Assert.NotNull(lastBatch);
        Assert.DoesNotContain(lastBatch!.Value.TransformChanges,
            change => change.Key.Equals(new ShooterViewEntityKey(ShooterViewEntityKind.Player, 1)));
        Assert.Contains(lastBatch.Value.TransformChanges,
            change => change.Key.Equals(new ShooterViewEntityKey(ShooterViewEntityKind.Player, 7)));
    }

    [Fact]
    public void PackedAndPureStateSnapshotsApplyEquivalentLocalAuthority()
    {
        var packedRuntime = new ShooterBattleRuntimePort();
        var pureRuntime = new ShooterBattleRuntimePort();
        var packedController = StartedController(packedRuntime, new ShooterPresentationFacade());
        var pureController = StartedController(pureRuntime, new ShooterPresentationFacade());

        packedController.BufferRemoteSnapshot(PackedPlayerSnapshot(
            frame: 3, x: 2f, y: -1f, aimX: 0f, aimY: 1f, hp: 81, score: 4));
        pureController.BufferRemoteSnapshot(PureStatePlayerSnapshot(
            frame: 3, x: 2f, y: -1f, aimX: 0f, aimY: 1f, hp: 81, score: 4));

        Assert.True(packedRuntime.TryGetPlayer(1, out var packed));
        Assert.True(pureRuntime.TryGetPlayer(1, out var pure));
        Assert.Equal(packed.X, pure.X, 3);
        Assert.Equal(packed.Y, pure.Y, 3);
        Assert.Equal(packed.AimX, pure.AimX, 3);
        Assert.Equal(packed.AimY, pure.AimY, 3);
        Assert.Equal(packed.Hp, pure.Hp);
        Assert.Equal(packed.Score, pure.Score);
        Assert.Equal(packed.Alive, pure.Alive);
    }

    [Fact]
    public void WorldChangeForcesDeltaSnapshotToSnapAndResetsFrameWatermark()
    {
        var runtime = new ShooterBattleRuntimePort();
        var controller = StartedController(runtime, new ShooterPresentationFacade());

        controller.BufferRemoteSnapshot(PackedPlayerSnapshot(frame: 10, x: 1f, worldId: 9001ul));
        controller.BufferRemoteSnapshot(PackedPlayerSnapshot(
            frame: 1, x: 3f, isFull: false, worldId: 9002ul));

        Assert.True(runtime.TryGetPlayer(1, out var player));
        Assert.Equal(3f, player.X, 3);
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
