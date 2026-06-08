using System;
using AbilityKit.Protocol.Shooter;

namespace AbilityKit.Demo.Shooter.View
{
    public sealed class ShooterSnapshotViewAdapter
    {
        private readonly ShooterSnapshotViewModel _viewModel = new ShooterSnapshotViewModel();

        public ShooterSnapshotViewModel ViewModel => _viewModel;

        public void ApplyPayload(byte[] payload)
        {
            if (payload == null) throw new ArgumentNullException(nameof(payload));

            var snapshot = ShooterStateSnapshotCodec.Deserialize(payload);
            ApplySnapshot(in snapshot);
        }

        public void ApplySnapshot(in ShooterStateSnapshotPayload snapshot)
        {
            _viewModel.Apply(in snapshot);
        }

        public void ApplyGatewaySnapshot(in ShooterGatewaySnapshot snapshot)
        {
            _viewModel.Apply(in snapshot);
        }

        public void Clear()
        {
            _viewModel.Clear();
        }
    }
}
