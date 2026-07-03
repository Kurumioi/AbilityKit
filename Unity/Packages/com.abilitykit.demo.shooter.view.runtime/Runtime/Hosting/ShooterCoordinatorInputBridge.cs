#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;
using AbilityKit.Ability.World.Abstractions;
using AbilityKit.Coordinator;
using AbilityKit.Coordinator.Core;
using AbilityKit.Demo.Shooter.Runtime;

namespace AbilityKit.Demo.Shooter.View.Hosting
{
    /// <summary>
    /// Owns the Shooter remote input path exposed through the generic SessionCoordinator.
    /// </summary>
    public sealed class ShooterCoordinatorInputBridge : IDisposable
    {
        private readonly SessionCoordinator _coordinator;
        private readonly ShooterGatewayCoordinatorInputTransport _transport;
        private bool _disposed;

        private ShooterCoordinatorInputBridge(
            SessionCoordinator coordinator,
            ShooterGatewayCoordinatorInputTransport transport)
        {
            _coordinator = coordinator ?? throw new ArgumentNullException(nameof(coordinator));
            _transport = transport ?? throw new ArgumentNullException(nameof(transport));
        }

        public SessionCoordinator Coordinator => _coordinator;

        public ShooterGatewayCoordinatorInputTransport Transport => _transport;

        public static ShooterCoordinatorInputBridge Create(
            IWorld world,
            ShooterClientNetworkLaunchResult launch,
            ShooterClientNetworkEndpoint endpoint,
            int tickRate)
        {
            if (world == null) throw new ArgumentNullException(nameof(world));
            if (launch == null) throw new ArgumentNullException(nameof(launch));

            var transport = new ShooterGatewayCoordinatorInputTransport(
                (local, timeout, cancellationToken) => launch.Battle.SubmitAcceptedInputToGatewayAsync(local, timeout, cancellationToken));
            var host = new ShooterCoordinatorSessionHost(world, transport);
            var coordinator = new SessionCoordinator();
            var config = CreateConfig(launch.Flow, endpoint, tickRate);
            coordinator.Initialize(config, host);
            coordinator.Start();

            if (coordinator.SyncAdapter is IRemoteSyncAdapter remote)
            {
                remote.Connect(config.ServerEndpoint, config.RoomId, config.LocalPlayerId);
            }
            else
            {
                transport.Connect(config.ServerEndpoint, config.RoomId, config.LocalPlayerId, config.SyncMode);
            }

            return new ShooterCoordinatorInputBridge(coordinator, transport);
        }

        public Task<ShooterClientGatewayInputSubmitResult> SubmitAcceptedInputAsync(
            ShooterClientInputSubmitResult local,
            TimeSpan? timeout = null,
            CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            return _transport.SubmitAcceptedInputViaCoordinatorAsync(_coordinator, local, timeout, cancellationToken);
        }

        public void Tick(float deltaTime)
        {
            if (_disposed)
            {
                return;
            }

            _coordinator.Tick(deltaTime);
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _coordinator.Destroy();
            _transport.Dispose();
        }

        private static SessionConfig CreateConfig(ShooterRoomGatewayFlowResult flow, ShooterClientNetworkEndpoint endpoint, int tickRate)
        {
            var worldId = flow.WorldId > int.MaxValue ? 1 : (int)flow.WorldId;
            var roomId = flow.NumericRoomId > long.MaxValue ? 0L : (long)flow.NumericRoomId;
            var playerId = flow.PlayerId > int.MaxValue ? 0 : (int)flow.PlayerId;

            return new SessionConfig
            {
                SessionId = SessionId.New(),
                MapId = 1,
                WorldId = worldId <= 0 ? 1 : worldId,
                WorldType = ShooterGameplay.WorldType,
                LocalPlayerId = playerId,
                ClientId = playerId,
                SyncMode = SyncMode.StateSync,
                HostMode = HostMode.Client,
                TickRate = tickRate <= 0 ? ShooterGameplay.DefaultTickRate : tickRate,
                RequireLogicWorldDriveGate = true,
                UseCoordinatorSpawnService = false,
                EnableReplayRecording = false,
                EnableReplayPlayback = false,
                EnableClientPrediction = false,
                MaxPredictionAheadFrames = 0,
                ServerEndpoint = new NetworkEndpoint(endpoint.Host, endpoint.Port),
                RoomId = roomId
            };
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(ShooterCoordinatorInputBridge));
            }
        }
    }
}
