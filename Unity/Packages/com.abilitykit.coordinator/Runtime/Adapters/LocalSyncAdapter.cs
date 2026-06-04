using System;
using System.Collections.Generic;
using System.Diagnostics;
using AbilityKit.Ability.World.Abstractions;

namespace AbilityKit.Coordinator
{
    /// <summary>
    /// Local Sync Adapter (Lockstep Mode)
    ///
    /// Design:
    /// - All clients run the same simulation locally
    /// - Inputs are shared and deterministic
    /// - No server authority required
    ///
    /// Use Case:
    /// - Single player
    /// - LAN multiplayer
    /// - Offline mode
    /// </summary>
    public sealed class LocalSyncAdapter : ILocalSyncAdapter
    {
        private readonly IWorld _world;
        private readonly SessionConfig _config;
        private readonly SessionRuntimePolicy _runtimePolicy;
        private ISessionCoordinator _coordinator;
        private ILogicWorldDriverBridge _driverHost;

        private double _lastTickTime;
        private double _renderTime;
        private int _currentFrame;
        private double _logicTime;
        private readonly List<PlayerInput> _pendingInputs = new();

        // ============== ISyncAdapter Implementation ==============

        public Core.SyncMode Mode => Core.SyncMode.Lockstep;

        public bool IsConnected => true; // Local mode always connected

        public int CurrentFrame => _driverHost?.CurrentFrame ?? _currentFrame;

        public double LogicTimeSeconds => _driverHost?.LogicTimeSeconds ?? _logicTime;

        public double RenderTimeSeconds => _renderTime;

        public int LocalPlayerId => _config.LocalPlayerId;

        public event Action<int, double> OnFrameSync;

        public LocalSyncAdapter(IWorld world, in SessionConfig config)
        {
            _world = world ?? throw new ArgumentNullException(nameof(world));
            _config = config;
            _runtimePolicy = config.ResolveRuntimePolicy();
            _lastTickTime = GetTimeSeconds();
            _renderTime = 0;
            _currentFrame = 0;
            _logicTime = 0;
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

        public void SetDriverHost(ILogicWorldDriverBridge driverHost)
        {
            _driverHost = driverHost;
        }

        public void Tick(float deltaTime)
        {
            // Update render time
            _renderTime += deltaTime;

            // Check if it's time for next logic frame
            double currentTime = GetTimeSeconds();
            double frameInterval = 1.0 / _config.TickRate;

            if (currentTime - _lastTickTime >= frameInterval)
            {
                ProcessLogicFrame(deltaTime);
                _lastTickTime = currentTime;
            }
        }

        public void SubmitInput(PlayerInput input)
        {
            lock (_pendingInputs)
            {
                _pendingInputs.Add(input);
            }
        }

        public SnapshotEntityState[] GetAllEntityStates()
        {
            if (_driverHost != null)
            {
                return _driverHost.GetAllEntityStates();
            }
            return Array.Empty<SnapshotEntityState>();
        }

        public void Dispose()
        {
            _coordinator = null;
            _driverHost = null;
            _pendingInputs.Clear();
            OnFrameSync = null;
        }

        // ============== Private Methods ==============

        private void ProcessLogicFrame(float deltaTime)
        {
            // Flush pending inputs
            PlayerInput[] inputsToSubmit;
            lock (_pendingInputs)
            {
                inputsToSubmit = _pendingInputs.ToArray();
                _pendingInputs.Clear();
            }

            if (!CanDriveLogicWorld(deltaTime))
            {
                return;
            }

            // Submit inputs through driver host
            if (_driverHost != null && inputsToSubmit.Length > 0)
            {
                _driverHost.SubmitInputs(inputsToSubmit);
            }

            if (_driverHost != null)
            {
                if (!_driverHost.IsRunning)
                {
                    _driverHost.Start();
                }

                _driverHost.AdvanceFrame(deltaTime);
            }
            else
            {
                _currentFrame++;
                _logicTime += deltaTime;
            }

            // Trigger frame sync event
            OnFrameSync?.Invoke(CurrentFrame, LogicTimeSeconds);
        }

        private bool CanDriveLogicWorld(float deltaTime)
        {
            if (_world?.Services != null && _world.Services.TryResolve<ILogicWorldDriveGate>(out var gate) && gate != null)
            {
                return gate.CanDriveLogicWorld(deltaTime);
            }

            return !_runtimePolicy.RequireLogicWorldDriveGate;
        }

        private static double GetTimeSeconds()
        {
            return Stopwatch.GetTimestamp() / (double)Stopwatch.Frequency;
        }
    }
}
