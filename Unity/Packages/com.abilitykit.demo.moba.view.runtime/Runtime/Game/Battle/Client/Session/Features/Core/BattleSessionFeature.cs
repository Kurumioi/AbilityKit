using System;
using AbilityKit.Ability.FrameSync;
using AbilityKit.Game.Battle;
using AbilityKit.Game.Flow.Battle.Replay;
using AbilityKit.Game.Flow.Modules;
using AbilityKit.Network.Abstractions;
using AbilityKit.Network.Runtime;

namespace AbilityKit.Game.Flow
{
    public sealed partial class BattleSessionFeature : IBattleSessionFeature
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        public static bool DebugForceClientHashMismatch { get; set; }
#endif

        private readonly IBattleBootstrapper _bootstrapper;
        private readonly IAbilityKitConnectionRegistry _connectionRegistry;
        private readonly IBattleSessionWorldInstaller _worldInstaller;
        private readonly IBattleSessionTransportFactory _transportFactory;
        private readonly IBattleSessionGatewayConnectionFactory _gatewayConnectionFactory;
        private readonly IBattleSessionGatewayRoomClientFactory _gatewayRoomClientFactory;

        private readonly BattleSessionState _state = new BattleSessionState();
        private readonly BattleSessionHandles _handles = new BattleSessionHandles();

        private readonly SessionOrchestrator _orchestrator;
        private readonly SessionDispatchersController _dispatchers;
        private readonly SessionNetAdapterController _net;
        private readonly SessionReplayController _replayCtrl;
        private readonly SessionPlanController _planCtrl;
        private readonly SessionEventsController _eventsCtrl;
        private readonly TickLoopController _tickLoop;
        private readonly SessionSnapshotRoutingController _snapshotRouting;
        private readonly SessionWorldCatchUpController _worldCatchUp;

#if UNITY_EDITOR
        private static bool _editorPlayModeHookInstalled;
#endif

        public BattleSessionFeature(
            IBattleBootstrapper bootstrapper,
            Func<BattleStartPlan, IConnection> gatewayConnectionFactory = null,
            IAbilityKitConnectionRegistry connectionRegistry = null)
            : this(
                bootstrapper,
                gatewayConnectionFactory,
                connectionRegistry,
                new DefaultBattleSessionWorldInstaller(),
                new DefaultBattleSessionTransportFactory(),
                new DefaultBattleSessionGatewayConnectionFactory(gatewayConnectionFactory),
                new DefaultBattleSessionGatewayRoomClientFactory())
        {
        }

        internal BattleSessionFeature(
            IBattleBootstrapper bootstrapper,
            Func<BattleStartPlan, IConnection> gatewayConnectionFactory,
            IAbilityKitConnectionRegistry connectionRegistry,
            IBattleSessionWorldInstaller worldInstaller,
            IBattleSessionTransportFactory transportFactory = null,
            IBattleSessionGatewayConnectionFactory gatewayRoomConnectionFactory = null,
            IBattleSessionGatewayRoomClientFactory gatewayRoomClientFactory = null)
        {
            _bootstrapper = bootstrapper;
            _connectionRegistry = connectionRegistry ?? new AbilityKitConnectionRegistry();
            _worldInstaller = worldInstaller ?? new DefaultBattleSessionWorldInstaller();
            _transportFactory = transportFactory ?? new DefaultBattleSessionTransportFactory();
            _gatewayConnectionFactory = gatewayRoomConnectionFactory ?? new DefaultBattleSessionGatewayConnectionFactory(gatewayConnectionFactory);
            _gatewayRoomClientFactory = gatewayRoomClientFactory ?? new DefaultBattleSessionGatewayRoomClientFactory();
            _orchestrator = new SessionOrchestrator(_state, _handles, this);
            _dispatchers = new SessionDispatchersController();
            _net = new SessionNetAdapterController();
            _replayCtrl = new SessionReplayController();
            _planCtrl = new SessionPlanController();
            _eventsCtrl = new SessionEventsController();
            _tickLoop = new TickLoopController(_state, _handles, this);
            _snapshotRouting = new SessionSnapshotRoutingController();
            _worldCatchUp = new SessionWorldCatchUpController();
        }

        public BattleLogicSession Session => _session;
        public int LastFrame => _lastFrame;
        public BattleStartPlan Plan => _plan;

        private float GetFixedDeltaSeconds() => _orchestrator.GetFixedDeltaSeconds();

        private void StartSession() => _orchestrator.StartSession();

        private void StopSession() => _orchestrator.StopSession();
    }
}
