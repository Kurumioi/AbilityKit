using System;
using System.Collections.Generic;
using AbilityKit.Ability.Flow;
using UnityHFSM;
using AbilityKit.Demo.Moba.Console.Battle.Features;
using AbilityKit.Demo.Moba.Console.Battle.Input;

namespace AbilityKit.Demo.Moba.Console.Battle.Game
{
    /// <summary>
    /// Console 游戏流程域
    /// 对齐 Unity GameFlowDomain，管理双层 HFSM 和 Feature 生命周期
    /// </summary>
    public sealed class ConsoleGameFlowDomain : IDisposable
    {
        private readonly ConsoleGameEntry _entry;
        private readonly ConsoleGamePhaseContext _ctx;

        // 事件类型
        private enum RootEvent
        {
            BootCompleted = 0,
            EnterBattle = 1,
            ReturnLobby = 2
        }

        private enum BattleEvent
        {
            PrepareDone = 0,
            Connected = 1,
            JoinedWorld = 2,
            LoadingDone = 3,
            Ended = 4
        }

        // 根状态机
        private readonly StateMachine<string, int, RootEvent> _rootFsm;
        private readonly FlowEventQueue<RootEvent> _rootEvents;
        private readonly HfsmFlowRunner<string, int, RootEvent> _rootRunner;

        // Battle 子状态机
        private StateMachine<int, int, BattleEvent>? _battleFsm;
        private readonly FlowEventQueue<BattleEvent> _battleEvents;
        private HfsmFlowRunner<int, int, BattleEvent>? _battleRunner;

        // Features
        private readonly List<IGamePhaseFeature> _features = new List<IGamePhaseFeature>(16);

        // 状态跟踪
        private int _activeRootState;
        private int _activeBattleState;
        private bool _battleRequested;

        // Bootstrapper
        private IBattleBootstrapper? _pendingBootstrapper;
        private bool _battleSessionStarted;
        private bool _battleFirstFrameReceived;

        public ConsoleGameFlowDomain(ConsoleGameEntry entry)
        {
            _entry = entry ?? throw new ArgumentNullException(nameof(entry));
            _ctx = new ConsoleGamePhaseContext(entry);

            _rootEvents = new FlowEventQueue<RootEvent>();
            _battleEvents = new FlowEventQueue<BattleEvent>();

            _rootFsm = BuildRootStateMachine();
            _rootRunner = new HfsmFlowRunner<string, int, RootEvent>(
                new FlowContext(), _rootFsm, _rootEvents);

            Platform.Log.System("[ConsoleGameFlowDomain] Initialized");
        }

        public int CurrentPhase => _activeRootState;

        /// <summary>
        /// 开始流程
        /// </summary>
        public void Start()
        {
            _rootRunner.Start();
            _rootEvents.Enqueue(RootEvent.BootCompleted);
            Platform.Log.System("[ConsoleGameFlowDomain] Started");
        }

        /// <summary>
        /// 每帧更新
        /// </summary>
        public void Step(float deltaTime)
        {
            // 处理根状态机
            _rootRunner.Step(deltaTime);

            // 处理 Battle 状态机
            _battleRunner?.Step(deltaTime);

            // Tick 所有 Features
            for (int i = 0; i < _features.Count; i++)
            {
                try
                {
                    _features[i].Tick(_ctx, deltaTime);
                }
                catch (Exception ex)
                {
                    Platform.Log.Error($"[ConsoleGameFlowDomain] Feature.Tick failed: {_features[i]?.GetType().FullName}");
                    Platform.Log.Error(ex.Message);
                }
            }
        }

        /// <summary>
        /// 请求进入战斗
        /// </summary>
        public void EnterBattle(IBattleBootstrapper? bootstrapper)
        {
            _battleRequested = true;
            _pendingBootstrapper = bootstrapper;
            _rootEvents.Enqueue(RootEvent.EnterBattle);
            Platform.Log.System("[ConsoleGameFlowDomain] Battle requested");
        }

        /// <summary>
        /// 请求返回大厅
        /// </summary>
        public void ReturnToLobby()
        {
            _battleRequested = false;
            _pendingBootstrapper = null;
            _rootEvents.Enqueue(RootEvent.ReturnLobby);
            Platform.Log.System("[ConsoleGameFlowDomain] Return to lobby requested");
        }

        /// <summary>
        /// 附加 Feature
        /// </summary>
        public void Attach(IGamePhaseFeature feature)
        {
            if (feature == null) throw new ArgumentNullException(nameof(feature));

            _features.Add(feature);
            feature.OnAttach(_ctx);
            Platform.Log.Debug($"[ConsoleGameFlowDomain] Attached feature: {feature.GetType().Name}");
        }

        /// <summary>
        /// 分离 Feature
        /// </summary>
        public void Detach(IGamePhaseFeature feature)
        {
            if (feature == null) return;

            feature.OnDetach(_ctx);
            _features.Remove(feature);
            Platform.Log.Debug($"[ConsoleGameFlowDomain] Detached feature: {feature.GetType().Name}");
        }

        /// <summary>
        /// 清除所有 Features
        /// </summary>
        private void ClearFeatures()
        {
            for (int i = _features.Count - 1; i >= 0; i--)
            {
                Detach(_features[i]);
            }
            _features.Clear();
        }

