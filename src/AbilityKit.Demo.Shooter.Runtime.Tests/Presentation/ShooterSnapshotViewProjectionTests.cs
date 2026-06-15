using AbilityKit.Demo.Shooter.View;
using AbilityKit.Protocol.Shooter;
using Xunit;

namespace AbilityKit.Demo.Shooter.Runtime.Tests.Presentation;

public sealed class ShooterSnapshotViewProjectionTests
{
    [Fact]
    public void FullSnapshotProjectsEntitiesAndSeparatedComponentsIntoStore()
    {
        var projection = new ShooterSnapshotViewProjection();
        var player = new ShooterViewEntityKey(ShooterViewEntityKind.Player, 1);
        var bullet = new ShooterViewEntityKey(ShooterViewEntityKind.Bullet, 100);
        var batch = new ShooterSnapshotViewBatch(
            worldId: 77ul,
            frame: 12,
            sequence: 1ul,
            ShooterViewSnapshotKind.Full,
            ShooterViewBatchSource.AuthoritativeCorrection,
            new[]
            {
                new ShooterViewEntityChange(player, ownerEntityId: 0, alive: true),
                new ShooterViewEntityChange(bullet, ownerEntityId: 1, alive: true)
            },
            Array.Empty<ShooterViewEntityKey>(),
            new[]
            {
                new ShooterViewTransformComponentChange(player, 1f, 2f, 1f, 0f, 0.5f, 0.25f),
                new ShooterViewTransformComponentChange(bullet, 3f, 4f, 0f, 1f, 8f, 9f)
            },
            new[] { new ShooterViewHealthComponentChange(player, 80) },
            new[] { new ShooterViewScoreComponentChange(player, 7) },
            new[] { new ShooterViewProjectileLifetimeComponentChange(bullet, 19) },
            Array.Empty<ShooterEventSnapshot>());

        projection.Apply(in batch);

        Assert.Equal(2, projection.Store.EntityCount);
        Assert.Equal(1, projection.Store.PlayerCount);
        Assert.Equal(1, projection.Store.BulletCount);
        Assert.True(projection.Store.TryGetTransform(player, out var playerTransform));
        Assert.Equal(1f, playerTransform.X);
        Assert.Equal(2f, playerTransform.Y);
        Assert.True(projection.Store.TryGetHealth(player, out var health));
        Assert.Equal(80, health.Hp);
        Assert.True(projection.Store.TryGetScore(player, out var score));
        Assert.Equal(7, score.Score);
        Assert.True(projection.Store.TryGetProjectileLifetime(bullet, out var lifetime));
        Assert.Equal(19, lifetime.RemainingFrames);
    }

    [Fact]
    public void DeltaSnapshotUpdatesComponentsWithoutRemovingMissingEntities()
    {
        var projection = new ShooterSnapshotViewProjection();
        var playerOne = new ShooterViewEntityKey(ShooterViewEntityKind.Player, 1);
        var playerTwo = new ShooterViewEntityKey(ShooterViewEntityKind.Player, 2);
        var full = new ShooterSnapshotViewBatch(
            worldId: 77ul,
            frame: 1,
            sequence: 1ul,
            ShooterViewSnapshotKind.Full,
            ShooterViewBatchSource.AuthoritativeCorrection,
            new[]
            {
                new ShooterViewEntityChange(playerOne, 0, alive: true),
                new ShooterViewEntityChange(playerTwo, 0, alive: true)
            },
            Array.Empty<ShooterViewEntityKey>(),
            new[]
            {
                new ShooterViewTransformComponentChange(playerOne, 0f, 0f, 1f, 0f, 0f, 0f),
                new ShooterViewTransformComponentChange(playerTwo, 10f, 0f, 1f, 0f, 0f, 0f)
            },
            Array.Empty<ShooterViewHealthComponentChange>(),
            Array.Empty<ShooterViewScoreComponentChange>(),
            Array.Empty<ShooterViewProjectileLifetimeComponentChange>(),
            Array.Empty<ShooterEventSnapshot>());
        var delta = new ShooterSnapshotViewBatch(
            worldId: 77ul,
            frame: 2,
            sequence: 2ul,
            ShooterViewSnapshotKind.Delta,
            ShooterViewBatchSource.LocalPrediction,
            Array.Empty<ShooterViewEntityChange>(),
            Array.Empty<ShooterViewEntityKey>(),
            new[] { new ShooterViewTransformComponentChange(playerOne, 2f, 3f, 0f, 1f, 4f, 5f) },
            Array.Empty<ShooterViewHealthComponentChange>(),
            Array.Empty<ShooterViewScoreComponentChange>(),
            Array.Empty<ShooterViewProjectileLifetimeComponentChange>(),
            Array.Empty<ShooterEventSnapshot>());

        projection.Apply(in full);
        projection.Apply(in delta);

        Assert.Equal(2, projection.Store.PlayerCount);
        Assert.True(projection.Store.TryGetTransform(playerOne, out var playerOneTransform));
        Assert.Equal(2f, playerOneTransform.X);
        Assert.Equal(3f, playerOneTransform.Y);
        Assert.True(projection.Store.TryGetTransform(playerTwo, out var playerTwoTransform));
        Assert.Equal(10f, playerTwoTransform.X);
    }

