#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;
using AbilityKit.Network.Abstractions;
using AbilityKit.Network.Runtime;

namespace AbilityKit.Demo.Shooter.View
{
    public sealed class ShooterRoomGatewayConnection : IShooterRoomGatewayRequestTransport, IDisposable
    {
        private readonly IConnection _connection;
        private readonly RequestClient _requestClient;
        private ShooterClientSession? _session;
        private bool _disposed;

        public ShooterRoomGatewayConnection(IConnection connection)
            : this(connection, null)
        {
        }

        public ShooterRoomGatewayConnection(IConnection connection, ShooterClientSession? session)
        {
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));
            _requestClient = new RequestClient(_connection);
            _session = session;
            _connection.ServerPushReceived += OnServerPushReceived;
        }

        public event Action<uint, ArraySegment<byte>, ShooterSnapshotApplyResult>? SnapshotPushDispatched;

        public ShooterSnapshotApplyResult LastPushResult { get; private set; } = ShooterSnapshotApplyResult.Ignored;

        public void AttachSession(ShooterClientSession session)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
        }

        public Task<ArraySegment<byte>> SendRequestAsync(uint opCode, ArraySegment<byte> payload, TimeSpan? timeout = null, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            return _requestClient.SendRequestAsync(opCode, payload, timeout, cancellationToken);
        }

        private void OnServerPushReceived(uint opCode, ArraySegment<byte> payload)
        {
            if (_disposed)
            {
                return;
            }

            var session = _session;
            var result = session == null
                ? ShooterSnapshotApplyResult.Ignored
                : session.ApplyGatewayPush(opCode, payload);

            LastPushResult = result;
            SnapshotPushDispatched?.Invoke(opCode, payload, result);
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(ShooterRoomGatewayConnection));
            }
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _connection.ServerPushReceived -= OnServerPushReceived;
            _requestClient.Dispose();
        }
    }
}
