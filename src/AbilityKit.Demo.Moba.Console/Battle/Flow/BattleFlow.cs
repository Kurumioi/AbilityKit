using System;
using AbilityKit.Demo.Moba.Console.Platform;

namespace AbilityKit.Demo.Moba.Console.Battle.Flow
{
    /// <summary>
    /// 战斗流程实现
    /// 使用 PhaseHost 管理阶段切换
    /// 实现 IFeatureContext 接口，提供给 Phase 中的 Features 使用
    /// </summary>
    public sealed class BattleFlow : IBattleFlow, IFeatureContext, IDisposable
    {
        private readonly PhaseHost _phaseHost;
        private readonly PhaseContext _context;
        private readonly BattleFlowEvents _events;
        private Context.ConsoleBattleContext? _battleContext;

        public BattleFlow()
        {
            _events = new BattleFlowEvents();
            _context = new PhaseContext();
            _phaseHost = new PhaseHost();

            // 注册所有阶段
            _phaseHost.Register(new IdlePhase());
            _phaseHost.Register(new PreparePhase());
            _phaseHost.Register(new ConnectPhase());
            _phaseHost.Register(new CreateOrJoinWorldPhase());
            _phaseHost.Register(new LoadAssetsPhase());
            _phaseHost.Register(new InMatchPhase(this, _events));
            _phaseHost.Register(new EndPhase());

            // 订阅 PhaseHost 的事件
            _phaseHost.PhaseEntered += OnPhaseEnteredFromHost;
            _phaseHost.PhaseExited += OnPhaseExitedFromHost;
        }

        /// <summary>
        /// 设置战斗上下文
        /// </summary>
        public void SetBattleContext(Context.ConsoleBattleContext context)
        {
            _battleContext = context;
            _context.Root = this;  // 将 BattleFlow 自身作为 Root
        }

        /// <summary>
        /// 获取 InMatchPhase 的 FeatureHost 进行配置
        /// </summary>
        public InMatchPhase? GetInMatchPhase()
        {
            return _phaseHost.CurrentPhase == "InMatch"
                ? _phaseHost.GetPhase("InMatch") as InMatchPhase
                : null;
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

        #region IFeatureContext 实现

        public int LastFrame => _battleContext?.LastFrame ?? 0;
        public double LogicTimeSeconds => _battleContext?.LogicTimeSeconds ?? 0d;
        public int LocalActorId => _battleContext?.LocalActorId ?? 0;

        #endregion

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
            _phaseHost.PhaseEntered -= OnPhaseEnteredFromHost;
            _phaseHost.PhaseExited -= OnPhaseExitedFromHost;
            _phaseHost?.Dispose();
        }
    }
}
