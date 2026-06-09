#nullable enable

using System;

namespace AbilityKit.Demo.Shooter.View
{
    public sealed class ShooterSnapshotStream
    {
        public event Action<ShooterSnapshotViewModel>? SnapshotApplied;

        public void Publish(ShooterSnapshotViewModel viewModel)
        {
            if (viewModel == null) throw new ArgumentNullException(nameof(viewModel));
            SnapshotApplied?.Invoke(viewModel);
        }
    }
}
