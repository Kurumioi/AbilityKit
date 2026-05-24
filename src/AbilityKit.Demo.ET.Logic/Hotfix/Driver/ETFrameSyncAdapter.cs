using System;
using System.Collections.Generic;
using AbilityKit.Ability.FrameSync;
using AbilityKit.Ability.Host;
using AbilityKit.Ability.World.Abstractions;
using AbilityKit.Core.Common.Log;
using AbilityKit.Demo.Moba.Share;

namespace ET.Logic
{
    /// <summary>
    /// Frame Sync Adapter (Lockstep Mode)
    ///
    /// Design:
    /// - All clients run the same simulation locally
    /// - Inputs are shared and deterministic
    /// - No server authority required
    ///
    /// Use Case:
    /// - Console Demo
    /// - LAN multiplayer
    /// - Single-player mode
    /// </summary>
    public sealed class ETFrameSyncAdapter : IETLocalSyncAdapter
    {
        // ============== Fields ==============

        private ETMobaBattleDriver _driver;
        private BattleStartPlan _plan;
        private double _lastTickTime;
        private double _renderTime;

        private readonly List<PlayerInputCommand> _pendingInputs = new();

        // ============== IETBattleSyncAdapter Implementation ==============

        public SyncMode Mode => SyncMode.Lockstep;

        public bool IsConnected => true; // Local mode always connected

        public int CurrentFrame => _driver?.CurrentFrame ?? 0;

        public double LogicTimeSeconds => _driver?.LogicTimeSeconds ?? 0;

        public double RenderTimeSeconds => _renderTime;

        public int LocalActorId => (int)(_plan.PlayerId > 0 ? _plan.PlayerId : 1);

        // ============== Events ==============

        public event Action<int, double> OnFrameSync;

        // ============== Initialize ==============

        public void Initialize(ETMobaBattleDriver driver, in BattleStartPlan plan)
        {
            _driver = driver;
            _plan = plan;
            _lastTickTime = GetCurrentTimeSeconds();
            _renderTime = 0;

            Log.Info($"[ETFrameSyncAdapter] Initialized: TickRate={plan.TickRate}, PlayerId={_plan.PlayerId}");
        }

        // ============== Tick ==============

        public void Tick(float deltaTime)
        {
            if (_driver == null)
                return;

            // Update render time (always runs, independent of logic)
            _renderTime += deltaTime;

            // Check if it's time for next logic frame
            double currentTime = GetCurrentTimeSeconds();
            double frameInterval = 1.0 / _driver.TickRate;

            if (currentTime - _lastTickTime >= frameInterval)
            {
                ProcessLogicFrame(deltaTime);
                _lastTickTime = currentTime;
            }
        }

        private void ProcessLogicFrame(float deltaTime)
        {
            if (_driver == null)
                return;

            // Flush pending inputs
            List<PlayerInputCommand> inputsToSubmit;
            lock (_pendingInputs)
            {
                inputsToSubmit = new List<PlayerInputCommand>(_pendingInputs);
                _pendingInputs.Clear();
            }

            // Submit inputs to driver
            if (inputsToSubmit.Count > 0)
            {
                var frame = new FrameIndex(_driver.CurrentFrame + 1);
                _driver.SubmitInputs(frame, inputsToSubmit);
            }

            // Trigger frame sync event
            OnFrameSync?.Invoke(_driver.CurrentFrame, _driver.LogicTimeSeconds);
        }

        // ============== Input ==============

        public void SubmitInput(PlayerInputCommand input)
        {
            lock (_pendingInputs)
            {
                _pendingInputs.Add(input);
            }

            Log.Debug($"[ETFrameSyncAdapter] Input queued: OpCode={input.OpCode}, Frame={input.Frame}");
        }

        // ============== State Query ==============

        public ActorStateSnapshotData[] GetAllActorStates()
        {
            // TODO: Query from ECS world when integration is complete
            return Array.Empty<ActorStateSnapshotData>();
        }

        // ============== IDisposable ==============

        public void Dispose()
        {
            _driver = null;
            _pendingInputs.Clear();

            OnFrameSync = null;

            Log.Info("[ETFrameSyncAdapter] Disposed");
        }

        // ============== Utility ==============

        private static double GetCurrentTimeSeconds()
        {
            return (double)Environment.TickCount64 / 1000.0;
        }
    }
}
