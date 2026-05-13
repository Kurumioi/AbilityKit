using System;
using AbilityKit.Demo.Moba.Console.Platform;

namespace AbilityKit.Demo.Moba.Console.Flow
{
    /// <summary>
    /// 战斗流程实现
    /// </summary>
    public sealed class BattleFlow : IBattleFlow, IDisposable
    {
        private readonly PhaseHost _phaseHost;
        private readonly PhaseContext _context;
        private readonly BattleFlowEvents _events;

        public BattleFlow()
        {
            _events = new BattleFlowEvents();
            _context = new PhaseContext();
            _phaseHost = new PhaseHost();

            _phaseHost.Register(new IdlePhase());
            _phaseHost.Register(new PreparePhase());
            _phaseHost.Register(new ConnectPhase());
            _phaseHost.Register(new CreateOrJoinWorldPhase());
            _phaseHost.Register(new LoadAssetsPhase());
            _phaseHost.Register(new InMatchPhase(this, _events));
            _phaseHost.Register(new EndPhase());

            _context.Root = this;

            // 订阅 PhaseHost 的事件
            _phaseHost.PhaseEntered += OnPhaseEnteredFromHost;
            _phaseHost.PhaseExited += OnPhaseExitedFromHost;
        }

        private void OnPhaseEnteredFromHost(string phaseName)
        {
            Platform.Log.Trace($"[TRACE] BattleFlow.OnPhaseEnteredFromHost({phaseName})");
            _events.OnPhaseEntered(phaseName);
        }

        private void OnPhaseExitedFromHost(string phaseName)
        {
            Platform.Log.Trace($"[TRACE] BattleFlow.OnPhaseExitedFromHost({phaseName})");
            _events.OnPhaseExited(phaseName);
        }

        public string CurrentPhase => _phaseHost.CurrentPhase;
        public bool IsRunning => _phaseHost.IsRunning;
        public IBattleFlowEvents Events => _events;

        public void Start()
        {
            Platform.Log.System("[BattleFlow] Starting battle flow...");
            _phaseHost.SetInitialPhase("Prepare");
            _phaseHost.Start(_context);
        }

        public void Stop()
        {
            Platform.Log.System("[BattleFlow] Stopping battle flow...");
            _phaseHost.Stop();
        }

        public void Tick(float deltaTime)
        {
            if (!IsRunning) return;
            _phaseHost.Tick(deltaTime);
        }

        public void EnterBattle()
        {
            Platform.Log.System("[BattleFlow] Entering battle...");
            // AI controls transition directly
        }

        public void ReturnToLobby()
        {
            Platform.Log.System("[BattleFlow] Returning to lobby...");
            if (CurrentPhase == "End" || CurrentPhase == "InMatch")
            {
                _phaseHost.TransitionTo("Prepare");
            }
        }

        public void TransitionTo(string phaseName)
        {
            _phaseHost.TransitionTo(phaseName);
        }

        public void Dispose()
        {
            _phaseHost?.Dispose();
        }
    }
}
