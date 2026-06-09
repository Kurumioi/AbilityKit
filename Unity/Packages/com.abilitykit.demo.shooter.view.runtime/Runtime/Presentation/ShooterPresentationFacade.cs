using System;
using AbilityKit.Protocol.Shooter;

namespace AbilityKit.Demo.Shooter.View
{
    public sealed class ShooterPresentationFacade
    {
        private readonly ShooterGatewaySnapshotDecoder _gatewayDecoder;
        private readonly ShooterSnapshotViewAdapter _adapter;
        private readonly ShooterSnapshotStream _stream;
        private readonly ShooterReconciliationDiagnosticsStream _diagnosticsStream;

        public ShooterPresentationFacade()
            : this(
                new ShooterGatewaySnapshotDecoder(),
                new ShooterSnapshotViewAdapter(),
                new ShooterSnapshotStream(),
                new ShooterReconciliationDiagnosticsStream())
        {
        }

        public ShooterPresentationFacade(
            ShooterGatewaySnapshotDecoder gatewayDecoder,
            ShooterSnapshotViewAdapter adapter,
            ShooterSnapshotStream stream)
            : this(gatewayDecoder, adapter, stream, new ShooterReconciliationDiagnosticsStream())
        {
        }

        public ShooterPresentationFacade(
            ShooterGatewaySnapshotDecoder gatewayDecoder,
            ShooterSnapshotViewAdapter adapter,
            ShooterSnapshotStream stream,
            ShooterReconciliationDiagnosticsStream diagnosticsStream)
        {
            _gatewayDecoder = gatewayDecoder ?? throw new ArgumentNullException(nameof(gatewayDecoder));
            _adapter = adapter ?? throw new ArgumentNullException(nameof(adapter));
            _stream = stream ?? throw new ArgumentNullException(nameof(stream));
            _diagnosticsStream = diagnosticsStream ?? throw new ArgumentNullException(nameof(diagnosticsStream));
        }

        public ShooterSnapshotViewModel ViewModel => _adapter.ViewModel;

        public ShooterSnapshotStream Snapshots => _stream;

        public ShooterReconciliationDiagnosticsStream ReconciliationDiagnostics => _diagnosticsStream;

        public void PublishReconciliation(in ShooterClientReconciliationResult result)
        {
            _diagnosticsStream.Publish(in result);
        }

        public bool TryApplyGatewayPush(uint opCode, ArraySegment<byte> payload)
        {
            if (!_gatewayDecoder.IsSnapshotPush(opCode))
            {
                return false;
            }

            var snapshot = _gatewayDecoder.Decode(payload);
            ApplyGatewaySnapshot(in snapshot);
            return true;
        }

        public void ApplyGatewaySnapshot(in ShooterGatewaySnapshot snapshot)
        {
            _adapter.ApplyGatewaySnapshot(in snapshot);
            _stream.Publish(_adapter.ViewModel);
        }

        public void ApplyShooterPayload(byte[] payload)
        {
            _adapter.ApplyPayload(payload);
            _stream.Publish(_adapter.ViewModel);
        }

        public void ApplyShooterSnapshot(in ShooterStateSnapshotPayload snapshot)
        {
            _adapter.ApplySnapshot(in snapshot);
            _stream.Publish(_adapter.ViewModel);
        }

        public void Clear()
        {
            _adapter.Clear();
            _stream.Publish(_adapter.ViewModel);
        }
    }
}
