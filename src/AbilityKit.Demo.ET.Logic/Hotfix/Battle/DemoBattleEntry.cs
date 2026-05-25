using System;
using System.Threading.Tasks;
using ET.AbilityKit.Demo.ET.Share;
using AbilityKit.Ability.Config;
using AbilityKit.Demo.Moba.Share;

namespace ET.Logic
{
    /// <summary>
    /// DemoBattleEntry - 战斗入口
    /// 对应 Moba.Console 的 ConsoleBattleBootstrapper
    /// </summary>
    public static class DemoBattleEntry
    {
        private static bool _isRunning;
        private static long _localActorId = 1001;

        /// <summary>
        /// 开始战斗
        /// </summary>
        public static async Task StartBattleAsync(Scene scene, long playerId, string playerName)
        {
            _isRunning = true;
            Log.Info($"[DemoBattleEntry] Starting battle for player: {playerName}");

            // 创建战斗组件
            var battleComponent = scene.AddComponent<ETBattleComponent>();

            // 创建启动计划
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

            // 初始化战斗组件
            var textAssetLoader = new ETTextAssetLoader();
            battleComponent.InitializeBattle(plan, textAssetLoader);

            // 创建视图事件桥接（发布到 ET 事件系统）
            var viewSink = new ETViewEventSink(scene);
            battleComponent.ViewSink = viewSink;

            // 运行输入循环
            await RunInputLoopAsync(scene);

            Log.Info("[DemoBattleEntry] Battle entry finished");
        }

        /// <summary>
        /// 输入循环
        /// </summary>
        private static async Task RunInputLoopAsync(Scene scene)
        {
            var battleComponent = scene.GetComponent<ETBattleComponent>();
            var unitComponent = scene.GetComponent<ETUnitComponent>();
            var inputComponent = scene.GetComponent<ETInputComponent>();

            while (_isRunning && battleComponent?.State != BattleState.Ended)
            {
                // 处理输入
                if (Console.KeyAvailable)
                {
                    var key = Console.ReadKey(true);
                    ProcessInput(scene, key, battleComponent, unitComponent, inputComponent);
                }

                await Task.Delay(16); // ~60 FPS
            }
        }

        /// <summary>
        /// 处理输入
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
                    // 停止移动由 moba.core 处理（通过快照更新）
                    break;

                case ConsoleKey.Q:
                    _isRunning = false;
                    Log.Info("[DemoBattleEntry] Quit requested");
                    break;
            }
        }

        /// <summary>
        /// 停止战斗
        /// </summary>
        public static void StopBattle()
        {
            _isRunning = false;
            Log.Info("[DemoBattleEntry] Battle stopped");
        }
    }

    /// <summary>
    /// 视图事件 Sink 实现
    /// 将 AbilityKit 事件桥接到 ET 事件系统，由 ET.View 处理渲染
    /// </summary>
    public class ETViewEventSink: IETViewEventSink
    {
        private readonly Scene _scene;

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
            // 发布到 ET 事件系统
            EventSystem.Instance.Publish(_scene, evt);
        }

        public void OnActorDead(ActorDeadEvent evt)
        {
            // 发布到 ET 事件系统
            EventSystem.Instance.Publish(_scene, evt);
        }

        public void OnActorMove(ActorMoveEvent evt)
        {
            // 发布到 ET 事件系统
            EventSystem.Instance.Publish(_scene, evt);
        }

        public void OnActorDamage(ActorDamageEvent evt)
        {
            // 发布到 ET 事件系统
            EventSystem.Instance.Publish(_scene, evt);
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
            // 发布到 ET 事件系统
            EventSystem.Instance.Publish(_scene, evt);
        }

        public void OnBattleEnd(BattleEndEvent evt)
        {
            // 发布到 ET 事件系统
            EventSystem.Instance.Publish(_scene, evt);
        }

        public void OnFrameTick(FrameTickEvent evt)
        {
        }

        public void OnFrameSyncComplete(int frame)
        {
        }
    }
}
