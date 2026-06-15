#nullable enable

using AbilityKit.Game.View.Presentation;

namespace AbilityKit.Demo.Shooter.View
{
    public interface IShooterSnapshotViewSink : IViewSink<ShooterSnapshotViewBatch>
    {
        void ApplySnapshot(in ShooterSnapshotViewBatch batch);

        new void Clear();

        // Bridge the framework's ApplyBatch contract onto the Shooter-specific ApplySnapshot name,
        // so existing sinks satisfy IViewSink<ShooterSnapshotViewBatch> without renames.
        void IViewSink<ShooterSnapshotViewBatch>.ApplyBatch(in ShooterSnapshotViewBatch batch)
        {
            ApplySnapshot(in batch);
        }
    }
}
