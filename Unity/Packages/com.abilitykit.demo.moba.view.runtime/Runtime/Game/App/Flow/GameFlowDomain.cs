using System;
using System.Collections.Generic;
using AbilityKit.Ability.Flow;
using AbilityKit.Core.Configuration;
using AbilityKit.Core.Logging;
using AbilityKit.Game;
using AbilityKit.Game.View.Flow;
using AbilityKit.World.ECS;
using UnityHFSM;
using UnityEngine;

namespace AbilityKit.Game.Flow
{
    public sealed class GameFlowDomain : IMobaFlowActionTarget, IFlowCommandSink, IGameFlowFeatureInstaller
    {
        private readonly IGameHost _entry;
        private readonly ILogSink _log;
        private readonly IFeatureBinder _featureBinder;
        private readonly IPresentationSink _presentationSink;
        private readonly GamePhaseContext _ctx;
        private readonly GameFlowRuntimeAdapter _runtimeAdapter;
        private readonly MobaFlowConfiguration _flowConfig;
        private readonly MobaFlowConditionResolver _conditionResolver;
        private readonly MobaFlowActionExecutor _actionExecutor;
        private readonly MobaFlowSwitchExecutor _switchExecutor;
        private readonly BattleWorldScopeHost _battleWorldScope = new BattleWorldScopeHost();
        private readonly MobaBattleAdvanceDecider _advanceDecider = new MobaBattleAdvanceDecider();
        private readonly FlowConditionContextBuilder _flowConditionContextBuilder;

        public LayeredJsonSettingsStore Settings { get; } = new LayeredJsonSettingsStore();

        private readonly FlowContext _flowContext;
        private readonly FlowEventQueue<MobaRootEvent> _rootEvents;
        private readonly StateMachine<string, MobaRootState, MobaRootEvent> _root;
        private readonly HfsmFlowRunner<string, MobaRootState, MobaRootEvent> _runner;
        private readonly FeatureScheduler _featureScheduler;
        private readonly BattleScopeManager _battleScopeManager;
        private Func<BattleStartPlan, AbilityKit.Network.Abstractions.IConnection> _pendingGatewayConnectionFactory;
        private MobaRootState _activeRoot;
        private MobaBattleState _activeBattle;
        private bool _battleRequested;
        private StateMachine<MobaRootState, MobaBattleState, MobaBattleEvent> _battleFsm;

        public GameFlowDomain(IGameHost entry)
            : this(entry, rootOverride: default, presentationSink: null)
        {
        }

        public GameFlowDomain(IGameHost entry, IEntity rootOverride)
            : this(entry, rootOverride, presentationSink: null)
        {
        }

        public GameFlowDomain(IGameHost entry, IPresentationSink presentationSink)
            : this(entry, rootOverride: default, presentationSink: presentationSink)
        {
        }

        public GameFlowDomain(IGameHost entry, IEntity rootOverride, IPresentationSink presentationSink)
            : this(
                entry,
                rootOverride,
                log: Log.Sink,
                featureBinder: null,
                presentationSink: presentationSink)
        {
        }

