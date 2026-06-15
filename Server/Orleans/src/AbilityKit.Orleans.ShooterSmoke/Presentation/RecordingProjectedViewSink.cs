using AbilityKit.Demo.Shooter.View;
internal sealed class RecordingProjectedViewSink : IShooterProjectedViewSink
{
    public int ApplyCount { get; private set; }

    public int FullSyncApplyCount { get; private set; }

    public ShooterViewProjectionApplyResult LastApplyResult { get; private set; } = ShooterViewProjectionApplyResult.Empty;

    public ShooterViewProjectionApplyResult LastFullSyncApplyResult { get; private set; } = ShooterViewProjectionApplyResult.Empty;

    public void ApplyViewState(
        ShooterViewEntityStore store,
        in ShooterSnapshotViewBatch sourceBatch,
        in ShooterViewProjectionApplyResult applyResult)
    {
        ApplyCount++;
        LastApplyResult = applyResult;
        if (sourceBatch.ShouldReplaceMissingEntities)
        {
            FullSyncApplyCount++;
            LastFullSyncApplyResult = applyResult;
        }
    }

    public void Clear()
    {
        ApplyCount = 0;
        FullSyncApplyCount = 0;
        LastApplyResult = ShooterViewProjectionApplyResult.Empty;
        LastFullSyncApplyResult = ShooterViewProjectionApplyResult.Empty;
    }
}
