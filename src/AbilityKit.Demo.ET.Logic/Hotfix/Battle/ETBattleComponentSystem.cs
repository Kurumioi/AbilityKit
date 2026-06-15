using System.Collections.Generic;
using AbilityKit.Ability.Config;
using ET.AbilityKit.Demo.ET.Share;
using BattleStartPlan = AbilityKit.Demo.Moba.Share.BattleStartPlan;

namespace ET.Logic
{
    /// <summary>
    /// ETBattleComponent system.
    /// Drives the ET-hosted MOBA battle loop through ETMobaBattleDriver, HostRuntime and WorldManager.
    /// </summary>
    [EntitySystemOf(typeof(ETBattleComponent))]
    [FriendOf(typeof(ETBattleComponent))]
    [FriendOf(typeof(ETUnitComponent))]
    [FriendOf(typeof(ETUnit))]
    [FriendOf(typeof(ETInputComponent))]
    [FriendOf(typeof(ETMobaBattleDriver))]
    public static partial class ETBattleComponentSystem
    {
        #region Lifecycle

        [EntitySystem]
        private static void Awake(this ETBattleComponent self)
        {
            Log.Info("[ETBattle] ETBattleComponent awake");
        }

        [EntitySystem]
        private static void Update(this ETBattleComponent self)
        {
            // Only update when battle is in progress
            if (self.State != BattleState.InProgress)
                return;

            var currentFrame = self.BattleDriver?.CurrentFrame ?? 0;
            Log.Debug($"[ETBattleComponentSystem.Update] Frame={currentFrame}, State={self.State}");

            // Tick the battle driver.
            //    - PreTickPhase: 递增帧号
            //    - ProcessETInputPhase: 从 ETInputComponent 读取命令并提交到 IWorldInputSink
            //    - DriveWorldPhase: 驱动 moba.core 执行命令
            //    - CollectSnapshotPhase: 收集实体状态快照
            //    - DispatchSnapshotPhase: 分发快照到视图层
            if (self.BattleDriver != null)
            {
                float deltaTime = 1f / self.TickRate;
                Log.Debug($"[ETBattleComponentSystem.Update] Calling driver.Tick, Frame={self.BattleDriver.CurrentFrame}, DeltaTime={deltaTime:F4}");
                self.BattleDriver.Tick(deltaTime);
            }

            // Advance frame (check battle end, send tick event)
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

        #region Init

        /// <summary>
        /// Initialize battle
        /// </summary>
        /// <param name="self">Battle component</param>
        /// <param name="plan">Battle start plan</param>
        /// <param name="textAssetLoader">Config loader for View layer</param>
        /// <param name="playerSpawnData">Explicit player loadouts required by formal MOBA world startup.</param>
        public static void InitializeBattle(this ETBattleComponent self, BattleStartPlan plan, ITextAssetLoader textAssetLoader, IReadOnlyList<ETPlayerSpawnData> playerSpawnData = null)
        {
            self.BattleId = IdGenerater.Instance.GenerateId();
            self.PlayerId = plan.PlayerId;
            self.PlayerActorId = 0;
            self.State = BattleState.Loading;

            Log.Info($"[ETBattle] Initializing battle {self.BattleId} for player {self.PlayerId}...");

            var scene = self.Scene();

            // Create runtime components used by the active ET battle loop.
            scene.AddComponent<ETUnitComponent>();
            scene.AddComponent<ETInputComponent>();

            // Create ET host component and platform-independent battle driver adapter.
            var battleHost = scene.AddComponent<ETMobaBattleDriver>();
            self.BattleHost = battleHost;
            self.BattleDriver = new ETMobaBattleRuntimeDriver(battleHost);

            battleHost.EnsureSnapshotHandlersRegistered();

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

            // Create Entity Cache Component for ET.View
            var cacheComponent = scene.AddComponent<ETBattleEntityCacheComponent>();

            // Create ET View Event Sink and pass to BattleDriver
            var viewSink = new ETBattleViewEventSink(self);
            viewSink.InitializeCache(cacheComponent);
            self.BattleDriver.Initialize(plan, viewSink);

            self.State = BattleState.Ready;
            Log.Info($"[ETBattle] Battle {self.BattleId} ready!");

            // Publish battle init event
            EventSystem.Instance.Publish<Scene, BattleSceneInitFinish>(
                scene,
                new BattleSceneInitFinish
                {
                    PlayerId = plan.PlayerId,
                    PlayerName = $"Player_{plan.PlayerId}"
                });
        }

        #endregion

        #region Battle

        /// <summary>
        /// Start battle
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

            // Start battle driver. Runtime view sink owns battle lifecycle notifications.
            ETBattleDriverBridge.Start(self);

            Log.Info($"[ETBattle] Battle {self.BattleId} started!");
            Log.Info("====================================");
        }

        /// <summary>
        /// End battle
        /// </summary>
        public static void EndBattle(this ETBattleComponent self, bool isVictory)
        {
            if (self.State != BattleState.InProgress)
                return;

            self.State = BattleState.Ended;

            // Stop battle driver. Runtime view sink owns battle lifecycle notifications.
            ETBattleDriverBridge.Stop(self);

            Log.Info("====================================");
            Log.Info($"[ETBattle] Battle {self.BattleId} ended!");
            Log.Info($"[ETBattle] Result: {(isVictory ? "VICTORY" : "DEFEAT")}");
            Log.Info($"[ETBattle] Duration: {self.LogicTimeSeconds:F1}s");
            Log.Info("====================================");
        }

        #endregion

        #region Frame

        /// <summary>
        /// Advance frame
        /// </summary>
        public static void AdvanceFrame(this ETBattleComponent self)
        {
            if (self.State != BattleState.InProgress)
                return;

            if (self.BattleDriver == null)
                return;

            // Send frame tick event
            self.ViewSink?.OnFrameTick(new FrameTickEvent()
            {
                Frame = self.BattleDriver.CurrentFrame,
                TimeSeconds = (float)self.BattleDriver.LogicTimeSeconds
            });

            // Check battle end
            self.CheckBattleEnd();
        }

        /// <summary>
        /// Check battle end
        /// </summary>
        public static void CheckBattleEnd(this ETBattleComponent self)
        {
        }

        #endregion
}
}