        public GameFlowDomain(
            IGameHost entry,
            IEntity rootOverride,
            ILogSink log,
            IFeatureBinder featureBinder = null,
            IPresentationSink presentationSink = null)
        {
            _entry = entry;
            _log = log ?? throw new ArgumentNullException(nameof(log));
            _presentationSink = presentationSink ?? NullPresentationSink.Instance;

            _runtimeAdapter = new GameFlowRuntimeAdapter(rootOverride);
            _featureBinder = featureBinder ?? _runtimeAdapter.FeatureBinder;
            _ctx = new GamePhaseContext(_entry, _runtimeAdapter.Root, _runtimeAdapter.Features, _runtimeAdapter.BattleEntities);
            _flowConfig = MobaFlowConfiguration.CreateDefault();
            _conditionResolver = new MobaFlowConditionResolver();
            _actionExecutor = new MobaFlowActionExecutor();
            _switchExecutor = new MobaFlowSwitchExecutor();
            _battleScopeManager = new BattleScopeManager(
                callbacks: new BattleScopeManager.Callbacks
                {
                    SetBattleRequested = value => _battleRequested = value,
                    EnqueueRootEvent = evt => _rootEvents.Enqueue(evt),
                    TriggerBattleFsm = evt =>
                    {
                        if (_battleFsm != null)
                            _battleFsm.Trigger(evt);
                    },
                    GetActiveBattle = () => _activeBattle,
                    ClearGatewayConnectionFactory = () => _pendingGatewayConnectionFactory = null,
                    GetGatewayConnectionFactory = () => _pendingGatewayConnectionFactory,
                    CreateBattleSessionFeature = (bootstrapper, gatewayConnectionFactory) =>
                        new BattleSessionFeature(bootstrapper, gatewayConnectionFactory)
                },
                battleWorldScope: _battleWorldScope,
                advanceDecider: _advanceDecider,
                log: _log);

            _flowConditionContextBuilder = new FlowConditionContextBuilder(
                callbacks: new FlowConditionContextBuilder.Callbacks
                {
                    GetBattleRequested = () => _battleRequested
                },
                battleWorldScope: _battleWorldScope,
                conditionResolver: _conditionResolver);

            var featurePlanFactory = new MobaFeaturePlanFactory(CreateFeatureFactoryRegistry());

            _featureScheduler = new FeatureScheduler(
                log: _log,
                callbacks: new FeatureScheduler.Callbacks
                {
                    FeatureBinderAttach = f => _featureBinder.AttachFeature(f),
                    FeatureBinderDetach = f => _featureBinder.DetachFeature(f),
                    ClearBattleSessionEvents = _battleScopeManager.ClearBattleSessionEvents,
                    ExecuteFlowAction = (actionId, installedCount) =>
                    {
                        var actionCtx = new MobaFlowActionContext(this, installedCount);
                        if (!_actionExecutor.Execute(actionId, in actionCtx))
                            _log.Error($"[GameFlowDomain] Unknown flow action: {actionId}");
                    },
                    ExecuteSwitchFlow = (switchFlowId, installedCount) =>
                    {
                        var actionCtx = new MobaFlowActionContext(this, installedCount);
                        if (!_switchExecutor.Execute(switchFlowId, in actionCtx))
                            _log.Error($"[GameFlowDomain] Unknown switch flow: {switchFlowId}");
                    }
                },
                ctx: _ctx,
                flowConfig: _flowConfig,
                bootFeaturePlan: featurePlanFactory.CreateBootFeaturePlan(),
                battleFeaturePlan: featurePlanFactory.CreateBattleFeaturePlan());

            _flowContext = new FlowContext();
            _rootEvents = new FlowEventQueue<MobaRootEvent>();

            var smBuilder = new FlowStateMachineBuilder(
                _flowConfig,
                _conditionResolver,
                _log,
                _presentationSink,
                new FlowStateMachineBuilder.FlowStateMachineCallbacks
                {
                    OnRootStateEntered = state => _activeRoot = state,
                    OnBattleStateEntered = state => _log.Info($"[GameFlowDomain] MobaBattleState.{state} entered"),
                    OnBattleStateChanged = state => _activeBattle = state,
                    EnterRootBindings = state => _featureScheduler.RootStateBindings.Enter(state, in _ctx),
                    ExitRootBindings = state => _featureScheduler.RootStateBindings.Exit(state, in _ctx),
                    EnterBattleBindings = state => _featureScheduler.BattleStateBindings.Enter(state, in _ctx),
                    ExitBattleBindings = state => _featureScheduler.BattleStateBindings.Exit(state, in _ctx),
                    EvaluateRootCondition = _flowConditionContextBuilder.EvaluateRootTransitionCondition
                });
            _battleFsm = smBuilder.BuildBattleStateMachine();
            _root = smBuilder.BuildRootStateMachine(_battleFsm);
            _runner = new HfsmFlowRunner<string, MobaRootState, MobaRootEvent>(_flowContext, _root, _rootEvents);
        }

        private MobaFeatureFactoryRegistry CreateFeatureFactoryRegistry()
        {
            return new MobaFeatureFactoryRegistry()
                .Register("boot_menu", (in GamePhaseContext ctx) => new BootMenuOnGUIFeature())
                .Register("root_debug", (in GamePhaseContext ctx) => new RootDebugOnGUIFeature())
                .Register("context", (in GamePhaseContext ctx) => new BattleContextFeature())
                .Register("session", (in GamePhaseContext ctx) => _battleScopeManager.CreateBattleSessionFeature())
                .Register("entity", (in GamePhaseContext ctx) => new BattleEntityFeature())
                .Register("sync", (in GamePhaseContext ctx) => new BattleSyncFeature())
                .Register("input", (in GamePhaseContext ctx) => new BattleInputFeature())
                .Register("view", (in GamePhaseContext ctx) => new BattleViewFeature())
                .Register("hud", (in GamePhaseContext ctx) => new BattleHudFeature())
                .Register("debug_ongui", (in GamePhaseContext ctx) => new BattleDebugOnGUIFeature());
        }