    [Fact]
    public void PackedDeltaSnapshotDoesNotRemoveMissingEntities()
    {
        var projection = new ShooterSnapshotViewProjection();
        var mapper = new ShooterSnapshotViewModelMapper();
        var playerOne = new ShooterViewEntityKey(ShooterViewEntityKind.Player, 1);
        var playerTwo = new ShooterViewEntityKey(ShooterViewEntityKind.Player, 2);
        var full = new ShooterSnapshotViewBatch(
            worldId: 77ul,
            frame: 1,
            sequence: 1ul,
            ShooterViewSnapshotKind.Full,
            ShooterViewBatchSource.AuthoritativeCorrection,
            new[]
            {
                new ShooterViewEntityChange(playerOne, 0, alive: true),
                new ShooterViewEntityChange(playerTwo, 0, alive: true)
            },
            Array.Empty<ShooterViewEntityKey>(),
            new[]
            {
                new ShooterViewTransformComponentChange(playerOne, 0f, 0f, 1f, 0f, 0f, 0f),
                new ShooterViewTransformComponentChange(playerTwo, 10f, 0f, 1f, 0f, 0f, 0f)
            },
            Array.Empty<ShooterViewHealthComponentChange>(),
            Array.Empty<ShooterViewScoreComponentChange>(),
            Array.Empty<ShooterViewProjectileLifetimeComponentChange>(),
            Array.Empty<ShooterEventSnapshot>());
        var packedDelta = new ShooterPackedSnapshotPayload(
            ShooterPackedSnapshotCodec.CurrentVersion,
            worldId: 77ul,
            frame: 2,
            serverTick: 2L,
            snapshotFlags: ShooterPackedSnapshotFlags.Delta,
            stateHash: 0u,
            entityCount: 1,
            extensionPayload: Array.Empty<byte>(),
            componentChunks: new[]
            {
                new ShooterPackedComponentChunk(
                    ShooterPackedComponentKinds.Transform,
                    ShooterPackedEntityKinds.Player,
                    count: 1,
                    entityIds: new[] { 1 },
                    valueX: new[] { 2f },
                    valueY: new[] { 3f },
                    valueZ: new[] { 0f },
                    valueW: new[] { 1f },
                    intValues: Array.Empty<int>(),
                    flags: Array.Empty<byte>(),
                    ownerIds: Array.Empty<int>(),
                    aux: new[] { 40000, 50000 })
            });
        var gatewaySnapshot = new ShooterGatewaySnapshot(
            worldId: 77ul,
            frame: 2,
            timestamp: 2d,
            serverTicks: 2L,
            isFullSnapshot: false,
            actors: Array.Empty<ShooterGatewayActorSnapshot>(),
            payloadOpCode: ShooterOpCodes.Snapshot.PackedStateDelta,
            packedSnapshot: packedDelta);

        var delta = mapper.Map(in gatewaySnapshot);
        projection.Apply(in full);
        projection.Apply(in delta);

        Assert.Equal(ShooterViewSnapshotKind.Delta, delta.SnapshotKind);
        Assert.False(delta.ShouldReplaceMissingEntities);
        Assert.Equal(2, projection.Store.PlayerCount);
        Assert.True(projection.Store.TryGetTransform(playerOne, out var playerOneTransform));
        Assert.Equal(2f, playerOneTransform.X);
        Assert.Equal(3f, playerOneTransform.Y);
        Assert.True(projection.Store.TryGetTransform(playerTwo, out var playerTwoTransform));
        Assert.Equal(10f, playerTwoTransform.X);
    }

