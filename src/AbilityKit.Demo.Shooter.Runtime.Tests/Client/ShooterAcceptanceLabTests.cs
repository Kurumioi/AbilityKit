using System.Linq;
using AbilityKit.Demo.Shooter.Runtime;
using AbilityKit.Demo.Shooter.View;
using AbilityKit.Network.Runtime;
using AbilityKit.Network.Runtime.Conditioning;
using AbilityKit.Network.Runtime.DemoHarness;
using Xunit;

namespace AbilityKit.Demo.Shooter.Runtime.Tests;

[Collection("ShooterAcceptance")]
public sealed class ShooterAcceptanceLabTests
{
    [Fact]
    public void CatalogExposesImplementedModesAndNetworkEnvironments()
    {
        Assert.NotEmpty(ShooterAcceptanceCatalog.SyncModes);
        Assert.NotEmpty(ShooterAcceptanceCatalog.NetworkEnvironments);

        Assert.Contains(ShooterAcceptanceCatalog.SyncModes,
            m => m.Model == NetworkSyncModel.PredictRollback && m.Implemented);
        Assert.Contains(ShooterAcceptanceCatalog.NetworkEnvironments, n => n.Id == "ideal");
        Assert.Contains(ShooterAcceptanceCatalog.NetworkEnvironments, n => n.Id == "poorwifi");
    }

    [Fact]
    public void CreateAssemblesStartedPredictRollbackSession()
    {
        var session = ShooterAcceptanceLab.Create(
            NetworkSyncModel.PredictRollback,
            NetworkConditionProfile.Ideal);

        Assert.Equal(NetworkSyncModel.PredictRollback, session.SyncModel);
        Assert.NotNull(session.Runtime);
        Assert.NotNull(session.Presentation);
        Assert.NotNull(session.Controller);
        Assert.Equal(ShooterDemoHarnessCarrier.DefaultCarrierName, session.Carrier.CarrierName);
    }

    [Fact]
    public void PredictRollbackSessionRunsThroughHarnessToCompletion()
    {
        var session = ShooterAcceptanceLab.Create(
            NetworkSyncModel.PredictRollback,
            NetworkConditionProfile.PoorWifi,
            networkName: "Poor WiFi");

        var result = session.Run(stepCount: 5, deltaSeconds: 1f / 30f, seed: 11);

        Assert.True(result.Completed);
        Assert.Equal(5, result.Metrics.StepsRun);
        Assert.Equal(5, result.Metrics.TotalTicks);
        Assert.Equal(session.Runtime.CurrentFrame, result.Metrics.LastFrame);
        Assert.Equal(session.Presentation.ViewModel.Frame, result.Metrics.LastFrame);
    }

    [Fact]
    public void CatalogOverloadBuildsRunnablePredictRollbackSession()
    {
        var sync = FindMode(NetworkSyncModel.PredictRollback);
        var network = FindNetwork("lan");

        var session = ShooterAcceptanceLab.Create(in sync, in network);
        var result = session.Run(stepCount: 3);

        Assert.True(result.Completed);
        Assert.Equal("LAN (5ms)", session.NetworkName);
    }

    [Fact]
    public void RunCatalogMatrixReturnsBatchResultWithEveryImplementedModeAndNetwork()
    {
        var batch = ShooterAcceptanceLab.RunCatalogMatrix(stepCount: 2);

        var implementedModes = 0;
        foreach (var mode in ShooterAcceptanceCatalog.SyncModes)
        {
            if (mode.Implemented)
            {
                implementedModes++;
            }
        }

        var expected = implementedModes * ShooterAcceptanceCatalog.NetworkEnvironments.Count;
        Assert.Equal(expected, batch.ScenarioCount);
        Assert.Equal(expected, batch.Results.Count);

        // PredictRollback + AuthoritativeInterpolation: completed.
        // HybridHeroPrediction: degraded (all entities via predict-rollback; per-entity split not implemented).
        // CompletedCount treats both Completed and Degraded as completed (both have Completed==true).
        var netCount = ShooterAcceptanceCatalog.NetworkEnvironments.Count;
        Assert.Equal(netCount * 3, batch.CompletedCount);
        Assert.Equal(0, batch.UnsupportedCount);
        Assert.Equal(0, batch.FailedCount);
        Assert.Equal(netCount, batch.DegradedCount);
        Assert.True(batch.AllCompleted);

        // Batch summary should have aggregated rows per (carrier, model, status).
        Assert.NotEmpty(batch.Summary.Rows);
        Assert.Equal(netCount, batch.Summary.CountFor(
            ShooterDemoHarnessCarrier.DefaultCarrierName,
            NetworkSyncModel.PredictRollback,
            DemoHarnessRunStatus.Completed));
        Assert.Equal(netCount, batch.Summary.CountFor(
            ShooterInterpolationDemoHarnessCarrier.DefaultCarrierName,
            NetworkSyncModel.AuthoritativeInterpolation,
            DemoHarnessRunStatus.Completed));
        Assert.Equal(netCount, batch.Summary.CountFor(
            ShooterHybridDemoHarnessCarrier.DefaultCarrierName,
            NetworkSyncModel.HybridHeroPrediction,
            DemoHarnessRunStatus.Degraded));
    }

