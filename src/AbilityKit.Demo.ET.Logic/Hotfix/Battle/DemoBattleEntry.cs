using System;
using System.Threading.Tasks;
using ET.AbilityKit.Demo.ET.Share;
using AbilityKit.Ability.Config;
using AbilityKit.Demo.Moba.Share;

namespace ET.Logic
{
    /// <summary>
    /// ???? - ??????
    /// ?? Moba.Console ? ConsoleBattleBootstrapper
    /// </summary>
    public static class DemoBattleEntry
    {
        private static bool _isRunning;
        private static long _localActorId = 1001;

        /// <summary>
        /// ????
        /// </summary>
        public static async Task StartBattleAsync(Scene scene, long playerId, string playerName)
        {
            _isRunning = true;
            Log.Info($"[DemoBattleEntry] Starting battle for player: {playerName}");

            // ?????????
            var battleComponent = scene.AddComponent<ETBattleComponent>();
            var battleViewComponent = scene.AddComponent<ETBattleViewComponent>();

            // ?????
            battleViewComponent.Initialize();
            battleViewComponent.ShowHelp();

            // ??????
            var plan = new BattleStartPlan(
                mapId: 1,
                worldId: 1,
                playerId: (int)playerId,
                clientId: (int)playerId,
                syncMode: SyncMode.SnapshotAuthority,
                hostMode: HostMode.Local,
                tickRate: 30,
                useGatewayTransport: false,
                enableConfirmedAuthorityWorld: false,
                enableReplayRecording: false,
                enableReplayPlayback: false,
                playerIds: new int[] { (int)playerId });

            // ?????????????
            var textAssetLoader = new ETTextAssetLoader();
            battleComponent.InitializeBattle(plan, textAssetLoader);

            // ?????
            var flowComponent = scene.GetComponent<ETFlowComponent>();
            flowComponent?.StartFlow(FlowPhase.None);

            // ?????????
            var viewSink = new ETViewEventSink(scene);
            battleComponent.ViewSink = viewSink;

            // ??????
            await RunInputLoopAsync(scene);

            Log.Info("[DemoBattleEntry] Battle entry finished");
        }

        /// <summary>
        /// ??????
        /// </summary>
        private static async Task RunInputLoopAsync(Scene scene)
        {
            var battleComponent = scene.GetComponent<ETBattleComponent>();
            var unitComponent = scene.GetComponent<ETUnitComponent>();
            var inputComponent = scene.GetComponent<ETInputComponent>();

            while (_isRunning && battleComponent?.State != BattleState.Ended)
            {
                // ???????
                if (Console.KeyAvailable)
                {
                    var key = Console.ReadKey(true);
                    ProcessInput(scene, key, battleComponent, unitComponent, inputComponent);
                }

                await Task.Delay(16); // ~60 FPS
            }
        }

        /// <summary>
        /// ????
        /// </summary>
        private static void ProcessInput(
            Scene scene,
            ConsoleKeyInfo key,
            ETBattleComponent battleComponent,
            ETUnitComponent unitComponent,
            ETInputComponent inputComponent)
        {
            if (battleComponent == null || unitComponent == null || inputComponent == null)
                return;

            var playerUnit = unitComponent.GetLocalPlayerUnit();
            if (playerUnit == null)
                return;

            float moveStep = 2f;

            switch (key.Key)
            {
                case ConsoleKey.W:
                case ConsoleKey.UpArrow:
                    playerUnit.TargetY += moveStep;
                    inputComponent.SubmitMoveInput(
                        battleComponent.CurrentFrame,
                        playerUnit.ActorId,
                        playerUnit.TargetX,
                        playerUnit.TargetY);
                    break;

                case ConsoleKey.S:
                case ConsoleKey.DownArrow:
                    playerUnit.TargetY -= moveStep;
                    inputComponent.SubmitMoveInput(
                        battleComponent.CurrentFrame,
                        playerUnit.ActorId,
                        playerUnit.TargetX,
                        playerUnit.TargetY);
                    break;

                case ConsoleKey.A:
                case ConsoleKey.LeftArrow:
                    playerUnit.TargetX -= moveStep;
                    inputComponent.SubmitMoveInput(
                        battleComponent.CurrentFrame,
                        playerUnit.ActorId,
                        playerUnit.TargetX,
                        playerUnit.TargetY);
                    break;

                case ConsoleKey.D:
                case ConsoleKey.RightArrow:
                    playerUnit.TargetX += moveStep;
                    inputComponent.SubmitMoveInput(
                        battleComponent.CurrentFrame,
                        playerUnit.ActorId,
                        playerUnit.TargetX,
                        playerUnit.TargetY);
                    break;

                case ConsoleKey.D1:
                case ConsoleKey.D2:
                case ConsoleKey.D3:
                case ConsoleKey.D4:
                    int skillSlot = key.Key - ConsoleKey.D1;
                    inputComponent.SubmitSkillInput(
                        battleComponent.CurrentFrame,
                        playerUnit.ActorId,
                        skillSlot,
                        playerUnit.X + 5f,
                        playerUnit.Y);
                    break;

                case ConsoleKey.Spacebar:
                    inputComponent.SubmitStopInput(battleComponent.CurrentFrame, playerUnit.ActorId);
                    playerUnit.StopMove();
                    break;

                case ConsoleKey.Q:
                    _isRunning = false;
                    Log.Info("[DemoBattleEntry] Quit requested");
                    break;
            }
        }

