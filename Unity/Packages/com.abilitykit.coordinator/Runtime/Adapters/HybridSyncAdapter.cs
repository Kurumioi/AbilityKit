using System;
using System.Collections.Generic;
using AbilityKit.Ability.World.Abstractions;

namespace AbilityKit.Coordinator
{
    /// <summary>
    /// Hybrid Sync Adapter (Client Prediction Mode)
    ///
    /// Design:
    /// - Client runs prediction locally
    /// - Inputs are sent to server
    /// - Server validates and sends corrections
    /// - Client reconciles prediction with server state
    ///
    /// Use Case:
    /// - Online multiplayer with client-side prediction
    /// - Reduced perceived latency
    /// - Server is still authoritative
    /// </summary>
    public sealed class HybridSyncAdapter : IPredictionSyncAdapter
    {
        private readonly IWorld _world;
        private readonly SessionConfig _config;
        private ISessionCoordinator _coordinator;
        private IBattleDriverHost _driverHost;

        private double _renderTime;
        private int _localPlayerId;
        private bool _isConnected;
        private bool _predictionEnabled;
        private NetworkEndpoint _endpoint;
        private long _roomId;
        private long _playerId;

        // Input buffer for prediction
        private readonly Queue<PlayerInput> _inputBuffer = new();

        // Prediction state
        private int _lastConfirmedFrame;
        private int _predictedFrame;
        private readonly List<EntityState> _confirmedSnapshot = new();

        // Reconciliation
        private bool _needsReconciliation;
        private EntityState[] _serverCorrection;

        // ============== ISyncAdapter Implementation ==============

        public Core.SyncMode Mode => Core.SyncMode.Hybrid;

        public int CurrentFrame => _driverHost?.CurrentFrame ?? _predictedFrame;

        public double LogicTimeSeconds => _driverHost?.LogicTimeSeconds ?? 0;

        public double RenderTimeSeconds => _renderTime;

        public int LocalPlayerId => _localPlayerId;

        // ============== IRemoteSyncAdapter Implementation ==============

        public bool IsConnected => _isConnected;

        // ============== IPredictionSyncAdapter Implementation ==============

        public bool IsPredictionEnabled => _predictionEnabled;

        public int PredictionAheadFrames => _predictionEnabled ? _config.MaxPredictionAheadFrames : 0;

        // ============== Events ==============

        public event Action<int, double> OnFrameSync;
        public event Action<bool> OnConnectionChanged;
        public event Action<EntityState[]> OnServerSnapshot;

        public HybridSyncAdapter(IWorld world, in SessionConfig config)
        {
            _world = world ?? throw new ArgumentNullException(nameof(world));
            _config = config;
            _localPlayerId = config.LocalPlayerId;
            _renderTime = 0;
            _isConnected = false;
            _predictionEnabled = config.EnableClientPrediction;
            _lastConfirmedFrame = 0;
            _predictedFrame = 0;
            _needsReconciliation = false;
        }

        public void Attach(ISessionCoordinator coordinator)
        {
            _coordinator = coordinator;
        }

        public void Attach(ISessionCoordinator coordinator, IBattleDriverHost driverHost)
        {
            _coordinator = coordinator;
            _driverHost = driverHost;
        }

        public void SetDriverHost(IBattleDriverHost driverHost)
        {
            _driverHost = driverHost;
        }

        // ============== IPredictionSyncAdapter Methods ==============

        public void SetPredictionEnabled(bool enabled)
        {
            _predictionEnabled = enabled;
        }

        public void TriggerReconciliation(int confirmedFrame, EntityState[] serverState)
        {
            _serverCorrection = serverState;
            _needsReconciliation = true;
            _lastConfirmedFrame = confirmedFrame;

            _confirmedSnapshot.Clear();
            if (serverState != null)
            {
                _confirmedSnapshot.AddRange(serverState);
            }
        }

        // ============== IRemoteSyncAdapter Methods ==============

        public void Connect(NetworkEndpoint endpoint, long roomId, long playerId)
        {
            _endpoint = endpoint;
            _roomId = roomId;
            _playerId = playerId;
            _localPlayerId = (int)playerId;

            // TODO: Integrate with actual network transport
            _isConnected = true;
            OnConnectionChanged?.Invoke(true);
        }

        public void Disconnect()
        {
            if (!_isConnected)
                return;

            _isConnected = false;
            _inputBuffer.Clear();
            OnConnectionChanged?.Invoke(false);
        }

        // ============== Input Handling ==============

        public void SubmitInput(PlayerInput input)
        {
            lock (_inputBuffer)
            {
                _inputBuffer.Enqueue(input);
            }

            // Local prediction
            if (_predictionEnabled)
            {
                _predictedFrame++;
                // TODO: Apply input to local prediction state
            }

            // Send to server
            if (_isConnected)
            {
                // TODO: Send to server via network transport
            }
        }

        // ============== Tick ==============

        public void Tick(float deltaTime)
        {
            // Update render time
            _renderTime += deltaTime;

            if (!_isConnected)
                return;

            // Process local prediction
            if (_predictionEnabled)
            {
                // TODO: Run local simulation for prediction
            }

            // Check for reconciliation
            if (_needsReconciliation)
            {
                Reconcile();
            }
        }

        private void Reconcile()
        {
            if (_serverCorrection == null || _coordinator == null)
                return;

            // Find and correct discrepancies
            foreach (var serverState in _serverCorrection)
            {
                if (serverState.EntityId == _localPlayerId)
                {
                    // Local player state - check for desync
                    // TODO: Compare with predicted state and correct if needed
                    _lastConfirmedFrame = _predictedFrame;
                    break;
                }
            }

            _needsReconciliation = false;
            _serverCorrection = null;
        }

        /// <summary>
        /// Feed server confirmation (called by network handler)
        /// </summary>
        public void FeedServerConfirmation(int serverFrame, EntityState[] states)
        {
            TriggerReconciliation(serverFrame, states);
            OnServerSnapshot?.Invoke(states);
            OnFrameSync?.Invoke(serverFrame, 0);
        }

        public EntityState[] GetAllEntityStates()
        {
            if (_driverHost != null)
            {
                return _driverHost.GetAllEntityStates();
            }

            if (_needsReconciliation)
            {
                return _confirmedSnapshot.ToArray();
            }

            // TODO: Return predicted snapshot when prediction is implemented
            return _confirmedSnapshot.ToArray();
        }

        public void Dispose()
        {
            Disconnect();
            _coordinator = null;
            _driverHost = null;
            _inputBuffer.Clear();
            _confirmedSnapshot.Clear();
            _serverCorrection = null;

            OnConnectionChanged = null;
            OnFrameSync = null;
            OnServerSnapshot = null;
        }
    }
}