        public MobaRootState CurrentPhase => _activeRoot;
        public MobaBattleState CurrentBattlePhase => _activeBattle;
        MobaRootState IFlowCommandSink.CurrentRootPhase => _activeRoot;
        MobaBattleState IFlowCommandSink.CurrentBattlePhase => _activeBattle;

        public void Start()
        {
            _runner.Start();
            _rootEvents.Enqueue(MobaRootEvent.BootCompleted);

            if (_entry != null)
            {
                _entry.RunCoroutine(UnityJsonSettingsBootstrap.LoadPersistentInto(Settings, RuntimeJsonSettingsCodec.DeserializeFlat));
            }
        }

        public void StartWithPersistentSettingsSync()
        {
            _runner.Start();
            _rootEvents.Enqueue(MobaRootEvent.BootCompleted);
            UnityJsonSettingsBootstrap.LoadPersistentIntoSync(Settings, RuntimeJsonSettingsCodec.DeserializeFlat);
        }

        public bool TrySaveSettingsOverridesToPersistent()
        {
            return UnityJsonSettingsBootstrap.TrySaveOverridesToPersistent(Settings.OverrideValues, RuntimeJsonSettingsCodec.SerializeFlat);
        }

        public void Tick(float deltaTime)
        {
            try
            {
                _runner.Step(deltaTime);
            }
            catch (Exception ex)
            {
                _log.Exception(ex, "[GameFlowDomain] HFSM Step failed");
            }

            _featureScheduler.Tick(in _ctx, deltaTime);
        }

        public void OnGUI()
        {
#if UNITY_EDITOR
            _featureScheduler.OnGUI(in _ctx);
#endif
        }

        public void SwitchTo(IGamePhase next)
        {
            if (next == null) throw new ArgumentNullException(nameof(next));

            if (next is BattlePhase battle)
            {
                EnterBattle((IBattleBootstrapper)null);
                return;
            }

            ReturnToBoot();
        }

        public void Attach(IGamePhaseFeature feature) => _featureScheduler.AttachFeature(feature);
        public void Detach(IGamePhaseFeature feature) => _featureScheduler.DetachFeature(feature);
        public int AttachBootFeatures() => _featureScheduler.AttachBootFeatures();
        public int AttachBattleFeatures(IReadOnlyList<string> featureIds = null) => AttachBattleFeatures(featureIds, gatewayConnectionFactory: null);

        public int AttachBattleFeatures(IReadOnlyList<string> featureIds, Func<BattleStartPlan, AbilityKit.Network.Abstractions.IConnection> gatewayConnectionFactory)
        {
            _pendingGatewayConnectionFactory = gatewayConnectionFactory;
            try
            {
                return _featureScheduler.AttachBattleFeatures(featureIds);
            }
            finally
            {
                _pendingGatewayConnectionFactory = null;
            }
        }

        public void EnterBattle(IBattleBootstrapper bootstrapper) => _battleScopeManager.EnterBattle(bootstrapper);
        public void ReturnToBoot() => _battleScopeManager.ReturnToBoot();
        void IFlowCommandSink.RequestEnterBattle() => EnterBattle((IBattleBootstrapper)null);
        void IFlowCommandSink.RequestBattleEnd()
        {
            if (_battleFsm != null)
            {
                _battleFsm.Trigger(MobaBattleEvent.Ended);
            }
        }
        void IFlowCommandSink.RequestReturnLobby() => ReturnToBoot();

        public void ResetBattleSessionRuntimeState() => _battleScopeManager.ResetBattleSessionRuntimeState();
        public void TryAdvanceOnConnectEnter() => _battleScopeManager.TryAdvanceOnConnectEnter();
        public void TryAdvanceOnCreateOrJoinWorldEnter() => _battleScopeManager.TryAdvanceOnCreateOrJoinWorldEnter();
        public void TryAdvanceOnLoadAssetsEnter() => _battleScopeManager.TryAdvanceOnLoadAssetsEnter();
        public void ReturnLobbyAfterBattleEnd() => _battleScopeManager.ReturnLobbyAfterBattleEnd();

    }
}

