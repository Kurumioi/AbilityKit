using AbilityKit.Game.View.Flow;

namespace AbilityKit.Game.Flow
{
    internal sealed class MobaFlowConfiguration
    {
        private MobaFlowConfiguration(
            PhaseStateMachineSpec<MobaRootState, MobaRootEvent> rootMachine,
            PhaseStateMachineSpec<MobaBattleState, MobaBattleEvent> battleMachine,
            PhaseStateFeatureSpec bootFeatures,
            PhaseStateFeatureSpec lobbyFeatures,
            PhaseStateFeatureSpec battlePrepareFeatures,
            PhaseStateFeatureSpec battleConnectFeatures,
            PhaseStateFeatureSpec battleCreateOrJoinWorldFeatures,
            PhaseStateFeatureSpec battleLoadAssetsFeatures,
            PhaseStateFeatureSpec battleInMatchFeatures,
            PhaseStateFeatureSpec battleEndFeatures)
        {
            RootMachine = rootMachine;
            BattleMachine = battleMachine;
            BootFeatures = bootFeatures;
            LobbyFeatures = lobbyFeatures;
            BattlePrepareFeatures = battlePrepareFeatures;
            BattleConnectFeatures = battleConnectFeatures;
            BattleCreateOrJoinWorldFeatures = battleCreateOrJoinWorldFeatures;
            BattleLoadAssetsFeatures = battleLoadAssetsFeatures;
            BattleInMatchFeatures = battleInMatchFeatures;
            BattleEndFeatures = battleEndFeatures;
        }

        public PhaseStateMachineSpec<MobaRootState, MobaRootEvent> RootMachine { get; }
        public PhaseStateMachineSpec<MobaBattleState, MobaBattleEvent> BattleMachine { get; }
        public PhaseStateFeatureSpec BootFeatures { get; }
        public PhaseStateFeatureSpec LobbyFeatures { get; }
        public PhaseStateFeatureSpec BattlePrepareFeatures { get; }
        public PhaseStateFeatureSpec BattleConnectFeatures { get; }
        public PhaseStateFeatureSpec BattleCreateOrJoinWorldFeatures { get; }
        public PhaseStateFeatureSpec BattleLoadAssetsFeatures { get; }
        public PhaseStateFeatureSpec BattleInMatchFeatures { get; }
        public PhaseStateFeatureSpec BattleEndFeatures { get; }

        public static MobaFlowConfiguration CreateDefault()
        {
            return new MobaFlowConfiguration(
                BuildRootMachine(),
                BuildBattleMachine(),
                new PhaseStateFeatureSpec("Boot", clearBeforeEnter: true),
                new PhaseStateFeatureSpec("Lobby", clearBeforeEnter: true)
                    .AddFeature("demo_lobby")
                    .AddFeature("root_debug"),
                new PhaseStateFeatureSpec("Battle.Prepare", clearBeforeEnter: true)
                    .AddEnterBeforeAction(MobaFlowActionIds.ResetBattleSessionRuntimeState)
                    .AddFeature("context")
                    .AddFeature("entity")
                    .AddFeature("session"),
                new PhaseStateFeatureSpec("Battle.Connect")
                    .AddFeature("debug_ongui")
                    .AddSwitchFlow(MobaFlowSwitchIds.AdvanceOnConnectEnter),
                new PhaseStateFeatureSpec("Battle.CreateOrJoinWorld")
                    .AddFeature("debug_ongui")
                    .AddSwitchFlow(MobaFlowSwitchIds.AdvanceOnCreateOrJoinWorldEnter),
                new PhaseStateFeatureSpec("Battle.LoadAssets")
                    .AddFeature("debug_ongui")
                    .AddSwitchFlow(MobaFlowSwitchIds.AdvanceOnLoadAssetsEnter),
                new PhaseStateFeatureSpec("Battle.InMatch")
                    .AddFeature("sync")
                    .AddFeature("input")
                    .AddFeature("view")
                    .AddFeature("hud")
                    .AddFeature("debug_ongui"),
                new PhaseStateFeatureSpec("Battle.End", clearBeforeEnter: true)
                    .AddFeature("debug_ongui")
                    .AddEnterAfterAction(MobaFlowActionIds.ReturnLobbyAfterBattleEnd));
        }

        private static PhaseStateMachineSpec<MobaRootState, MobaRootEvent> BuildRootMachine()
        {
            return new PhaseStateMachineSpec<MobaRootState, MobaRootEvent>("Root", 3, 5)
                .AddState(MobaRootState.Boot)
                .AddState(MobaRootState.Lobby)
                .AddState(MobaRootState.Battle)
                .SetStartState(MobaRootState.Boot)
                .AddTransition(MobaRootEvent.BootCompleted, MobaRootState.Boot, MobaRootState.Lobby)
                .AddTransition(MobaRootEvent.EnterBattle, MobaRootState.Lobby, MobaRootState.Battle, MobaFlowConditionIds.BattleEntryReady)
                .AddTransition(MobaRootEvent.EnterBattle, MobaRootState.Boot, MobaRootState.Battle, MobaFlowConditionIds.BattleEntryReady)
                .AddTransition(MobaRootEvent.ReturnLobby, MobaRootState.Battle, MobaRootState.Lobby)
                .AddTransition(MobaRootEvent.ReturnLobby, MobaRootState.Boot, MobaRootState.Lobby);
        }

        private static PhaseStateMachineSpec<MobaBattleState, MobaBattleEvent> BuildBattleMachine()
        {
            return new PhaseStateMachineSpec<MobaBattleState, MobaBattleEvent>("Battle", 6, 9)
                .AddState(MobaBattleState.Prepare)
                .AddState(MobaBattleState.Connect)
                .AddState(MobaBattleState.CreateOrJoinWorld)
                .AddState(MobaBattleState.LoadAssets)
                .AddState(MobaBattleState.InMatch)
                .AddState(MobaBattleState.End)
                .SetStartState(MobaBattleState.Prepare)
                .AddTransition(MobaBattleEvent.PrepareDone, MobaBattleState.Prepare, MobaBattleState.Connect)
                .AddTransition(MobaBattleEvent.Connected, MobaBattleState.Connect, MobaBattleState.CreateOrJoinWorld)
                .AddTransition(MobaBattleEvent.JoinedWorld, MobaBattleState.CreateOrJoinWorld, MobaBattleState.LoadAssets)
                .AddTransition(MobaBattleEvent.LoadingDone, MobaBattleState.LoadAssets, MobaBattleState.InMatch)
                .AddTransition(MobaBattleEvent.Ended, MobaBattleState.Prepare, MobaBattleState.End)
                .AddTransition(MobaBattleEvent.Ended, MobaBattleState.Connect, MobaBattleState.End)
                .AddTransition(MobaBattleEvent.Ended, MobaBattleState.CreateOrJoinWorld, MobaBattleState.End)
                .AddTransition(MobaBattleEvent.Ended, MobaBattleState.LoadAssets, MobaBattleState.End)
                .AddTransition(MobaBattleEvent.Ended, MobaBattleState.InMatch, MobaBattleState.End);
        }
    }
}
