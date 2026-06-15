using AbilityKit.Core.Math;
using AbilityKit.Network.Runtime;
using AbilityKit.Network.Runtime.LagCompensation;
using Xunit;

namespace AbilityKit.Network.Runtime.Tests;

public sealed class ServerRewindLagCompensationServiceTests
{
    [Fact]
    public void ExposesServerRewindSyncModel()
    {
        var service = new ServerRewindLagCompensationService();

        Assert.Equal(NetworkSyncModel.ServerRewindLagCompensation, service.SyncModel);
    }

    [Fact]
    public void DefaultConfigRetainsRecordedHistory()
    {
        var service = new ServerRewindLagCompensationService();

        service.RecordFrame(1, new[] { Entity(2, new Vec3(1f, 0f, 0f), radius: 0.5f) });
        service.RecordFrame(2, new[] { Entity(2, new Vec3(2f, 0f, 0f), radius: 0.5f) });

        Assert.Equal(2, service.CapturedFrameCount);
        Assert.Equal(1, service.OldestFrame);
        Assert.Equal(2, service.LatestFrame);
    }

    [Fact]
    public void AcceptsHitAgainstRewoundFrameWhenCurrentFrameWouldMiss()
    {
        var service = new ServerRewindLagCompensationService(new ServerRewindLagCompensationConfig(
            maxHistoryFrames: 8,
            maxRewindFrames: 10,
            hitRadiusPadding: 0f));

        service.RecordFrame(10, new[]
        {
            Entity(2, new Vec3(5f, 0f, 0f), radius: 0.5f)
        });
        service.RecordFrame(12, new[]
        {
            Entity(2, new Vec3(5f, 3f, 0f), radius: 0.5f)
        });

        var query = new LagCompensationQuery(
            shooterEntityId: 1,
            origin: Vec3.Zero,
            direction: Vec3.Right,
            maxDistance: 10f,
            targetLayerMask: 1,
            rewindFrame: 10,
            serverReceiveFrame: 12);

        var accepted = service.TryEvaluateHit(in query, out var result);

        Assert.True(accepted);
        Assert.True(result.Accepted);
        Assert.Equal(LagCompensationResultReason.Hit, result.Reason);
        Assert.Equal(10, result.RequestedFrame);
        Assert.Equal(10, result.EvaluatedFrame);
        Assert.Equal(2, result.HitEntityId);
        Assert.InRange(result.Distance, 4.49f, 4.51f);
    }

    [Fact]
    public void RejectsWhenRewindWindowIsExceeded()
    {
        var service = new ServerRewindLagCompensationService(new ServerRewindLagCompensationConfig(
            maxHistoryFrames: 16,
            maxRewindFrames: 3));
        service.RecordFrame(10, new[] { Entity(2, new Vec3(5f, 0f, 0f), radius: 0.5f) });

        var query = new LagCompensationQuery(
            shooterEntityId: 1,
            origin: Vec3.Zero,
            direction: Vec3.Right,
            maxDistance: 10f,
            targetLayerMask: 1,
            rewindFrame: 10,
            serverReceiveFrame: 14);

        var accepted = service.TryEvaluateHit(in query, out var result);

        Assert.False(accepted);
        Assert.False(result.Accepted);
        Assert.Equal(LagCompensationResultReason.RewindWindowExceeded, result.Reason);
        Assert.Equal(10, result.RequestedFrame);
        Assert.Equal(-1, result.EvaluatedFrame);
    }

