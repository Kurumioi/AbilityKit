#nullable enable

using System;
using AbilityKit.Game.View.Presentation;

namespace AbilityKit.Demo.Shooter.View
{
    public sealed class ShooterSnapshotStream : IViewStream<ShooterSnapshotViewBatch>
    {
        public event Action<ShooterSnapshotViewBatch>? SnapshotApplied;

        event Action<ShooterSnapshotViewBatch>? IViewStream<ShooterSnapshotViewBatch>.BatchApplied
        {
            add => SnapshotApplied += value;
            remove => SnapshotApplied -= value;
        }

        public void Publish(in ShooterSnapshotViewBatch batch)
        {
            SnapshotApplied?.Invoke(batch);
        }
    }
}
