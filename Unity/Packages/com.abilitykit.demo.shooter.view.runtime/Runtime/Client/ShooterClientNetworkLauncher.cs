#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;
using AbilityKit.Demo.Shooter.Runtime;
using AbilityKit.Network.Abstractions;
using AbilityKit.Protocol.Shooter;

namespace AbilityKit.Demo.Shooter.View
{
    public sealed class ShooterClientNetworkLauncher : IDisposable
    {
        private readonly IConnection _connection;
        private readonly ShooterRoomGatewayConnection _gatewayConnection;
        private bool _disposed;

        public ShooterClientNetworkLauncher(IConnection connection)
        {
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));
            _gatewayConnection = new ShooterRoomGatewayConnection(_connection);
        }

        public static ShooterClientNetworkLauncher Create(IShooterClientConnectionFactory connectionFactory)
        {
            if (connectionFactory == null)
            {
                throw new ArgumentNullException(nameof(connectionFactory));
            }

            return new ShooterClientNetworkLauncher(connectionFactory.CreateConnection());
        }

        public IConnection Connection => _connection;

        public ShooterRoomGatewayConnection GatewayConnection => _gatewayConnection;

        public bool IsConnected => _connection.IsConnected;

        public void Open(ShooterClientNetworkEndpoint endpoint)
        {
            Open(endpoint.Host, endpoint.Port);
        }

        public void Open(string host, int port)
        {
            ThrowIfDisposed();
            _connection.Open(host, port);
        }

        public void Close()
        {
            if (_disposed)
            {
                return;
            }

            _connection.Close();
        }

        public void Tick(float deltaTime)
        {
            ThrowIfDisposed();
            _connection.Tick(deltaTime);
        }

        public Task<ShooterClientNetworkLaunchResult> CreateReadyStartAndSubscribeAsync(
            ShooterClientNetworkEndpoint endpoint,
            IShooterBattleRuntimePort runtime,
            ShooterPresentationFacade presentation,
            ShooterStartGamePayload startGame,
            string sessionToken,
            ShooterRoomLaunchSpec launchSpec,
            uint playerId,
            int tickRate = ShooterGameplay.DefaultTickRate,
            TimeSpan? timeout = null,
            CancellationToken cancellationToken = default)
        {
            return CreateReadyStartAndSubscribeAsync(
                endpoint.Host,
                endpoint.Port,
                runtime,
                presentation,
                startGame,
                sessionToken,
                launchSpec,
                playerId,
                tickRate,
                timeout,
                cancellationToken);
        }

        public async Task<ShooterClientNetworkLaunchResult> CreateReadyStartAndSubscribeAsync(
            string host,
            int port,
            IShooterBattleRuntimePort runtime,
            ShooterPresentationFacade presentation,
            ShooterStartGamePayload startGame,
            string sessionToken,
            ShooterRoomLaunchSpec launchSpec,
            uint playerId,
            int tickRate = ShooterGameplay.DefaultTickRate,
            TimeSpan? timeout = null,
            CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            OpenIfNeeded(host, port);

            var launcher = new ShooterClientGatewayLauncher(_gatewayConnection);
            var launched = await launcher.CreateReadyStartAndSubscribeAsync(
                runtime,
                presentation,
                startGame,
                sessionToken,
                launchSpec,
                playerId,
                tickRate,
                timeout,
                cancellationToken).ConfigureAwait(false);

            _gatewayConnection.AttachSession(launched.Session);
            return new ShooterClientNetworkLaunchResult(_connection, _gatewayConnection, launched);
        }

        public Task<ShooterClientNetworkLaunchResult> JoinReadyStartAndSubscribeAsync(
            ShooterClientNetworkEndpoint endpoint,
            IShooterBattleRuntimePort runtime,
            ShooterPresentationFacade presentation,
            ShooterStartGamePayload startGame,
            string sessionToken,
            string roomId,
            ShooterRoomLaunchSpec launchSpec,
            uint playerId,
            int tickRate = ShooterGameplay.DefaultTickRate,
            TimeSpan? timeout = null,
            CancellationToken cancellationToken = default)
        {
            return JoinReadyStartAndSubscribeAsync(
                endpoint.Host,
                endpoint.Port,
                runtime,
                presentation,
                startGame,
                sessionToken,
                roomId,
                launchSpec,
                playerId,
                tickRate,
                timeout,
                cancellationToken);
        }

        public async Task<ShooterClientNetworkLaunchResult> JoinReadyStartAndSubscribeAsync(
            string host,
            int port,
            IShooterBattleRuntimePort runtime,
            ShooterPresentationFacade presentation,
            ShooterStartGamePayload startGame,
            string sessionToken,
            string roomId,
            ShooterRoomLaunchSpec launchSpec,
            uint playerId,
            int tickRate = ShooterGameplay.DefaultTickRate,
            TimeSpan? timeout = null,
            CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            OpenIfNeeded(host, port);

            var launcher = new ShooterClientGatewayLauncher(_gatewayConnection);
            var launched = await launcher.JoinReadyStartAndSubscribeAsync(
                runtime,
                presentation,
                startGame,
                sessionToken,
                roomId,
                launchSpec,
                playerId,
                tickRate,
                timeout,
                cancellationToken).ConfigureAwait(false);

            _gatewayConnection.AttachSession(launched.Session);
            return new ShooterClientNetworkLaunchResult(_connection, _gatewayConnection, launched);
        }

        private void OpenIfNeeded(string host, int port)
        {
            if (!_connection.IsConnected)
            {
                _connection.Open(host, port);
            }
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(ShooterClientNetworkLauncher));
            }
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _gatewayConnection.Dispose();
            _connection.Dispose();
        }
    }

    public sealed class ShooterClientNetworkLaunchResult
    {
        public ShooterClientNetworkLaunchResult(
            IConnection connection,
            ShooterRoomGatewayConnection gatewayConnection,
            ShooterClientGatewayLaunchResult gatewayLaunch)
        {
            Connection = connection ?? throw new ArgumentNullException(nameof(connection));
            GatewayConnection = gatewayConnection ?? throw new ArgumentNullException(nameof(gatewayConnection));
            GatewayLaunch = gatewayLaunch ?? throw new ArgumentNullException(nameof(gatewayLaunch));
        }

        public IConnection Connection { get; }

        public ShooterRoomGatewayConnection GatewayConnection { get; }

        public ShooterClientGatewayLaunchResult GatewayLaunch { get; }

        public ShooterClientSession Session => GatewayLaunch.Session;

        public ShooterClientBattleHandle Battle => GatewayLaunch.Battle;

        public ShooterRoomGatewayFlowResult Flow => GatewayLaunch.Flow;
    }
}
