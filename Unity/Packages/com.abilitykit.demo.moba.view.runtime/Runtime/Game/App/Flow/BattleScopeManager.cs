using System;
using AbilityKit.Ability.Flow;
using AbilityKit.Core.Common.Log;

namespace AbilityKit.Game.Flow
{
    /// <summary>
    /// �?<c>GameFlowDomain</c> 提取�?Battle 作用域管理器（Step 4.7c）�?
    /// 负责 per-battle �?BattleWorldScope 管理、BattleSessionFeature 创建与事件处理�?
    /// session 推进逻辑�?
    /// 纯逻辑，零 Unity 依赖，可独立测试�?
    /// </summary>
    internal sealed class BattleScopeManager
    {
        /// <summary>
        /// BattleScopeManager 需要的回调集合，由 Domain 提供�?
        /// </summary>
        internal sealed class Callbacks
        {
            /// <summary>设置 _battleRequested 标志�?/summary>
            public Action<bool> SetBattleRequested { get; set; }

            /// <summary>入队 Root 事件（用于触�?HFSM 根状态转移）�?/summary>
            public Action<MobaRootEvent> EnqueueRootEvent { get; set; }

            /// <summary>触发 Battle 状态机事件（内部做 null 守卫）�?/summary>
            public Action<MobaBattleEvent> TriggerBattleFsm { get; set; }

            /// <summary>获取当前活跃�?Battle 状态�?/summary>
            public Func<MobaBattleState> GetActiveBattle { get; set; }

            /// <summary>清除瞬�?gateway 连接工厂�?/summary>
            public Action ClearGatewayConnectionFactory { get; set; }

            /// <summary>获取瞬�?gateway 连接工厂（仅�?AttachBattleFeatures 期间有效）�?/summary>
            public Func<Func<BattleStartPlan, AbilityKit.Network.Abstractions.IConnection>> GetGatewayConnectionFactory { get; set; }

            /// <summary>创建 Battle session feature，由 runtime 层提供具体实现。</summary>
            public Func<IBattleBootstrapper, Func<BattleStartPlan, AbilityKit.Network.Abstractions.IConnection>, IBattleSessionFeature> CreateBattleSessionFeature { get; set; }
        }

        private readonly Callbacks _callbacks;
        private readonly BattleWorldScopeHost _battleWorldScope;
        private readonly MobaBattleAdvanceDecider _advanceDecider;
        private readonly ILogSink _log;

        private IBattleSessionFeature _battleSessionFeature;

        internal BattleScopeManager(
            Callbacks callbacks,
            BattleWorldScopeHost battleWorldScope,
            MobaBattleAdvanceDecider advanceDecider,
            ILogSink log)
        {
            _callbacks = callbacks ?? throw new ArgumentNullException(nameof(callbacks));
            _battleWorldScope = battleWorldScope ?? throw new ArgumentNullException(nameof(battleWorldScope));
            _advanceDecider = advanceDecider ?? throw new ArgumentNullException(nameof(advanceDecider));
            _log = log ?? throw new ArgumentNullException(nameof(log));
        }

        // --- Battle 作用域生命周�?---

        public void EnterBattle(IBattleBootstrapper bootstrapper)
        {
            _callbacks.SetBattleRequested(true);
            // per-battle �?bootstrapper 在建 scope 时播种（生命周期�?flow，scope 不接管释放）�?
            // bootstrapper 可为 null（某些进入路径不�?bootstrapper）：null 局不播种，取回�?TryResolve 落空�?null�?
            if (bootstrapper != null)
            {
                _battleWorldScope.BeginBattle(s => s.Seed<IBattleBootstrapper>(bootstrapper));
            }
            else
            {
                _battleWorldScope.BeginBattle();
            }
            _callbacks.EnqueueRootEvent(MobaRootEvent.EnterBattle);
        }

        public void ReturnToBoot()
        {
            _callbacks.SetBattleRequested(false);
            _callbacks.ClearGatewayConnectionFactory();
            _battleWorldScope.EndBattle();
            _callbacks.EnqueueRootEvent(MobaRootEvent.ReturnLobby);
        }

        // --- BattleSessionFeature 工厂 ---

