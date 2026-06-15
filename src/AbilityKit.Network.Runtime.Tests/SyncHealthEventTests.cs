using System;
using AbilityKit.Network.Runtime.Conditioning;
using AbilityKit.Network.Runtime.DemoHarness;
using AbilityKit.Network.Runtime.Sync;
using Xunit;

namespace AbilityKit.Network.Runtime.Tests;

public sealed class SyncHealthEventTests
{
    [Fact]
    public void None_HasNoEvent()
    {
        var none = SyncHealthEvent.None;

        Assert.False(none.HasEvent);
        Assert.Equal(SyncHealthEventKind.None, none.Kind);
        Assert.Equal(SyncHealthSeverity.Info, none.Severity);
        Assert.Equal(0, none.Frame);
        Assert.Equal(0L, none.Value);
    }

    [Fact]
    public void Factories_SetSeverityAndPayload()
    {
        var info = SyncHealthEvent.Info(SyncHealthEventKind.SnapshotReceived, frame: 3, value: 7L);
        var warning = SyncHealthEvent.Warning(SyncHealthEventKind.InterpolationStarved, frame: 4);
        var error = SyncHealthEvent.Error(SyncHealthEventKind.InputRejected, frame: 5, value: 2L);

        Assert.True(info.HasEvent);
        Assert.Equal(SyncHealthSeverity.Info, info.Severity);
        Assert.Equal(SyncHealthEventKind.SnapshotReceived, info.Kind);
        Assert.Equal(3, info.Frame);
        Assert.Equal(7L, info.Value);

        Assert.Equal(SyncHealthSeverity.Warning, warning.Severity);
        Assert.Equal(SyncHealthSeverity.Error, error.Severity);
        Assert.Equal(2L, error.Value);
    }

    [Fact]
    public void NegativeFrame_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new SyncHealthEvent(SyncHealthEventKind.SnapshotReceived, SyncHealthSeverity.Info, frame: -1));
    }

    [Fact]
    public void Equality_ComparesAllFields()
    {
        var a = SyncHealthEvent.Warning(SyncHealthEventKind.RollbackStarted, frame: 2, value: 4L);
        var b = SyncHealthEvent.Warning(SyncHealthEventKind.RollbackStarted, frame: 2, value: 4L);
        var c = SyncHealthEvent.Warning(SyncHealthEventKind.RollbackStarted, frame: 2, value: 5L);

        Assert.True(a == b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
        Assert.True(a != c);
        Assert.False(a.Equals(c));
    }

    [Fact]
    public void StepTelemetry_DefaultsToEmptyHealthEvents()
    {
        var telemetry = new DemoHarnessStepTelemetry(
            tickResult: new SyncTickResult(1, 4, 1u),
            reconciliationReport: SyncReconciliationReport.None,
            networkStats: NewStats());

        Assert.Empty(telemetry.HealthEvents);
    }

    [Fact]
    public void StepTelemetry_CarriesHealthEvents()
    {
        var telemetry = new DemoHarnessStepTelemetry(
            tickResult: new SyncTickResult(1, 4, 1u),
            reconciliationReport: SyncReconciliationReport.None,
            networkStats: NewStats(),
            remoteJitter: 0d,
            acceptedHits: 0,
            rejectedHits: 0,
            SyncHealthEvent.Info(SyncHealthEventKind.SnapshotReceived),
            SyncHealthEvent.Warning(SyncHealthEventKind.InterpolationStarved, frame: 1));

        Assert.Equal(2, telemetry.HealthEvents.Count);
        Assert.Equal(SyncHealthEventKind.SnapshotReceived, telemetry.HealthEvents[0].Kind);
        Assert.Equal(SyncHealthEventKind.InterpolationStarved, telemetry.HealthEvents[1].Kind);
    }

    [Fact]
    public void Metrics_AggregateHealthEventCountsBySeverity()
    {
        var carrier = new HealthEmittingCarrier("Shooter", NetworkSyncModel.PredictRollback);
        var scenario = new DemoHarnessScenario(
            "Shooter",
            NetworkSyncModel.PredictRollback,
            NetworkConditionProfile.Ideal,
            carrierName: "Shooter",
            stepCount: 4,
            deltaSeconds: 0.05f);
        var runner = new DemoHarnessRunner();

        var result = runner.Run(in scenario, carrier);

        Assert.Equal(DemoHarnessRunStatus.Completed, result.Status);
        // Step 0: Info, Step 1: Warning, Step 2: Error, Step 3: Info + None (None ignored).
        Assert.Equal(4, result.Metrics.HealthEventCount);
        Assert.Equal(1, result.Metrics.HealthWarningCount);
        Assert.Equal(1, result.Metrics.HealthErrorCount);
    }

    private static NetworkConditioningStats NewStats()
    {
        return new NetworkConditioningStats(0, 0, 0, 0, 0, 0, 0, 0, 0);
    }

    private sealed class HealthEmittingCarrier : ISyncDemoCarrier
    {
        public HealthEmittingCarrier(string carrierName, NetworkSyncModel syncModel)
        {
            CarrierName = carrierName;
            SyncModel = syncModel;
        }

        public string CarrierName { get; }

        public NetworkSyncModel SyncModel { get; }

        public DemoHarnessStepTelemetry Step(in DemoHarnessStepContext context)
        {
            var step = context.StepIndex + 1;
            var tick = new SyncTickResult(step, step * 4, (uint)step);
            var stats = NewStats();

            return context.StepIndex switch
            {
                0 => new DemoHarnessStepTelemetry(tick, SyncReconciliationReport.None, stats, 0d, 0, 0,
                    SyncHealthEvent.Info(SyncHealthEventKind.SnapshotReceived, context.StepIndex)),
                1 => new DemoHarnessStepTelemetry(tick, SyncReconciliationReport.None, stats, 0d, 0, 0,
                    SyncHealthEvent.Warning(SyncHealthEventKind.InterpolationStarved, context.StepIndex)),
                2 => new DemoHarnessStepTelemetry(tick, SyncReconciliationReport.None, stats, 0d, 0, 0,
                    SyncHealthEvent.Error(SyncHealthEventKind.InputRejected, context.StepIndex)),
                _ => new DemoHarnessStepTelemetry(tick, SyncReconciliationReport.None, stats, 0d, 0, 0,
                    SyncHealthEvent.Info(SyncHealthEventKind.FullSnapshotApplied, context.StepIndex),
                    SyncHealthEvent.None),
            };
        }
    }
}
