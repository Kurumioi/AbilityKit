using AbilityKit.Demo.Shooter.View;
using AbilityKit.Network.Runtime;
using AbilityKit.Network.Runtime.DemoHarness;
using Xunit;

namespace AbilityKit.Demo.Shooter.Runtime.Tests;

/// <summary>
/// Golden-baseline tests that lock down the expected four-state distribution of the
/// Shooter acceptance matrix. When a sync mode is added, a network environment is added,
/// or an existing combination changes status (e.g. a mode that was Unsupported becomes
/// Degraded), these tests will fail, forcing the developer to consciously update the
/// baseline rather than silently accepting a regression.
/// </summary>
[Collection("ShooterAcceptance")]
public sealed class ShooterAcceptanceMatrixSnapshotTests
{
    [Fact]
    public void FullMatrixSnapshotMatchesExpectedBaseline()
    {
        var batch = ShooterAcceptanceLab.RunCatalogMatrix(stepCount: 2);

        // === Golden baseline ===
        // Catalog: 3 implemented modes × 6 network environments = 18 scenarios.
        // PredictRollback: 6 Completed (ShooterDemoHarnessCarrier supports it).
        // AuthoritativeInterpolation: 6 Completed (ShooterInterpolationDemoHarnessCarrier supports it).
        // HybridHeroPrediction: 6 Completed (ShooterHybridDemoHarnessCarrier requires the dedicated Hybrid controller).

        Assert.Equal(18, batch.ScenarioCount);
        Assert.Equal(18, batch.CompletedCount);
        Assert.Equal(0, batch.DegradedCount);
        Assert.Equal(0, batch.UnsupportedCount);
        Assert.Equal(0, batch.FailedCount);
        Assert.True(batch.AllCompleted);
    }

    [Fact]
    public void BatchSummaryRowsAggregateByCarrierModelAndStatus()
    {
        var batch = ShooterAcceptanceLab.RunCatalogMatrix(stepCount: 2);

        // PredictRollback × 6 Completed + AuthoritativeInterpolation × 6 Completed
        // + HybridHeroPrediction × 6 Completed = 3 summary rows.
        Assert.Equal(3, batch.Summary.Rows.Count);

        // PredictRollback: 6 Completed.
        Assert.Equal(6, batch.Summary.CountFor(
            ShooterDemoHarnessCarrier.DefaultCarrierName,
            NetworkSyncModel.PredictRollback,
            DemoHarnessRunStatus.Completed));

        // AuthoritativeInterpolation: 6 Completed.
        Assert.Equal(6, batch.Summary.CountFor(
            ShooterInterpolationDemoHarnessCarrier.DefaultCarrierName,
            NetworkSyncModel.AuthoritativeInterpolation,
            DemoHarnessRunStatus.Completed));

        // HybridHeroPrediction: 6 Completed.
        Assert.Equal(6, batch.Summary.CountFor(
            ShooterHybridDemoHarnessCarrier.DefaultCarrierName,
            NetworkSyncModel.HybridHeroPrediction,
            DemoHarnessRunStatus.Completed));

        // No other statuses for PredictRollback.
        Assert.Equal(0, batch.Summary.CountFor(
            ShooterDemoHarnessCarrier.DefaultCarrierName,
            NetworkSyncModel.PredictRollback,
            DemoHarnessRunStatus.Failed));
        Assert.Equal(0, batch.Summary.CountFor(
            ShooterDemoHarnessCarrier.DefaultCarrierName,
            NetworkSyncModel.PredictRollback,
            DemoHarnessRunStatus.Unsupported));
        Assert.Equal(0, batch.Summary.CountFor(
            ShooterDemoHarnessCarrier.DefaultCarrierName,
            NetworkSyncModel.PredictRollback,
            DemoHarnessRunStatus.Degraded));
    }

    [Fact]
    public void PerResultMetricsArePopulatedForEveryCompletedScenario()
    {
        var batch = ShooterAcceptanceLab.RunCatalogMatrix(stepCount: 3, deltaSeconds: 1f / 30f);

        // All scenarios complete now that AuthoritativeInterpolation has its own carrier.
        foreach (var result in batch.Results)
        {
            Assert.True(result.Completed);
            Assert.Equal(3, result.Metrics.StepsRun);
            Assert.Equal(3, result.Metrics.TotalTicks);
            Assert.True(result.Metrics.LastFrame > 0);
            Assert.True(result.Metrics.ReconciliationCount >= 0);
            Assert.True(result.Metrics.NetworkStats.PendingCount >= 0);
        }
    }

    [Fact]
    public void SnapshotIsReproducibleWithDeterministicSeed()
    {
        var first = ShooterAcceptanceLab.RunCatalogMatrix(stepCount: 2, seed: 42);
        var second = ShooterAcceptanceLab.RunCatalogMatrix(stepCount: 2, seed: 42);

        Assert.Equal(first.ScenarioCount, second.ScenarioCount);
        Assert.Equal(first.CompletedCount, second.CompletedCount);
        Assert.Equal(first.UnsupportedCount, second.UnsupportedCount);
        Assert.Equal(first.DegradedCount, second.DegradedCount);
        Assert.Equal(first.FailedCount, second.FailedCount);

        // Per-result metrics should be identical across runs.
        for (var i = 0; i < first.Results.Count; i++)
        {
            var a = first.Results[i];
            var b = second.Results[i];
            Assert.Equal(a.Status, b.Status);
            Assert.Equal(a.Metrics.StepsRun, b.Metrics.StepsRun);
            Assert.Equal(a.Metrics.TotalTicks, b.Metrics.TotalTicks);
            Assert.Equal(a.Metrics.LastFrame, b.Metrics.LastFrame);
            Assert.Equal(a.Metrics.ReconciliationCount, b.Metrics.ReconciliationCount);
        }
    }
}
