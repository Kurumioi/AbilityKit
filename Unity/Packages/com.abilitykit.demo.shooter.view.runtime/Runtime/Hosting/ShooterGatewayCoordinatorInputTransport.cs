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
        private readonly Func<ShooterClientInputSubmitResult, TimeSpan?, CancellationToken, Task<ShooterClientGatewayInputSubmitResult>> _submitAsync;
        private readonly object _sync = new();
        private bool _connected;
        private ShooterClientInputSubmitResult _pendingLocal;
        private TimeSpan? _pendingTimeout;
        private CancellationToken _pendingCancellationToken;
        private bool _hasPendingLocal;
        private Task<ShooterClientGatewayInputSubmitResult>? _pendingTask;

        public ShooterGatewayCoordinatorInputTransport(
            Func<ShooterClientInputSubmitResult, TimeSpan?, CancellationToken, Task<ShooterClientGatewayInputSubmitResult>> submitAsync)
        {
            _submitAsync = submitAsync ?? throw new ArgumentNullException(nameof(submitAsync));
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
            ShooterClientInputSubmitResult local;
            lock (_sync)
            {
                if (!_connected || !_hasPendingLocal)
                {
                    return false;
                }

                local = _pendingLocal;
                _hasPendingLocal = false;
            }

            TimeSpan? timeout;
            CancellationToken cancellationToken;
            lock (_sync)
            {
                timeout = _pendingTimeout;
                cancellationToken = _pendingCancellationToken;
                _pendingTimeout = null;
                _pendingCancellationToken = default;
            }

            var frameLocal = local.WithRequestedFrame(input.Frame);
            var task = _submitAsync(frameLocal, timeout, cancellationToken);
            lock (_sync)
            {
                _pendingTask = task;
            }

            return true;
        }

        public Task<ShooterClientGatewayInputSubmitResult> SubmitAcceptedInputViaCoordinatorAsync(
            SessionCoordinator coordinator,
            ShooterClientInputSubmitResult local,
            TimeSpan? timeout,
            CancellationToken cancellationToken = default)
        {
            if (coordinator == null) throw new ArgumentNullException(nameof(coordinator));

            var input = new PlayerInput(
                local.RequestedFrame,
                local.Packet.Command.PlayerId,
                local.Packet.OpCode,
                local.Packet.Payload ?? Array.Empty<byte>());

            lock (_sync)
            {
                if (!_connected)
                {
                    throw new InvalidOperationException("Shooter coordinator input transport is not connected.");
                }

                _pendingLocal = local;
                _pendingTimeout = timeout;
                _pendingCancellationToken = cancellationToken;
                _hasPendingLocal = true;
                _pendingTask = null;
            }

            coordinator.SubmitLocalInput(input);

            Task<ShooterClientGatewayInputSubmitResult>? task;
            lock (_sync)
            {
                task = _pendingTask;
                _pendingTask = null;
                _hasPendingLocal = false;
            }

            return task ?? Task.FromException<ShooterClientGatewayInputSubmitResult>(
                new InvalidOperationException("Coordinator did not submit Shooter input to the Gateway transport."));
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
                    _pendingTimeout = null;
                    _pendingCancellationToken = default;
                    _hasPendingLocal = false;
                    _pendingTask = null;
                }
            }

            if (changed)
            {
                OnConnectionChanged?.Invoke(connected);
            }
        }
    }
}