    [Fact]
    public void FullSnapshotRemovesEntitiesMissingFromAuthoritativeBatch()
    {
        var projection = new ShooterSnapshotViewProjection();
        var playerOne = new ShooterViewEntityKey(ShooterViewEntityKind.Player, 1);
        var playerTwo = new ShooterViewEntityKey(ShooterViewEntityKind.Player, 2);
        var first = new ShooterSnapshotViewBatch(
            worldId: 77ul,
            frame: 1,
            sequence: 1ul,
            ShooterViewSnapshotKind.Full,
            ShooterViewBatchSource.AuthoritativeCorrection,
            new[]
            {
                new ShooterViewEntityChange(playerOne, 0, alive: true),
                new ShooterViewEntityChange(playerTwo, 0, alive: true)
            },
            Array.Empty<ShooterViewEntityKey>(),
            new[]
            {
                new ShooterViewTransformComponentChange(playerOne, 0f, 0f, 1f, 0f, 0f, 0f),
                new ShooterViewTransformComponentChange(playerTwo, 10f, 0f, 1f, 0f, 0f, 0f)
            },
            Array.Empty<ShooterViewHealthComponentChange>(),
            Array.Empty<ShooterViewScoreComponentChange>(),
            Array.Empty<ShooterViewProjectileLifetimeComponentChange>(),
            Array.Empty<ShooterEventSnapshot>());
        var correction = new ShooterSnapshotViewBatch(
            worldId: 77ul,
            frame: 2,
            sequence: 2ul,
            ShooterViewSnapshotKind.Full,
            ShooterViewBatchSource.AuthoritativeCorrection,
            new[] { new ShooterViewEntityChange(playerOne, 0, alive: true) },
            Array.Empty<ShooterViewEntityKey>(),
            new[] { new ShooterViewTransformComponentChange(playerOne, 4f, 5f, 0f, 1f, 0f, 0f) },
            Array.Empty<ShooterViewHealthComponentChange>(),
            Array.Empty<ShooterViewScoreComponentChange>(),
            Array.Empty<ShooterViewProjectileLifetimeComponentChange>(),
            Array.Empty<ShooterEventSnapshot>());

        projection.Apply(in first);
        projection.Apply(in correction);

        Assert.True(projection.Store.ContainsEntity(playerOne));
        Assert.False(projection.Store.ContainsEntity(playerTwo));
        Assert.False(projection.Store.Transforms.ContainsKey(playerTwo));
        Assert.Equal(1, projection.Store.PlayerCount);
    }