        /// <summary>
        /// 构建根状态机
        /// </summary>
        private StateMachine<string, int, RootEvent> BuildRootStateMachine()
        {
            const int BootState = 0;
            const int LobbyState = 1;
            const int BattleState = 2;

            var fsm = new StateMachine<string, int, RootEvent>();

            // 使用 Lambda 方式创建 State
            fsm.AddState(BootState, new State<int, RootEvent>(
                onEnter: _ =>
                {
                    _activeRootState = BootState;
                    Platform.Log.Trace("[FSM] Boot entered");
                    ClearFeatures();
                    Attach(new ConsoleBootFeature());
                }
            ));

            fsm.AddState(LobbyState, new State<int, RootEvent>(
                onEnter: _ =>
                {
                    _activeRootState = LobbyState;
                    Platform.Log.Trace("[FSM] Lobby entered");
                    ClearFeatures();
                    Attach(new ConsoleLobbyFeature(this));
                }
            ));

            var battle = BuildBattleStateMachine();
            fsm.AddState(BattleState, battle);

            // 使用 Transition 对象添加触发转换
            fsm.AddTriggerTransition(RootEvent.BootCompleted, new Transition<int>(BootState, LobbyState));
            fsm.AddTriggerTransition(RootEvent.EnterBattle, new Transition<int>(LobbyState, BattleState, _ => _battleRequested));
            fsm.AddTriggerTransition(RootEvent.EnterBattle, new Transition<int>(BootState, BattleState, _ => _battleRequested));
            fsm.AddTriggerTransition(RootEvent.ReturnLobby, new Transition<int>(BattleState, LobbyState));
            fsm.AddTriggerTransition(RootEvent.ReturnLobby, new Transition<int>(BootState, LobbyState));

            fsm.SetStartState(BootState);
            return fsm;
        }

        /// <summary>
        /// 构建 Battle 子状态机
        /// </summary>
        private StateMachine<int, int, BattleEvent> BuildBattleStateMachine()
        {
            const int PrepareState = 0;
            const int ConnectState = 1;
            const int CreateOrJoinWorldState = 2;
            const int LoadAssetsState = 3;
            const int InMatchState = 4;
            const int EndState = 5;

            var fsm = new StateMachine<int, int, BattleEvent>();
            _battleFsm = fsm;

            fsm.AddState(PrepareState, new State<int, BattleEvent>(
                onEnter: _ =>
                {
                    _activeRootState = 2; // Battle
                    Platform.Log.Trace("[FSM] Battle.Prepare entered");
                    ClearFeatures();

                    _battleSessionStarted = false;
                    _battleFirstFrameReceived = false;

                    Attach(new BattleContextFeature());
                    Attach(new ConsoleBattleSessionFeature(_pendingBootstrapper));
                }
            ));

            fsm.AddState(ConnectState, new State<int, BattleEvent>(
                onEnter: _ =>
                {
                    _activeRootState = 2;
                    Platform.Log.Trace("[FSM] Battle.Connect entered");

                    if (_battleSessionStarted || _battleFirstFrameReceived)
                    {
                        _battleEvents.Enqueue(BattleEvent.Connected);
                    }
                }
            ));

            fsm.AddState(CreateOrJoinWorldState, new State<int, BattleEvent>(
                onEnter: _ =>
                {
                    _activeRootState = 2;
                    Platform.Log.Trace("[FSM] Battle.CreateOrJoinWorld entered");

                    if (_battleFirstFrameReceived)
                    {
                        _battleEvents.Enqueue(BattleEvent.JoinedWorld);
                    }
                }
            ));

            fsm.AddState(LoadAssetsState, new State<int, BattleEvent>(
                onEnter: _ =>
                {
                    _activeRootState = 2;
                    Platform.Log.Trace("[FSM] Battle.LoadAssets entered");

                    if (_battleFirstFrameReceived)
                    {
                        _battleEvents.Enqueue(BattleEvent.LoadingDone);
                    }
                }
            ));

            fsm.AddState(InMatchState, new State<int, BattleEvent>(
                onEnter: _ =>
                {
                    _activeRootState = 2;
                    _activeBattleState = InMatchState;
                    Platform.Log.Trace("[FSM] Battle.InMatch entered");
                    ClearFeatures();

                    Attach(new BattleContextFeature());
                    Attach(new ConsoleBattleSessionFeature(_pendingBootstrapper));
                    Attach(new ConsoleInputFeature());
                    Attach(new ConsoleViewFeature());
                    Attach(new ConsoleHudFeature());
                }
            ));

            fsm.AddState(EndState, new State<int, BattleEvent>(
                onEnter: _ =>
                {
                    _activeRootState = 2;
                    _activeBattleState = EndState;
                    Platform.Log.Trace("[FSM] Battle.End entered");
                    ClearFeatures();

                    Attach(new ConsoleBattleEndFeature());
                }
            ));

            // 使用 Transition 对象添加自动转换
            fsm.AddTransition(new Transition<int>(PrepareState, ConnectState, _ => _battleSessionStarted || _battleFirstFrameReceived));
            fsm.AddTransition(new Transition<int>(ConnectState, CreateOrJoinWorldState, _ => _battleFirstFrameReceived));
            fsm.AddTransition(new Transition<int>(CreateOrJoinWorldState, LoadAssetsState, _ => _battleFirstFrameReceived));
            fsm.AddTransition(new Transition<int>(LoadAssetsState, InMatchState, _ => _battleFirstFrameReceived));
            fsm.AddTransition(new Transition<int>(InMatchState, EndState, _ => ShouldEndBattle()));

            fsm.SetStartState(PrepareState);

            _battleRunner = new HfsmFlowRunner<int, int, BattleEvent>(
                new FlowContext(), _battleFsm, _battleEvents);

            _battleRunner.Start();

            return fsm;
        }

        private bool ShouldEndBattle()
        {
            return false;
        }

        public void Dispose()
        {
            ClearFeatures();
            _battleRunner?.Dispose();
            _rootRunner.Dispose();
            Platform.Log.System("[ConsoleGameFlowDomain] Disposed");
        }
    }
}