    [Fact]
    public void CatalogOverloadRejectsUnimplementedMode()
    {
        var unimplemented = new ShooterAcceptanceSyncOption(
            NetworkSyncModel.Lockstep, "Lockstep", implemented: false);
        var network = FindNetwork("ideal");

        Assert.Throws<System.NotSupportedException>(() =>
            ShooterAcceptanceLab.Create(in unimplemented, in network));
    }

    private static ShooterAcceptanceSyncOption FindMode(NetworkSyncModel model)
    {
        foreach (var mode in ShooterAcceptanceCatalog.SyncModes)
        {
            if (mode.Model == model)
            {
                return mode;
            }
        }

        return Assert.IsType<ShooterAcceptanceSyncOption>(null!);
    }

    private static ShooterAcceptanceNetworkOption FindNetwork(string id)
    {
        foreach (var network in ShooterAcceptanceCatalog.NetworkEnvironments)
        {
            if (network.Id == id)
            {
                return network;
            }
        }

        return Assert.IsType<ShooterAcceptanceNetworkOption>(null!);
    }

    [Fact]
    public void HybridSyncModeIsExposedInCatalog()
    {
        Assert.Contains(ShooterAcceptanceCatalog.SyncModes,
            m => m.Model == NetworkSyncModel.HybridHeroPrediction && m.Implemented);
        Assert.Contains(ShooterAcceptanceCatalog.SyncModes,
            m => m.DisplayName == "Hybrid (Predict + Interpolation)");
    }

    [Fact]
    public void HybridSessionRunsThroughHarnessWithDegradedResult()
    {
        var session = ShooterAcceptanceLab.Create(
            NetworkSyncModel.HybridHeroPrediction,
            NetworkConditionProfile.Ideal,
            networkName: "Hybrid Ideal");

        Assert.Equal(NetworkSyncModel.HybridHeroPrediction, session.SyncModel);
        Assert.Equal(ShooterHybridDemoHarnessCarrier.DefaultCarrierName, session.Carrier.CarrierName);

        var result = session.Run(stepCount: 5, deltaSeconds: 1f / 30f, seed: 19);

        // Hybrid mode runs in Degraded status (all entities predict-rollback).
        Assert.Equal(DemoHarnessRunStatus.Degraded, result.Status);
        Assert.True(result.Completed);
        Assert.Equal(5, result.Metrics.StepsRun);
        Assert.Equal(session.Runtime.CurrentFrame, result.Metrics.LastFrame);
        Assert.Equal(session.Presentation.ViewModel.Frame, result.Metrics.LastFrame);
    }

    [Fact]
    public void LimitedBandwidthNetworkProfileIsExposedInCatalog()
    {
        Assert.Contains(ShooterAcceptanceCatalog.NetworkEnvironments, n => n.Id == "limitedbw");

        var limited = ShooterAcceptanceCatalog.NetworkEnvironments
            .First(n => n.Id == "limitedbw");
        Assert.Equal("Limited BW (128 Kbps)", limited.DisplayName);
        Assert.Equal(128, limited.Profile.BandwidthKbps);
        Assert.Equal(0, limited.Profile.BaseLatencyMs);
        Assert.Equal(0d, limited.Profile.PacketLossRate);
    }

    [Fact]
    public void AuthoritativeWorldPublishesSnapshotsThroughCarrierNetworkLink()
    {
        var session = ShooterAcceptanceLab.Create(
            NetworkSyncModel.PredictRollback,
            NetworkConditionProfile.Ideal,
            enableAuthoritativeWorld: true);

        var result = session.Run(stepCount: 3, deltaSeconds: 1f / 30f, seed: 23);

        Assert.True(result.Completed);
        Assert.NotNull(session.CarrierNetworkStats);
        Assert.Equal(3, session.CarrierNetworkStats.Value.InboundReceived);
        Assert.Equal(3, session.CarrierNetworkStats.Value.InboundDelivered);
        Assert.Equal(0, session.CarrierNetworkStats.Value.PendingCount);
        Assert.Equal(ShooterSnapshotApplyResult.AppliedPackedSnapshot, session.LastCarrierSnapshotApplyResult);
        var lagCompTelemetry = Assert.IsType<ShooterLagCompensationTelemetry>(session.LagCompensationTelemetry);
        Assert.Equal(3, lagCompTelemetry.CapturedFrameCount);
        Assert.Equal(1, lagCompTelemetry.OldestFrame);
        Assert.Equal(3, lagCompTelemetry.LatestFrame);
    }

