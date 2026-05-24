using System;
using System.Collections.Generic;
using AbilityKit.Ability.FrameSync;
using AbilityKit.Ability.Host;
using AbilityKit.Core.Common.Log;
using AbilityKit.Demo.Moba.Share;

namespace ET.Logic
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
    public sealed class ETHybridSyncAdapter : IETRemoteSyncAdapter, IETPredictionSyncAdapter
    {
        // ============== Fields ==============

        private ETMobaBattleDriver _driver;
        private BattleStartPlan _plan;
        private double _renderTime;
        private int _localActorId;
        private bool _isConnected;
        private bool _predictionEnabled;

        // Input buffer for prediction
        private readonly Queue<PlayerInputCommand> _inputBuffer = new();

        // Prediction state
        private int _lastConfirmedFrame;
        private int _predictedFrame;
        private readonly List<ActorStateSnapshotData> _confirmedSnapshot = new();
        private readonly List<ActorStateSnapshotData> _predictedSnapshot = new();

        // Reconciliation
        private bool _needsReconciliation;
        private ActorStateSnapshotData[] _serverCorrection;

        // Server connection info
        private string _host;
        private int _port;
        private long _roomId;
        private long _playerId;

        // ============== IETBattleSyncAdapter Implementation ==============

        public SyncMode Mode => SyncMode.Hybrid;

        public int CurrentFrame => _predictedFrame;

        public double LogicTimeSeconds => 0;

        public double RenderTimeSeconds => _renderTime;

        public int LocalActorId => _localActorId;

        // ============== IETRemoteSyncAdapter Implementation ==============

        public bool IsConnected => _isConnected;

        // ============== IETPredictionSyncAdapter Implementation ==============

        public bool IsPredictionEnabled => _predictionEnabled;

        public int PredictionAheadFrames => _predictionEnabled ? 3 : 0;

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
            _predictionEnabled = true; // Enabled by default in Hybrid mode
            _lastConfirmedFrame = 0;
            _predictedFrame = 0;
            _needsReconciliation = false;

            Log.Info($"[ETHybridSyncAdapter] Initialized: PlayerId={_plan.PlayerId}, EnablePrediction={_predictionEnabled}");
        }

        // ============== IETPredictionSyncAdapter Methods ==============

        public void SetPredictionEnabled(bool enabled)
        {
            _predictionEnabled = enabled;
            Log.Info($"[ETHybridSyncAdapter] Prediction {(enabled ? "enabled" : "disabled")}");
        }

        public void TriggerReconciliation(int confirmedFrame, ActorStateSnapshotData[] serverState)
        {
            _serverCorrection = serverState;
            _needsReconciliation = true;
            _lastConfirmedFrame = confirmedFrame;

            // Store confirmed state
            _confirmedSnapshot.Clear();
            if (serverState != null)
            {
                _confirmedSnapshot.AddRange(serverState);
            }

            Log.Info($"[ETHybridSyncAdapter] Reconciliation triggered: ConfirmedFrame={confirmedFrame}");
        }

        // ============== Connect/Disconnect ==============

        public void Connect(string host, int port, long roomId, long playerId)
        {
            _host = host;
            _port = port;
            _roomId = roomId;
            _playerId = playerId;
            _localActorId = (int)playerId;

            // TODO: Integrate with ET's network system
            SimulateConnection();

            Log.Info($"[ETHybridSyncAdapter] Connecting to server: {host}:{port}, Room={roomId}, Player={playerId}");
        }

        public void Disconnect()
        {
            if (!_isConnected)
                return;

            _isConnected = false;
            _inputBuffer.Clear();
            OnConnectionChanged?.Invoke(false);

            Log.Info("[ETHybridSyncAdapter] Disconnected from server");
        }

        private void SimulateConnection()
        {
            _isConnected = true;
            OnConnectionChanged?.Invoke(true);
        }

        // ============== Input ==============

        public void SubmitInput(PlayerInputCommand input)
        {
            lock (_inputBuffer)
            {
                _inputBuffer.Enqueue(input);
            }

            // Local prediction
            if (_driver != null && _predictionEnabled)
            {
                PredictInput(input);
            }

            // Send to server
            SendToServer(input);

            Log.Debug($"[ETHybridSyncAdapter] Input submitted: OpCode={input.OpCode}, PredictedFrame={_predictedFrame}");
        }

        private void PredictInput(PlayerInputCommand input)
        {
            _predictedFrame++;
            // TODO: Apply input to local prediction state
            // This would call into moba.core services for local simulation
        }

        private void SendToServer(PlayerInputCommand input)
        {
            // TODO: Send to server via ET network
        }

        // ============== Tick ==============

        public void Tick(float deltaTime)
        {
            // Update render time
            _renderTime += deltaTime;

            if (!_isConnected)
                return;

            // Process local prediction
            if (_driver != null && _predictionEnabled)
            {
                TickPrediction(deltaTime);
            }

            // Check for reconciliation
            if (_needsReconciliation)
            {
                Reconcile();
            }
        }

        private void TickPrediction(float deltaTime)
        {
            // TODO: Run local simulation for prediction
            // This would call into moba.core services
        }

        private void Reconcile()
        {
            if (_serverCorrection == null || _driver == null)
                return;

            Log.Info($"[ETHybridSyncAdapter] Reconciling: Predicted={_predictedFrame}, Confirmed={_lastConfirmedFrame}");

            // Find and correct discrepancies
            foreach (var serverState in _serverCorrection)
            {
                if (serverState.ActorId == _localActorId)
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

        // ============== Server Snapshot Handling ==============

        /// <summary>
        /// Feed server confirmation (called by network handler)
        /// </summary>
        public void FeedServerConfirmation(int serverFrame, ActorStateSnapshotData[] states)
        {
            TriggerReconciliation(serverFrame, states);

            // Notify listeners
            OnActorStateSnapshot?.Invoke(states);
            OnFrameSync?.Invoke(serverFrame, 0);

            Log.Debug($"[ETHybridSyncAdapter] Server confirmation: Frame={serverFrame}, StateCount={states?.Length ?? 0}");
        }

        // ============== State Query ==============

        public ActorStateSnapshotData[] GetAllActorStates()
        {
            // Return predicted state (local) or confirmed state (reconciling)
            if (_needsReconciliation)
            {
                return _confirmedSnapshot.ToArray();
            }

            // TODO: Return predicted snapshot when prediction is implemented
            return _confirmedSnapshot.ToArray();
        }

        // ============== IDisposable ==============

        public void Dispose()
        {
            Disconnect();

            _driver = null;
            _inputBuffer.Clear();
            _confirmedSnapshot.Clear();
            _predictedSnapshot.Clear();
            _serverCorrection = null;

            OnConnectionChanged = null;
            OnFrameSync = null;
            OnActorStateSnapshot = null;

            Log.Info("[ETHybridSyncAdapter] Disposed");
        }
    }
}
