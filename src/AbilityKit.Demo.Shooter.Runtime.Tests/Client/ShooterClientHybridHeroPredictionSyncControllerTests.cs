using AbilityKit.Demo.Shooter.Runtime;
using AbilityKit.Demo.Shooter.View;
using AbilityKit.Network.Runtime;
using AbilityKit.Network.Runtime.Sync;
using AbilityKit.Protocol.Shooter;
using Xunit;

namespace AbilityKit.Demo.Shooter.Runtime.Tests;

public sealed class ShooterClientHybridHeroPredictionSyncControllerTests
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

    [Fact]
    public void ControllerBuffersRemoteSamplesThroughHybridStrategyContract()
    {
        var controller = new ShooterClientHybridHeroPredictionSyncController(
            new ShooterBattleRuntimePort(),
            new ShooterPresentationFacade(),
            tickRate: 30,
            decoder: null,
            gateway: null,
            new InterpolationConfig(ticksPerSecond: 1000L, interpolationDelayTicks: 100L, bufferCapacity: 16, catchUpRate: 0d));
        IClientSyncStrategy<ShooterPlayerCommand, ShooterRemoteSnapshotSample> strategy = controller;

        strategy.ObserveRemote(new ShooterRemoteSnapshotSample(9001ul, frame: 1, serverTicks: 1000L,
            actors: RemoteSnapshot(1, 1000L, 0f).Actors));
        strategy.ObserveRemote(new ShooterRemoteSnapshotSample(9001ul, frame: 2, serverTicks: 1100L,
            actors: RemoteSnapshot(2, 1100L, 10f).Actors));

        Assert.Equal(NetworkSyncModel.HybridHeroPrediction, strategy.SyncModel);
        Assert.Equal(2, controller.BufferedRemoteSnapshotCount);
        Assert.Equal(1100L, controller.EstimatedServerTicks);
    }

    [Fact]
    public void ControllerPublishesInterpolatedRemoteFrameWhileKeepingHybridSyncModel()
    {
        var presentation = new ShooterPresentationFacade();
        var controller = new ShooterClientHybridHeroPredictionSyncController(
            new ShooterBattleRuntimePort(),
            presentation,
            tickRate: 30,
            decoder: null,
            gateway: null,
            new InterpolationConfig(ticksPerSecond: 1000L, interpolationDelayTicks: 100L, bufferCapacity: 16, catchUpRate: 0d));
        ShooterSnapshotViewBatch? lastBatch = null;
        presentation.Snapshots.SnapshotApplied += batch => lastBatch = batch;

        controller.BufferRemoteSnapshot(RemoteSnapshot(frame: 1, serverTicks: 1000L, actorX: 0f));
        controller.BufferRemoteSnapshot(RemoteSnapshot(frame: 2, serverTicks: 1100L, actorX: 10f));
        controller.Tick(0.05f);

        Assert.Equal(NetworkSyncModel.HybridHeroPrediction, controller.SyncModel);
        Assert.True(controller.HasPublishedRemoteFrame);
        Assert.NotNull(lastBatch);
        var key = new ShooterViewEntityKey(ShooterViewEntityKind.Player, 7);
        Assert.Equal(5f, TransformX(lastBatch!.Value, key), 2);
    }

    [Fact]
    public void ClientSessionExposesInterpolationDiagnosticsForHybridModel()
    {
        var session = new ShooterClientSession(
            new ShooterBattleRuntimePort(),
            ShooterPresentationSessionContext.CreateFromFacade(new ShooterPresentationFacade()),
            tickRate: 30,
            decoder: null,
            gateway: null,
            syncModel: NetworkSyncModel.HybridHeroPrediction,
            interpolationConfig: new InterpolationConfig(ticksPerSecond: 1000L, interpolationDelayTicks: 100L, bufferCapacity: 8, catchUpRate: 0d));
        var controller = Assert.IsType<ShooterClientHybridHeroPredictionSyncController>(session.SyncController);

        controller.BufferRemoteSnapshot(RemoteSnapshot(frame: 1, serverTicks: 1000L, actorX: 0f));
        controller.BufferRemoteSnapshot(RemoteSnapshot(frame: 2, serverTicks: 1100L, actorX: 10f));
        session.Tick(0f);

        Assert.True(session.TryGetInterpolationDiagnostics(out var diagnostics));
        Assert.Equal(2, diagnostics.BufferedRemoteSnapshotCount);
        Assert.Equal(1100L, diagnostics.EstimatedServerTicks);
        Assert.Equal(1000L, diagnostics.RemotePlaybackTicks);
        Assert.Equal(100L, diagnostics.PlaybackDelayTicks);
        Assert.True(diagnostics.HasPublishedRemoteFrame);
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
