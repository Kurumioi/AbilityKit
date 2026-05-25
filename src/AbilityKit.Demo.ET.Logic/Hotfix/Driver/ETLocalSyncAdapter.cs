using System;
using AbilityKit.Ability.FrameSync;
using AbilityKit.Ability.Host;
using AbilityKit.Demo.Moba.Share;

namespace ET.Logic
{
    /// <summary>
    /// ET Local Sync Adapter (Lockstep mode)
    ///
    /// Placeholder implementation for local-only sync
    /// TODO: Implement full local sync logic
    /// </summary>
    public sealed class ETLocalSyncAdapter : IETLocalSyncAdapter
    {
        private ETMobaBattleDriver _driver;
        private bool _isDisposed;

        // IETBattleSyncAdapter implementation
        public SyncMode Mode => SyncMode.Lockstep;
        public int CurrentFrame => _driver?.CurrentFrame ?? 0;
        public double LogicTimeSeconds => _driver?.LogicTimeSeconds ?? 0;
        public double RenderTimeSeconds => 0;
        public int LocalActorId => _driver?.Plan.PlayerId ?? 0;
        public bool IsConnected => true;

        public event Action<int, double> OnFrameSync;

        public void Initialize(ETMobaBattleDriver driver, in BattleStartPlan plan)
        {
            _driver = driver;
        }

        public void Tick(float deltaTime)
        {
            OnFrameSync?.Invoke(CurrentFrame, LogicTimeSeconds);
        }

        public void SubmitInput(PlayerInputCommand input)
        {
            // Local sync - input goes directly to driver
        }

        public ActorStateSnapshotData[] GetAllActorStates() => Array.Empty<ActorStateSnapshotData>();

        // IETBattleSyncAdapter - unused methods
        public void AttachToSession(object session) { }
        public void DetachFromSession() { }

        // IDisposable implementation
        public void Dispose()
        {
            if (_isDisposed)
                return;
            _isDisposed = true;
            _driver = null;
        }
    }
}