    [Fact]
    public void FullBatchSynchronizesPresentationEntitySet()
    {
        var projection = new ShooterSnapshotViewProjection();
        var stalePlayer = new ShooterViewEntityKey(ShooterViewEntityKind.Player, 1);
        var deadPlayer = new ShooterViewEntityKey(ShooterViewEntityKind.Player, 2);
        var newPlayer = new ShooterViewEntityKey(ShooterViewEntityKind.Player, 3);
        var staleBullet = new ShooterViewEntityKey(ShooterViewEntityKind.Bullet, 100);
        var newBullet = new ShooterViewEntityKey(ShooterViewEntityKind.Bullet, 101);
        var first = new ShooterSnapshotViewBatch(
            worldId: 77ul,
            frame: 1,
            sequence: 1ul,
            ShooterViewSnapshotKind.Full,
            ShooterViewBatchSource.JoinOrReconnect,
            new[]
            {
                new ShooterViewEntityChange(stalePlayer, 0, alive: true),
                new ShooterViewEntityChange(deadPlayer, 0, alive: true),
                new ShooterViewEntityChange(staleBullet, 1, alive: true)
            },
            Array.Empty<ShooterViewEntityKey>(),
            new[]
            {
                new ShooterViewTransformComponentChange(stalePlayer, 1f, 1f, 1f, 0f, 0f, 0f),
                new ShooterViewTransformComponentChange(deadPlayer, 2f, 2f, 1f, 0f, 0f, 0f),
                new ShooterViewTransformComponentChange(staleBullet, 3f, 3f, 0f, 1f, 6f, 7f)
            },
            new[] { new ShooterViewHealthComponentChange(deadPlayer, 25) },
            Array.Empty<ShooterViewScoreComponentChange>(),
            new[] { new ShooterViewProjectileLifetimeComponentChange(staleBullet, 8) },
            Array.Empty<ShooterEventSnapshot>());
        var batchSync = new ShooterSnapshotViewBatch(
            worldId: 77ul,
            frame: 8,
            sequence: 2ul,
            ShooterViewSnapshotKind.Full,
            ShooterViewBatchSource.JoinOrReconnect,
            new[]
            {
                new ShooterViewEntityChange(deadPlayer, 0, alive: false),
                new ShooterViewEntityChange(newPlayer, 0, alive: true),
                new ShooterViewEntityChange(newBullet, 3, alive: true)
            },
            Array.Empty<ShooterViewEntityKey>(),
            new[]
            {
                new ShooterViewTransformComponentChange(deadPlayer, 20f, 20f, 1f, 0f, 0f, 0f),
                new ShooterViewTransformComponentChange(newPlayer, 4f, 5f, 0f, 1f, 0.5f, 0.25f),
                new ShooterViewTransformComponentChange(newBullet, 6f, 7f, 0f, 1f, 8f, 9f)
            },
            new[]
            {
                new ShooterViewHealthComponentChange(deadPlayer, 0),
                new ShooterViewHealthComponentChange(newPlayer, 100)
            },
            new[] { new ShooterViewScoreComponentChange(newPlayer, 3) },
            new[] { new ShooterViewProjectileLifetimeComponentChange(newBullet, 12) },
            Array.Empty<ShooterEventSnapshot>());

        projection.Apply(in first);
        var result = projection.Apply(in batchSync);
 
        Assert.Equal(2, result.AddedEntities);
        Assert.Equal(0, result.UpdatedEntities);
        Assert.Equal(3, result.RemovedEntities);
        Assert.Equal(3, result.MissingEntityRemovals);
        Assert.Equal(0, result.ExplicitEntityRemovals);
        Assert.Equal(0, result.DeadEntityRemovals);
        Assert.Equal(5, result.ComponentUpdates);
        Assert.Equal(2, result.FinalEntityCount);
        Assert.Equal(ShooterViewBatchSource.JoinOrReconnect, result.Source);
        Assert.False(projection.Store.ContainsEntity(stalePlayer));
        Assert.False(projection.Store.ContainsEntity(deadPlayer));
        Assert.False(projection.Store.Transforms.ContainsKey(stalePlayer));
        Assert.False(projection.Store.Transforms.ContainsKey(deadPlayer));
        Assert.False(projection.Store.Health.ContainsKey(deadPlayer));
        Assert.False(projection.Store.ContainsEntity(staleBullet));
        Assert.False(projection.Store.ProjectileLifetimes.ContainsKey(staleBullet));
        Assert.True(projection.Store.ContainsEntity(newPlayer));
        Assert.True(projection.Store.ContainsEntity(newBullet));
        Assert.True(projection.Store.TryGetTransform(newPlayer, out var newPlayerTransform));
        Assert.Equal(4f, newPlayerTransform.X);
        Assert.True(projection.Store.TryGetHealth(newPlayer, out var newPlayerHealth));
        Assert.Equal(100, newPlayerHealth.Hp);
        Assert.True(projection.Store.TryGetProjectileLifetime(newBullet, out var newBulletLifetime));
        Assert.Equal(12, newBulletLifetime.RemainingFrames);
        Assert.Equal(2, projection.Store.EntityCount);
        Assert.Equal(1, projection.Store.PlayerCount);
        Assert.Equal(1, projection.Store.BulletCount);
    }