    [Fact]
    public void TrimsOldestHistoryBeyondCapacity()
    {
        var service = new ServerRewindLagCompensationService(new ServerRewindLagCompensationConfig(
            maxHistoryFrames: 2,
            maxRewindFrames: 10));

        service.RecordFrame(1, new[] { Entity(2, new Vec3(1f, 0f, 0f), radius: 0.5f) });
        service.RecordFrame(2, new[] { Entity(2, new Vec3(2f, 0f, 0f), radius: 0.5f) });
        service.RecordFrame(3, new[] { Entity(2, new Vec3(3f, 0f, 0f), radius: 0.5f) });

        Assert.Equal(2, service.CapturedFrameCount);
        Assert.Equal(2, service.OldestFrame);
        Assert.Equal(3, service.LatestFrame);

        var query = new LagCompensationQuery(
            shooterEntityId: 1,
            origin: Vec3.Zero,
            direction: Vec3.Right,
            maxDistance: 10f,
            targetLayerMask: 1,
            rewindFrame: 1,
            serverReceiveFrame: 3);

        var accepted = service.TryEvaluateHit(in query, out var result);

        Assert.False(accepted);
        Assert.Equal(LagCompensationResultReason.HistoryUnavailable, result.Reason);
    }

    [Fact]
    public void UsesNearestOlderFrameForSubTickStyleRewindRequests()
    {
        var service = new ServerRewindLagCompensationService(new ServerRewindLagCompensationConfig(
            maxHistoryFrames: 8,
            maxRewindFrames: 10));

        service.RecordFrame(10, new[] { Entity(2, new Vec3(5f, 0f, 0f), radius: 0.5f) });
        service.RecordFrame(15, new[] { Entity(2, new Vec3(5f, 3f, 0f), radius: 0.5f) });

        var query = new LagCompensationQuery(
            shooterEntityId: 1,
            origin: Vec3.Zero,
            direction: Vec3.Right,
            maxDistance: 10f,
            targetLayerMask: 1,
            rewindFrame: 12,
            serverReceiveFrame: 15);

        var accepted = service.TryEvaluateHit(in query, out var result);

        Assert.True(accepted);
        Assert.Equal(10, result.EvaluatedFrame);
        Assert.Equal(2, result.HitEntityId);
    }

    [Fact]
    public void PaddingMakesNearMissAcceptedForFavorTheShooterTolerance()
    {
        var service = new ServerRewindLagCompensationService(new ServerRewindLagCompensationConfig(
            maxHistoryFrames: 8,
            maxRewindFrames: 10,
            hitRadiusPadding: 0.15f));
        service.RecordFrame(10, new[] { Entity(2, new Vec3(5f, 0.6f, 0f), radius: 0.5f) });

        var query = new LagCompensationQuery(
            shooterEntityId: 1,
            origin: Vec3.Zero,
            direction: Vec3.Right,
            maxDistance: 10f,
            targetLayerMask: 1,
            rewindFrame: 10,
            serverReceiveFrame: 12);

        var accepted = service.TryEvaluateHit(in query, out var result);

        Assert.True(accepted);
        Assert.Equal(LagCompensationResultReason.Hit, result.Reason);
        Assert.Equal(2, result.HitEntityId);
    }

    [Fact]
    public void MissesDeadSelfAndFilteredLayerTargets()
    {
        var service = new ServerRewindLagCompensationService(new ServerRewindLagCompensationConfig(
            maxHistoryFrames: 8,
            maxRewindFrames: 10));
        service.RecordFrame(10, new[]
        {
            Entity(1, new Vec3(1f, 0f, 0f), radius: 1f),
            Entity(2, new Vec3(3f, 0f, 0f), radius: 1f, layerMask: 2),
            Entity(3, new Vec3(5f, 0f, 0f), radius: 1f, isAlive: false)
        });

        var query = new LagCompensationQuery(
            shooterEntityId: 1,
            origin: Vec3.Zero,
            direction: Vec3.Right,
            maxDistance: 10f,
            targetLayerMask: 1,
            rewindFrame: 10,
            serverReceiveFrame: 12);

        var accepted = service.TryEvaluateHit(in query, out var result);

        Assert.False(accepted);
        Assert.Equal(LagCompensationResultReason.Miss, result.Reason);
        Assert.Equal(10, result.EvaluatedFrame);
    }

    private static LagCompensatedEntitySnapshot Entity(
        int id,
        Vec3 position,
        float radius,
        int layerMask = 1,
        bool isAlive = true)
    {
        return new LagCompensatedEntitySnapshot(id, in position, radius, layerMask, isAlive);
    }
}