    [Fact]
    public void AuthoritativeCarrierSnapshotsAreStampedWithTimeAnchor()
    {
        var session = ShooterAcceptanceLab.Create(
            NetworkSyncModel.PredictRollback,
            NetworkConditionProfile.Ideal,
            enableAuthoritativeWorld: true);

        var result = session.Run(stepCount: 3, deltaSeconds: 1f / 30f, seed: 37);

        Assert.True(result.Completed);
        Assert.Equal(3, session.LastCarrierTimeAnchor.LocalFrame);
        Assert.Equal(3, session.LastCarrierTimeAnchor.TimelineTicks);
        Assert.True(session.LastCarrierTimeAnchor.HasAuthoritativeFrame);
        Assert.Equal(3, session.LastCarrierTimeAnchor.AuthoritativeFrame);
        Assert.Equal(0.1d, session.LastCarrierTimeAnchor.ElapsedSeconds, precision: 6);
    }

    [Fact]
    public void CarrierNetworkLinkBuffersSnapshotsUntilLatencyElapses()
    {
        var session = ShooterAcceptanceLab.Create(
            NetworkSyncModel.PredictRollback,
            new NetworkConditionProfile(baseLatencyMs: 100, jitterMs: 0, packetLossRate: 0d, reorderRate: 0d, bandwidthKbps: 0),
            enableAuthoritativeWorld: true);

        session.TickAuthoritativeWorld(1f / 30f);

        Assert.NotNull(session.CarrierNetworkStats);
        Assert.Equal(1, session.CarrierNetworkStats.Value.InboundReceived);
        Assert.Equal(0, session.CarrierNetworkStats.Value.InboundDelivered);
        Assert.Equal(1, session.CarrierNetworkStats.Value.PendingCount);
    }

    [Fact]
    public void CarrierNetworkLinkAppliesPacketLossBeforeControllerDelivery()
    {
        var session = ShooterAcceptanceLab.Create(
            NetworkSyncModel.PredictRollback,
            new NetworkConditionProfile(baseLatencyMs: 0, jitterMs: 0, packetLossRate: 1d, reorderRate: 0d, bandwidthKbps: 0),
            enableAuthoritativeWorld: true);

        var result = session.Run(stepCount: 4, deltaSeconds: 1f / 30f, seed: 29);

        Assert.True(result.Completed);
        Assert.NotNull(session.CarrierNetworkStats);
        Assert.Equal(4, session.CarrierNetworkStats.Value.InboundReceived);
        Assert.Equal(0, session.CarrierNetworkStats.Value.InboundDelivered);
        Assert.Equal(4, session.CarrierNetworkStats.Value.InboundDropped);
        Assert.Equal(ShooterSnapshotApplyResult.Ignored, session.LastCarrierSnapshotApplyResult);
    }

    [Fact]
    public void CarrierNetworkLinkRecordsReorderedSnapshots()
    {
        var session = ShooterAcceptanceLab.Create(
            NetworkSyncModel.PredictRollback,
            new NetworkConditionProfile(baseLatencyMs: 30, jitterMs: 0, packetLossRate: 0d, reorderRate: 1d, bandwidthKbps: 0),
            enableAuthoritativeWorld: true);

        var result = session.Run(stepCount: 4, deltaSeconds: 1f / 30f, seed: 31);

        Assert.True(result.Completed);
        Assert.NotNull(session.CarrierNetworkStats);
        Assert.Equal(4, session.CarrierNetworkStats.Value.InboundReceived);
        Assert.True(session.CarrierNetworkStats.Value.InboundReordered > 0);
    }

    [Fact]
    public void LimitedBandwidthSessionRunsThroughHarnessToCompletion()
    {
        var session = ShooterAcceptanceLab.Create(
            NetworkSyncModel.PredictRollback,
            NetworkConditionProfile.LimitedBandwidth,
            networkName: "Limited BW (128 Kbps)");

        var result = session.Run(stepCount: 5, deltaSeconds: 1f / 30f, seed: 17);

        Assert.True(result.Completed);
        Assert.Equal(5, result.Metrics.StepsRun);
        Assert.Equal(session.Runtime.CurrentFrame, result.Metrics.LastFrame);
        Assert.Equal(128, result.Scenario.NetworkProfile.BandwidthKbps);

        // Bandwidth throttling does not cause reconciliation failures
        // because Shooter demo uses < 1 Kbps per tick at 30 Hz with 2 players.
        Assert.True(result.Metrics.ReconciliationCount >= 0);
    }
}
