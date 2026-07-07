using System.Collections.Generic;
using AbilityKit.Ability.Config;
using AbilityKit.Demo.Moba.Share;
using ET.AbilityKit.Demo.ET.Share;
using BattleStartPlan = AbilityKit.Demo.Moba.Share.BattleStartPlan;

namespace ET.Logic
{
    /// <summary>
    /// ETBattleComponent 系统。
    /// 通过 ET 宿主门面和框架 MOBA 驱动宿主驱动由 ET 承载的 MOBA 战斗循环。
    /// </summary>
    [EntitySystemOf(typeof(ETBattleComponent))]
    [FriendOf(typeof(ETBattleComponent))]
    [FriendOf(typeof(ETUnitComponent))]
    [FriendOf(typeof(ETUnit))]
    [FriendOf(typeof(ETInputComponent))]
    [FriendOf(typeof(ETMobaBattleDriver))]
    public static partial class ETBattleComponentSystem
    {
        #region 生命周期

        [EntitySystem]
        private static void Awake(this ETBattleComponent self)
        {
            Log.Info("[ETBattle] ETBattleComponent awake");
        }

        [EntitySystem]
        private static void Update(this ETBattleComponent self)
        {
            // 仅在战斗进行中更新。
            if (self.State != BattleState.InProgress)
                return;

            // 通过框架 MOBA 驱动宿主推进战斗驱动器。
            if (self.BattleDriver != null)
            {
                float deltaTime = 1f / self.TickRate;
                self.BattleDriver.Tick(deltaTime);
            }

            // 推进帧（检查战斗结束并发送 Tick 事件）。
            self.AdvanceFrame();
        }

        [EntitySystem]
        private static void Destroy(this ETBattleComponent self)
        {
            if (self.BattleDriver != null)
            {
                if (self.BattleDriver.IsRunning)
                {
                    self.BattleDriver.Stop();
                }

                self.BattleDriver.Destroy();
                self.BattleDriver = null;
            }

            self.BattleHost = null;
            self.ViewSink = null;
            self.State = BattleState.Ended;
            Log.Info("[ETBattle] ETBattleComponent destroyed");
        }

        #endregion

        #region 初始化

        /// <summary>
        /// 初始化战斗。
        /// </summary>
        /// <param name="self">战斗组件。</param>
        /// <param name="plan">战斗启动计划。</param>
        /// <param name="textAssetLoader">View 层配置加载器。</param>
        /// <param name="playerSpawnData">正式 MOBA 世界启动所需的显式玩家配置。</param>
        public static void InitializeBattle(this ETBattleComponent self, BattleStartPlan plan, ITextAssetLoader textAssetLoader, IReadOnlyList<ETPlayerSpawnData> playerSpawnData = null)
        {
            self.BattleId = IdGenerater.Instance.GenerateId();
            self.PlayerId = plan.PlayerId;
            self.PlayerActorId = 0;
            self.State = BattleState.Loading;

            Log.Info($"[ETBattle] Initializing battle {self.BattleId} for player {self.PlayerId}...");

            var scene = self.Scene();

            // 创建活动 ET 战斗循环使用的运行时组件。
            scene.AddComponent<ETUnitComponent>();
            scene.AddComponent<ETInputComponent>();

            // 创建 ET 宿主组件和平台无关的战斗驱动适配器。
            var battleHost = scene.AddComponent<ETMobaBattleDriver>();
            self.BattleHost = battleHost;
            self.BattleDriver = new ETMobaBattleRuntimeDriver(battleHost);

            if (playerSpawnData != null && playerSpawnData.Count > 0)
            {
                battleHost.PlayerSpawnData.Clear();
                battleHost.PlayerSpawnData.AddRange(playerSpawnData);
                Log.Info($"[ETBattle] Preloaded player spawn data: Count={battleHost.PlayerSpawnData.Count}");
            }
            else
            {
                var defaultSpawnData = PlayerSpawnBuilder.BuildSpawnListFromConfig(textAssetLoader, plan.PlayerId);
                battleHost.PlayerSpawnData.Clear();
                battleHost.PlayerSpawnData.AddRange(defaultSpawnData);
                Log.Info($"[ETBattle] Built default player spawn data from config: Count={battleHost.PlayerSpawnData.Count}");
            }

            // 为 ET.View 创建实体缓存组件。
            var cacheComponent = scene.AddComponent<ETBattleEntityCacheComponent>();

            // 创建 ET 视图接收器和可选 Demo 自动化接收器，再将组合后的输出传给驱动器。
            var viewSink = new ETBattleViewEventSink(self, cacheComponent);
            IBattleViewEventSink runtimeSink = self.AutomationOptions?.HasAnyAutomationEnabled == true
                ? new ETCompositeBattleViewEventSink(viewSink, new ETBattleAutomationSnapshotSink(scene, self))
                : viewSink;
            self.BattleDriver.Initialize(plan, runtimeSink);

            self.State = BattleState.Ready;
            Log.Info($"[ETBattle] Battle {self.BattleId} ready!");

            // 发布战斗初始化事件。
            EventSystem.Instance.Publish<Scene, BattleSceneInitFinish>(
                scene,
                new BattleSceneInitFinish
                {
                    PlayerId = plan.PlayerId,
                    PlayerName = $"Player_{plan.PlayerId}"
                });
        }

        #endregion

        #region 战斗

        /// <summary>
        /// 启动战斗。
        /// </summary>
        public static void StartBattle(this ETBattleComponent self)
        {
            if (self.State != BattleState.Ready)
            {
                Log.Warning($"[ETBattle] Cannot start battle, current state: {self.State}");
                return;
            }

            if (self.BattleHost != null && !self.BattleHost.RuntimeGameStarted)
            {
                Log.Warning("[ETBattle] Cannot start battle before runtime game start succeeds");
                return;
            }

            self.State = BattleState.InProgress;

            // 启动战斗驱动器。运行时视图接收器负责战斗生命周期通知。
            self.BattleDriver?.Start();

            Log.Info($"[ETBattle] Battle {self.BattleId} started!");
            Log.Info("====================================");
        }

        /// <summary>
        /// 结束战斗。
        /// </summary>
        public static void EndBattle(this ETBattleComponent self, bool isVictory)
        {
            if (self.State != BattleState.InProgress)
                return;

            self.State = BattleState.Ended;

            // 停止战斗驱动器。运行时视图接收器负责战斗生命周期通知。
            self.BattleDriver?.Stop();

            Log.Info("====================================");
            Log.Info($"[ETBattle] Battle {self.BattleId} ended!");
            Log.Info($"[ETBattle] Result: {(isVictory ? "VICTORY" : "DEFEAT")}");
            Log.Info($"[ETBattle] Duration: {self.LogicTimeSeconds:F1}s");
            Log.Info("====================================");
        }

        #endregion

        #region 帧

        /// <summary>
        /// 推进帧。
        /// </summary>
        public static void AdvanceFrame(this ETBattleComponent self)
        {
            if (self.State != BattleState.InProgress)
                return;

            if (self.BattleDriver == null)
                return;

            // 发送帧 Tick 事件。
            self.ViewSink?.OnFrameTick(new FrameTickEvent()
            {
                Frame = self.BattleDriver.CurrentFrame,
                TimeSeconds = (float)self.BattleDriver.LogicTimeSeconds
            });

            // 检查战斗结束。
            self.CheckBattleEnd();
        }

        /// <summary>
        /// 检查战斗结束。
        /// </summary>
        public static void CheckBattleEnd(this ETBattleComponent self)
        {
        }

        #endregion
}
}