    [Fact]
    public void LocalPredictionFullSnapshotDoesNotRemoveMissingEntities()
    {
        var projection = new ShooterSnapshotViewProjection();
        var playerOne = new ShooterViewEntityKey(ShooterViewEntityKind.Player, 1);
        var playerTwo = new ShooterViewEntityKey(ShooterViewEntityKind.Player, 2);
        var authoritative = new ShooterSnapshotViewBatch(
            worldId: 77ul,
            frame: 1,
            sequence: 1ul,
            ShooterViewSnapshotKind.Full,
            ShooterViewBatchSource.AuthoritativeCorrection,
            new[]
            {
                new ShooterViewEntityChange(playerOne, 0, alive: true),
                new ShooterViewEntityChange(playerTwo, 0, alive: true)
            },
            Array.Empty<ShooterViewEntityKey>(),
            Array.Empty<ShooterViewTransformComponentChange>(),
            Array.Empty<ShooterViewHealthComponentChange>(),
            Array.Empty<ShooterViewScoreComponentChange>(),
            Array.Empty<ShooterViewProjectileLifetimeComponentChange>(),
            Array.Empty<ShooterEventSnapshot>());
        var localPrediction = new ShooterSnapshotViewBatch(
            worldId: 77ul,
            frame: 2,
            sequence: 2ul,
            ShooterViewSnapshotKind.Full,
            ShooterViewBatchSource.LocalPrediction,
            new[] { new ShooterViewEntityChange(playerOne, 0, alive: true) },
            Array.Empty<ShooterViewEntityKey>(),
            Array.Empty<ShooterViewTransformComponentChange>(),
            Array.Empty<ShooterViewHealthComponentChange>(),
            Array.Empty<ShooterViewScoreComponentChange>(),
            Array.Empty<ShooterViewProjectileLifetimeComponentChange>(),
            Array.Empty<ShooterEventSnapshot>());

        projection.Apply(in authoritative);
        projection.Apply(in localPrediction);

        Assert.True(projection.Store.ContainsEntity(playerOne));
        Assert.True(projection.Store.ContainsEntity(playerTwo));
        Assert.Equal(2, projection.Store.PlayerCount);
    }

    [Fact]
    public void ProjectedSinkPublishesStoreAfterApplyingBatch()
    {
        var recordingSink = new RecordingProjectedViewSink();
        var projectedSink = new ShooterProjectedSnapshotViewSink(recordingSink);
        var player = new ShooterViewEntityKey(ShooterViewEntityKind.Player, 1);
        var batch = new ShooterSnapshotViewBatch(
            worldId: 77ul,
            frame: 1,
            sequence: 1ul,
            ShooterViewSnapshotKind.Full,
            ShooterViewBatchSource.AuthoritativeCorrection,
            new[] { new ShooterViewEntityChange(player, 0, alive: true) },
            Array.Empty<ShooterViewEntityKey>(),
            Array.Empty<ShooterViewTransformComponentChange>(),
            Array.Empty<ShooterViewHealthComponentChange>(),
            Array.Empty<ShooterViewScoreComponentChange>(),
            Array.Empty<ShooterViewProjectileLifetimeComponentChange>(),
            Array.Empty<ShooterEventSnapshot>());

        projectedSink.ApplySnapshot(in batch);
 
        Assert.Equal(1, recordingSink.ApplyCount);
        Assert.Equal(1, recordingSink.LastPlayerCount);
        Assert.Equal(1, recordingSink.LastFrame);
        Assert.Equal(1, recordingSink.LastAddedEntities);
        Assert.Equal(1, projectedSink.LastApplyResult.AddedEntities);
    }

