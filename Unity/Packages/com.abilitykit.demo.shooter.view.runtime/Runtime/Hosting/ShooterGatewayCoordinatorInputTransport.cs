#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;
using AbilityKit.Coordinator;

namespace AbilityKit.Demo.Shooter.View.Hosting
{
    /// <summary>
    /// Coordinator transport that forwards accepted Shooter inputs to the existing Gateway submission path.
    /// </summary>
    public sealed class ShooterGatewayCoordinatorInputTransport : IRemoteBattleSyncTransport
    {
        private readonly CoordinatorInputSubmitBridge<ShooterClientInputSubmitResult, ShooterClientGatewayInputSubmitResult> _submitBridge;
        private readonly object _sync = new();
        private bool _connected;

        public ShooterGatewayCoordinatorInputTransport(
            Func<ShooterClientInputSubmitResult, TimeSpan?, CancellationToken, Task<ShooterClientGatewayInputSubmitResult>> submitAsync)
        {
            if (submitAsync == null) throw new ArgumentNullException(nameof(submitAsync));

            _submitBridge = new CoordinatorInputSubmitBridge<ShooterClientInputSubmitResult, ShooterClientGatewayInputSubmitResult>(
                CreateCoordinatorInput,
                BindCoordinatorInput,
                submitAsync);
        }

        public bool IsConnected
        {
            get
            {
                lock (_sync)
                {
                    return _connected;
                }
            }
        }

        public event Action<bool>? OnConnectionChanged;
        public event Action<int, SnapshotEntityState[]>? OnServerSnapshot;
        public event Action<int, SnapshotEntityState[]>? OnServerConfirmation;

        public bool Connect(NetworkEndpoint endpoint, long roomId, long playerId, AbilityKit.Coordinator.Core.SyncMode syncMode)
        {
            SetConnected(true);
            return true;
        }

        public void Disconnect()
        {
            SetConnected(false);
        }

        public void Tick(float deltaTime)
        {
        }

        public bool SubmitInput(PlayerInput input)
        {
            if (!IsConnected)
            {
                return false;
            }

            return _submitBridge.TrySubmit(input);
        }

        public Task<ShooterClientGatewayInputSubmitResult> SubmitAcceptedInputViaCoordinatorAsync(
            SessionCoordinator coordinator,
            ShooterClientInputSubmitResult local,
            TimeSpan? timeout,
            CancellationToken cancellationToken = default)
        {
            if (coordinator == null) throw new ArgumentNullException(nameof(coordinator));

            if (!IsConnected)
            {
                throw new InvalidOperationException("Shooter coordinator input transport is not connected.");
            }

            return _submitBridge.SubmitViaCoordinatorAsync(coordinator, local, timeout, cancellationToken);
        }

        public void Dispose()
        {
            Disconnect();
            OnConnectionChanged = null;
            OnServerSnapshot = null;
            OnServerConfirmation = null;
        }

        private void SetConnected(bool connected)
        {
            var changed = false;
            lock (_sync)
            {
                if (_connected != connected)
                {
                    _connected = connected;
                    changed = true;
                }

                if (!connected)
                {
                    _submitBridge.Reset();
                }
            }

            if (changed)
            {
                OnConnectionChanged?.Invoke(connected);
            }
        }

        private static PlayerInput CreateCoordinatorInput(ShooterClientInputSubmitResult local)
        {
            return new PlayerInput(
                local.RequestedFrame,
                local.Packet.Command.PlayerId,
                local.Packet.OpCode,
                local.Packet.Payload ?? Array.Empty<byte>());
        }

        private static ShooterClientInputSubmitResult BindCoordinatorInput(
            ShooterClientInputSubmitResult local,
            PlayerInput input)
        {
            return local.WithRequestedFrame(input.Frame);
        }
    }
}