        /// <summary>
        /// ????
        /// </summary>
        public static void StopBattle()
        {
            _isRunning = false;
            Log.Info("[DemoBattleEntry] Battle stopped");
        }
    }

    /// <summary>
    /// ???? Sink ??
    /// ?????????????
    /// </summary>
    public class ETViewEventSink: IETViewEventSink
    {
        private Scene _scene;

        public ETViewEventSink(Scene scene)
        {
            _scene = scene;
        }

        private Scene GetBattleScene()
        {
            return _scene;
        }

        public void OnActorSpawn(ActorSpawnEvent evt)
        {
            var scene = GetBattleScene();
            if (scene != null)
            {
                var unitViewComponent = scene.GetComponent<ETUnitViewComponent>();
                unitViewComponent?.CreateUnitView(evt);
            }
        }

        public void OnActorDead(ActorDeadEvent evt)
        {
            var scene = GetBattleScene();
            if (scene != null)
            {
                var unitViewComponent = scene.GetComponent<ETUnitViewComponent>();
                unitViewComponent?.DestroyUnitView(evt.ActorId);
            }
        }

        public void OnActorMove(ActorMoveEvent evt)
        {
            var scene = GetBattleScene();
            if (scene != null)
            {
                var unitViewComponent = scene.GetComponent<ETUnitViewComponent>();
                unitViewComponent?.UpdateUnitPosition(evt);
            }
        }

        public void OnActorDamage(ActorDamageEvent evt)
        {
            var scene = GetBattleScene();
            if (scene != null)
            {
                var unitViewComponent = scene.GetComponent<ETUnitViewComponent>();
                unitViewComponent?.UpdateUnitHp(evt);
            }
        }

        public void OnActorAttributeChange(ActorAttributeChangeEvent evt)
        {
        }

        public void OnSkillCast(SkillCastEvent evt)
        {
        }

        public void OnSkillHit(SkillHitEvent evt)
        {
        }

        public void OnVfxSpawn(VfxSpawnEvent evt)
        {
        }

        public void OnFloatingText(FloatingTextEvent evt)
        {
        }

        public void OnBattleStart(BattleStartEvent evt)
        {
            var scene = GetBattleScene();
            if (scene != null)
            {
                var battleViewComponent = scene.GetComponent<ETBattleViewComponent>();
                battleViewComponent?.OnBattleStart(evt);
            }
        }

        public void OnBattleEnd(BattleEndEvent evt)
        {
            var scene = GetBattleScene();
            if (scene != null)
            {
                var battleViewComponent = scene.GetComponent<ETBattleViewComponent>();
                battleViewComponent?.OnBattleEnd(evt);
            }
        }

        public void OnFrameTick(FrameTickEvent evt)
        {
        }

        public void OnFrameSyncComplete(int frame)
        {
        }
    }
}