    [Fact]
    public void SnapshotStreamKeepsBoundedRecentBatchesAndSamplesByFrame()
    {
        var stream = new ShooterSnapshotStream(bufferCapacity: 2);
        var first = CreateBatch(frame: 1, sequence: 1ul);
        var second = CreateBatch(frame: 2, sequence: 2ul);
        var third = CreateBatch(frame: 3, sequence: 3ul);

        stream.Publish(in first);
        stream.Publish(in second);
        stream.Publish(in third);

        Assert.Equal(2, stream.BufferedSnapshotCount);
        Assert.True(stream.TrySample(playbackFrame: 1.5f, out var sampledOldest));
        Assert.Equal(2, sampledOldest.Frame);
        Assert.True(stream.TrySample(playbackFrame: 99f, out var sampledLatest));
        Assert.Equal(3, sampledLatest.Frame);
    }

    [Fact]
    public void SnapshotStreamAdvancesDelayedPlaybackWithoutRepeatingSameSequence()
    {
        var stream = new ShooterSnapshotStream(bufferCapacity: 4)
        {
            PlaybackFramesPerSecond = 10f,
            InterpolationDelayFrames = 1f
        };
        var first = CreateBatch(frame: 10, sequence: 10ul);
        var second = CreateBatch(frame: 11, sequence: 11ul);
        var third = CreateBatch(frame: 12, sequence: 12ul);

        stream.Publish(in first);
        stream.Publish(in second);
        stream.Publish(in third);

        Assert.True(stream.TryAdvancePlayback(0f, out var initial));
        Assert.Equal(11, initial.Frame);
        Assert.False(stream.TryAdvancePlayback(0f, out _));
        Assert.True(stream.TryAdvancePlayback(0.1f, out var advanced));
        Assert.Equal(12, advanced.Frame);
    }

    [Fact]
    public void SnapshotStreamInterpolatesTransformAndStepsDiscreteComponentsByDefaultPolicy()
    {
        var stream = new ShooterSnapshotStream(bufferCapacity: 4);
        var first = CreateBatch(frame: 10, sequence: 10ul, x: 0f, hp: 100, score: 1);
        var second = CreateBatch(frame: 20, sequence: 20ul, x: 10f, hp: 50, score: 9);

        stream.Publish(in first);
        stream.Publish(in second);

        Assert.True(stream.TrySample(playbackFrame: 15f, out var sampled, out var isContinuousSample));
        Assert.True(isContinuousSample);
        Assert.Single(sampled.TransformChanges);
        Assert.Equal(5f, sampled.TransformChanges[0].X);
        Assert.Single(sampled.HealthChanges);
        Assert.Equal(100, sampled.HealthChanges[0].Hp);
        Assert.Single(sampled.ScoreChanges);
        Assert.Equal(1, sampled.ScoreChanges[0].Score);
    }

    [Fact]
    public void SnapshotStreamCanDisableTransformInterpolationThroughPolicy()
    {
        var policy = new ShooterSnapshotSamplingPolicy(new ShooterSnapshotSamplingPolicyOptions
        {
            TransformMode = ShooterSnapshotComponentSamplingMode.Step
        });
        var stream = new ShooterSnapshotStream(bufferCapacity: 4, policy);
        var first = CreateBatch(frame: 10, sequence: 10ul, x: 0f);
        var second = CreateBatch(frame: 20, sequence: 20ul, x: 10f);

        stream.Publish(in first);
        stream.Publish(in second);

        Assert.True(stream.TrySample(playbackFrame: 15f, out var sampled, out var isContinuousSample));
        Assert.False(isContinuousSample);
        Assert.Equal(0f, sampled.TransformChanges[0].X);
    }

