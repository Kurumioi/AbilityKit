using System;
using System.Collections.Generic;
using AbilityKit.Ability.World.Abstractions;

namespace AbilityKit.Coordinator
{
    /// <summary>
    /// Remote Sync Adapter (Server Authority Mode)
    ///
    /// Design:
    /// - Server is authoritative for all game state
    /// - Client sends inputs to server
    /// - Client receives snapshots from server and renders
    ///
    /// Use Case:
    /// - Online multiplayer with dedicated server
    /// - Authoritative server model
    /// </summary>
    public sealed class RemoteSyncAdapter : IRemoteSyncAdapter
    {
        private readonly IWorld _world;
        private readonly SessionConfig _config;
        private readonly SessionRuntimePolicy _runtimePolicy;
        private ISessionCoordinator _coordinator;
        private ILogicWorldDriverBridge _driverHost;

        private double _renderTime;
        private int _localPlayerId;
        private IRemoteBattleSyncTransport _transport;
        private NetworkEndpoint _endpoint;
        private long _roomId;
        private long _playerId;

        // Snapshot storage
        private readonly List<SnapshotEntityState> _lastSnapshot = new();

        // ============== ISyncAdapter Implementation ==============

        public Core.SyncMode Mode => _runtimePolicy.EffectiveSyncMode;

        public int CurrentFrame => _driverHost?.CurrentFrame ?? 0;

        public double LogicTimeSeconds => _driverHost?.LogicTimeSeconds ?? 0;

        public double RenderTimeSeconds => _renderTime;

        public int LocalPlayerId => _localPlayerId;

        public event Action<int, double> OnFrameSync;

        // ============== IRemoteSyncAdapter Implementation ==============

        public bool IsConnected => _transport?.IsConnected == true;

        public event Action<bool> OnConnectionChanged;
        public event Action<SnapshotEntityState[]> OnServerSnapshot;

        public RemoteSyncAdapter(IWorld world, in SessionConfig config)
        {
            _world = world ?? throw new ArgumentNullException(nameof(world));
            _config = config;
            _runtimePolicy = config.ResolveRuntimePolicy();
            _localPlayerId = config.LocalPlayerId;
            _renderTime = 0;
            BindTransport(ResolveTransport(world));
        }

        public void Attach(ISessionCoordinator coordinator)
        {
            _coordinator = coordinator;
        }

        public void Attach(ISessionCoordinator coordinator, ILogicWorldDriverBridge driverHost)
        {
            _coordinator = coordinator;
            _driverHost = driverHost;
        }

        public void SetLogicWorldDriver(ILogicWorldDriverBridge driverHost)
        {
            _driverHost = driverHost;
        }

        public void Connect(NetworkEndpoint endpoint, long roomId, long playerId)
        {
            _endpoint = endpoint;
            _roomId = roomId;
            _playerId = playerId;
            _localPlayerId = (int)playerId;

            var connected = _transport.Connect(endpoint, roomId, playerId, _runtimePolicy.EffectiveSyncMode);
            if (!connected)
            {
                OnConnectionChanged?.Invoke(false);
            }
        }

        public void Disconnect()
        {
            _transport?.Disconnect();
        }

        public void Tick(float deltaTime)
        {
            // Update render time
            _renderTime += deltaTime;

            _transport?.Tick(deltaTime);
        }

        public void SubmitInput(PlayerInput input)
        {
            if (_transport?.IsConnected != true)
                return;

            _transport.SubmitInput(input);
        }

        /// <summary>
        /// Feed a snapshot from server (called by network handler).
        /// </summary>
        public void FeedServerSnapshot(int serverFrame, SnapshotEntityState[] states)
        {
            _lastSnapshot.Clear();
            if (states != null)
            {
                _lastSnapshot.AddRange(states);
            }

            OnServerSnapshot?.Invoke(states);
            OnFrameSync?.Invoke(serverFrame, 0);
        }

        public SnapshotEntityState[] GetAllEntityStates()
        {
            if (_driverHost != null)
            {
                return _driverHost.GetAllEntityStates();
            }
            return _lastSnapshot.ToArray();
        }

        public void Dispose()
        {
            Disconnect();
            UnbindTransport();
            _coordinator = null;
            _driverHost = null;
            _lastSnapshot.Clear();

            OnConnectionChanged = null;
            OnFrameSync = null;
            OnServerSnapshot = null;
        }

        private static IRemoteBattleSyncTransport ResolveTransport(IWorld world)
        {
            if (world?.Services != null && world.Services.TryResolve<IRemoteBattleSyncTransport>(out var transport) && transport != null)
            {
                return transport;
            }

            return NullRemoteBattleSyncTransport.Instance;
        }

        private void BindTransport(IRemoteBattleSyncTransport transport)
        {
            _transport = transport ?? NullRemoteBattleSyncTransport.Instance;
            _transport.OnConnectionChanged += HandleConnectionChanged;
            _transport.OnServerSnapshot += FeedServerSnapshot;
        }

        private void UnbindTransport()
        {
            if (_transport == null)
            {
                return;
            }

            _transport.OnConnectionChanged -= HandleConnectionChanged;
            _transport.OnServerSnapshot -= FeedServerSnapshot;
            _transport = NullRemoteBattleSyncTransport.Instance;
        }

        private void HandleConnectionChanged(bool connected)
        {
            OnConnectionChanged?.Invoke(connected);
        }
    }
}
