using System;
using System.Collections.Generic;
using AbilityKit.Ability.FrameSync;
using AbilityKit.Ability.Host;
using AbilityKit.Core.Common.Log;
using AbilityKit.Demo.Moba.Share;

namespace ET.Logic
{
    /// <summary>
    /// State Sync Adapter (Server Authority Mode)
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
    public sealed class ETStateSyncAdapter : IETRemoteSyncAdapter
    {
        // ============== Fields ==============

        private ETMobaBattleDriver _driver;
        private BattleStartPlan _plan;
        private double _renderTime;
        private int _localActorId;
        private bool _isConnected;

        // Server connection info (placeholder - would integrate with ET network)
        private string _host;
        private int _port;
        private long _roomId;
        private long _playerId;

        // Snapshot storage
        private readonly List<ActorStateSnapshotData> _lastSnapshot = new();

        // ============== IETBattleSyncAdapter Implementation ==============

        public SyncMode Mode => SyncMode.StateSync;

        public int CurrentFrame => _lastSnapshot.Count > 0 ? _lastSnapshot[0].ActorId : 0;

        public double LogicTimeSeconds => 0; // Server authoritative

        public double RenderTimeSeconds => _renderTime;

        public int LocalActorId => _localActorId;

        // ============== IETRemoteSyncAdapter Implementation ==============

        public bool IsConnected => _isConnected;

        // ============== Events ==============

        public event Action<bool> OnConnectionChanged;
        public event Action<int, double> OnFrameSync;
        public event Action<ActorStateSnapshotData[]> OnActorStateSnapshot;

        // ============== Initialize ==============

        public void Initialize(ETMobaBattleDriver driver, in BattleStartPlan plan)
        {
            _driver = driver;
            _plan = plan;
            _localActorId = (int)(plan.PlayerId > 0 ? plan.PlayerId : 1);
            _renderTime = 0;
            _isConnected = false;

            Log.Info($"[ETStateSyncAdapter] Initialized: PlayerId={_plan.PlayerId}");
        }

        // ============== Connect/Disconnect ==============

        public void Connect(string host, int port, long roomId, long playerId)
        {
            _host = host;
            _port = port;
            _roomId = roomId;
            _playerId = playerId;
            _localActorId = (int)playerId;

            // TODO: Integrate with ET's network system (NetComponent, Router, etc.)
            SimulateConnection();

            Log.Info($"[ETStateSyncAdapter] Connecting to server: {host}:{port}, Room={roomId}, Player={playerId}");
        }

        public void Disconnect()
        {
            if (!_isConnected)
                return;

            _isConnected = false;
            OnConnectionChanged?.Invoke(false);

            Log.Info("[ETStateSyncAdapter] Disconnected from server");
        }

        private void SimulateConnection()
        {
            _isConnected = true;
            OnConnectionChanged?.Invoke(true);
        }

        // ============== Input ==============

        public void SubmitInput(PlayerInputCommand input)
        {
            if (!_isConnected)
                return;

            // TODO: Send to server via ET network
            Log.Debug($"[ETStateSyncAdapter] Input sent to server: OpCode={input.OpCode}");
        }

        // ============== Tick ==============

        public void Tick(float deltaTime)
        {
            // Update render time
            _renderTime += deltaTime;

            if (!_isConnected)
                return;

            // TODO: Receive snapshots from server
            // For now, simulate snapshot receiving
            ReceiveSimulatedSnapshot();
        }

        private void ReceiveSimulatedSnapshot()
        {
            // TODO: Integrate with ET network to receive actual snapshots
        }

        /// <summary>
        /// Feed a snapshot from server (called by network handler)
        /// </summary>
        public void FeedServerSnapshot(int serverFrame, ActorStateSnapshotData[] states)
        {
            _lastSnapshot.Clear();
            if (states != null)
            {
                _lastSnapshot.AddRange(states);
            }

            // Notify listeners
            OnActorStateSnapshot?.Invoke(states);
            OnFrameSync?.Invoke(serverFrame, 0);
        }

        // ============== State Query ==============

        public ActorStateSnapshotData[] GetAllActorStates()
        {
            return _lastSnapshot.ToArray();
        }

        // ============== IDisposable ==============

        public void Dispose()
        {
            Disconnect();

            _driver = null;
            _lastSnapshot.Clear();

            OnConnectionChanged = null;
            OnFrameSync = null;
            OnActorStateSnapshot = null;

            Log.Info("[ETStateSyncAdapter] Disposed");
        }
    }
}