    [Fact]
    public void SnapshotViewBinderConsumesBufferedSnapshotsOnInterpolationTick()
    {
        var stream = new ShooterSnapshotStream(bufferCapacity: 4)
        {
            PlaybackFramesPerSecond = 30f,
            InterpolationDelayFrames = 0f
        };
        var presentation = new ShooterPresentationFacade(
            new ShooterGatewaySnapshotDecoder(),
            new ShooterSnapshotViewAdapter(),
            stream);
        var sink = new RecordingSnapshotViewSink();
        using var binder = new ShooterSnapshotViewBinder(presentation, sink);
        var batch = CreateBatch(frame: 5, sequence: 5ul);

        presentation.Snapshots.Publish(in batch);

        Assert.Equal(0, sink.ApplyCount);
        Assert.True(binder.HasBufferedSnapshots);

        binder.TickInterpolation(0f);

        Assert.Equal(1, sink.ApplyCount);
        Assert.Equal(5, sink.LastFrame);
    }

    [Fact]
    public void SnapshotViewBinderCanBypassInterpolationForImmediateSync()
    {
        var presentation = new ShooterPresentationFacade();
        var sink = new RecordingSnapshotViewSink();
        using var binder = new ShooterSnapshotViewBinder(presentation, sink)
        {
            InterpolationEnabled = false
        };
        var batch = CreateBatch(frame: 6, sequence: 6ul);

        presentation.Snapshots.Publish(in batch);

        Assert.Equal(1, sink.ApplyCount);
        Assert.Equal(6, sink.LastFrame);
    }

    private static ShooterSnapshotViewBatch CreateBatch(int frame, ulong sequence)
    {
        return CreateBatch(frame, sequence, frame, hp: null, score: null);
    }

    private static ShooterSnapshotViewBatch CreateBatch(int frame, ulong sequence, float x, int? hp = null, int? score = null)
    {
        var player = new ShooterViewEntityKey(ShooterViewEntityKind.Player, 1);
        return new ShooterSnapshotViewBatch(
            worldId: 77ul,
            frame,
            sequence,
            ShooterViewSnapshotKind.Full,
            ShooterViewBatchSource.AuthoritativeCorrection,
            new[] { new ShooterViewEntityChange(player, 0, alive: true) },
            Array.Empty<ShooterViewEntityKey>(),
            new[] { new ShooterViewTransformComponentChange(player, x, 0f, 1f, 0f, 0f, 0f) },
            hp.HasValue ? new[] { new ShooterViewHealthComponentChange(player, hp.Value) } : Array.Empty<ShooterViewHealthComponentChange>(),
            score.HasValue ? new[] { new ShooterViewScoreComponentChange(player, score.Value) } : Array.Empty<ShooterViewScoreComponentChange>(),
            Array.Empty<ShooterViewProjectileLifetimeComponentChange>(),
            Array.Empty<ShooterEventSnapshot>());
    }

    private sealed class RecordingSnapshotViewSink : IShooterSnapshotViewSink
    {
        public int ApplyCount { get; private set; }

        public int LastFrame { get; private set; }

        public void ApplySnapshot(in ShooterSnapshotViewBatch batch)
        {
            ApplyCount++;
            LastFrame = batch.Frame;
        }

        public void Clear()
        {
            LastFrame = 0;
        }
    }

    private sealed class RecordingProjectedViewSink : IShooterProjectedViewSink
    {
        public int ApplyCount { get; private set; }

        public int LastPlayerCount { get; private set; }

        public int LastFrame { get; private set; }

        public int LastAddedEntities { get; private set; }
 
        public void ApplyViewState(
            ShooterViewEntityStore store,
            in ShooterSnapshotViewBatch sourceBatch,
            in ShooterViewProjectionApplyResult applyResult)
        {
            ApplyCount++;
            LastPlayerCount = store.PlayerCount;
            LastFrame = sourceBatch.Frame;
            LastAddedEntities = applyResult.AddedEntities;
        }

        public void Clear()
        {
            LastPlayerCount = 0;
            LastFrame = 0;
            LastAddedEntities = 0;
        }
    }
}
