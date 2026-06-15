using AbilityKit.Game.View.Presentation;

namespace AbilityKit.Demo.Shooter.View
{
    public interface IShooterViewBinder : IViewBinder<ShooterSnapshotViewBatch>
    {
        new bool InterpolationEnabled { get; set; }
        void Sync(in ShooterSnapshotViewBatch batch);
        new void TickInterpolation(float deltaTime);
        new void Clear();
        new void RebindAll();

        // Bridge the framework's ApplyBatch contract onto the Shooter-specific Sync name.
        void IViewSink<ShooterSnapshotViewBatch>.ApplyBatch(in ShooterSnapshotViewBatch batch)
        {
            Sync(in batch);
        }
    }
}