        internal IBattleSessionFeature CreateBattleSessionFeature()
        {
            // bootstrapper �?per-battle scope 取回（EnterBattle 时播种）�?
            // 取不到（null 局未播种）则传 null——与迁移前「_pendingBootstrapper �?null」行为等价�?
            _battleWorldScope.TryResolve<IBattleBootstrapper>(out var bootstrapper);
            _battleSessionFeature = _callbacks.CreateBattleSessionFeature(bootstrapper, _callbacks.GetGatewayConnectionFactory());
            _battleSessionFeature.SessionStarted += OnBattleSessionStarted;
            _battleSessionFeature.FirstFrameReceived += OnBattleFirstFrameReceived;
            _battleSessionFeature.SessionFailed += OnBattleSessionFailed;
            return _battleSessionFeature;
        }

        internal void ClearBattleSessionEvents()
        {
            if (_battleSessionFeature != null)
            {
                _battleSessionFeature.SessionStarted -= OnBattleSessionStarted;
                _battleSessionFeature.FirstFrameReceived -= OnBattleFirstFrameReceived;
                _battleSessionFeature.SessionFailed -= OnBattleSessionFailed;
                _battleSessionFeature = null;
            }
        }

        // --- Session 事件处理 ---

        internal void OnBattleSessionStarted()
        {
            _battleWorldScope.Resolve<IBattleRuntimeState>().SessionStarted = true;
            _log.Info($"[BattleScopeManager] SessionStarted, activeBattle={_callbacks.GetActiveBattle()}");
            var next = _advanceDecider.OnSessionStarted(_callbacks.GetActiveBattle());
            if (next.HasValue) _callbacks.TriggerBattleFsm(next.Value);
        }

        internal void OnBattleFirstFrameReceived()
        {
            _battleWorldScope.Resolve<IBattleRuntimeState>().FirstFrameReceived = true;
            _log.Info($"[BattleScopeManager] FirstFrameReceived, activeBattle={_callbacks.GetActiveBattle()}");
            var next = _advanceDecider.OnFirstFrameReceived(_callbacks.GetActiveBattle());
            if (next.HasValue) _callbacks.TriggerBattleFsm(next.Value);
        }

        internal void OnBattleSessionFailed(Exception ex)
        {
            // runtime state only tracks session start / first frame in current contract

            _log.Error($"[BattleScopeManager] Battle session failed: {ex}");
            var next = _advanceDecider.OnSessionFailed(_callbacks.GetActiveBattle());
            if (next.HasValue) _callbacks.TriggerBattleFsm(next.Value);
        }

        // --- 抽取的推进判�?---

        internal void TryAdvanceOnConnectEnter()
        {
            var state = _battleWorldScope.Resolve<IBattleRuntimeState>();
            var next = _advanceDecider.OnStateEntered(MobaBattleState.Connect, state.SessionStarted, state.FirstFrameReceived);
            if (next.HasValue) _callbacks.TriggerBattleFsm(next.Value);
        }

        internal void TryAdvanceOnCreateOrJoinWorldEnter()
        {
            var state = _battleWorldScope.Resolve<IBattleRuntimeState>();
            var next = _advanceDecider.OnStateEntered(MobaBattleState.CreateOrJoinWorld, state.SessionStarted, state.FirstFrameReceived);
            if (next.HasValue) _callbacks.TriggerBattleFsm(next.Value);
        }

        internal void TryAdvanceOnLoadAssetsEnter()
        {
            var state = _battleWorldScope.Resolve<IBattleRuntimeState>();
            var next = _advanceDecider.OnStateEntered(MobaBattleState.LoadAssets, state.SessionStarted, state.FirstFrameReceived);
            if (next.HasValue) _callbacks.TriggerBattleFsm(next.Value);
        }

        internal void ResetBattleSessionRuntimeState()
        {
            _battleWorldScope.Resolve<IBattleRuntimeState>().Reset();
        }

        internal void ReturnLobbyAfterBattleEnd()
        {
            _callbacks.SetBattleRequested(false);
            _callbacks.ClearGatewayConnectionFactory();
            _callbacks.EnqueueRootEvent(MobaRootEvent.ReturnLobby);
        }
    }
}


