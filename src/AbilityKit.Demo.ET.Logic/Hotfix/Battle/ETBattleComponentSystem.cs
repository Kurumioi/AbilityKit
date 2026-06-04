using System;
using AbilityKit.Ability.Config;
using ET.AbilityKit.Demo.ET.Share;
using BattleStartPlan = AbilityKit.Demo.Moba.Share.BattleStartPlan;

namespace ET.Logic
{
    /// <summary>
    /// ETBattleComponent System
    /// ??????
    ///
    /// ?? ETMobaBattleDriver + HostRuntime + WorldManager ??? moba.core
    /// </summary>
    [EntitySystemOf(typeof(ETBattleComponent))]
    [FriendOf(typeof(ETBattleComponent))]
    [FriendOf(typeof(ETUnitComponent))]
    [FriendOf(typeof(ETUnit))]
    [FriendOf(typeof(ETInputComponent))]
    [FriendOf(typeof(ETFlowComponent))]
    [FriendOf(typeof(ETSessionComponent))]
    [FriendOf(typeof(ETMobaBattleDriver))]
    [FriendOf(typeof(ETBattleAutoTestComponent))]
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

            // 1. Process auto test (generates move commands to ETInputComponent)
            var currentFrame = self.BattleDriver?.CurrentFrame ?? 0;
            Log.Debug($"[ETBattleComponentSystem.Update] Frame={currentFrame}, State={self.State}");
            ETBattleDriverBridge.ProcessAutoTest(self, currentFrame);

            // 2. Process skill test (generates skill commands to ETInputComponent)
            ETBattleDriverBridge.ProcessSkillTest(self, currentFrame);

            // 3. Tick the battle driver
            //    - PreTickPhase: 递增帧号
            //    - ProcessETInputPhase: 从 ETInputComponent 读取命令并提交到 IWorldInputSink
            //    - DriveWorldPhase: 驱动 moba.core 执行命令
            //    - CollectSnapshotPhase: 收集实体状态快照
            //    - DispatchSnapshotPhase: 分发快照到视图层
            if (self.BattleDriver is ETMobaBattleDriver driver)
            {
                float deltaTime = 1f / self.TickRate;
                Log.Debug($"[ETBattleComponentSystem.Update] Calling driver.Tick, Frame={driver.CurrentFrame}, DeltaTime={deltaTime:F4}");
                driver.Tick(deltaTime);
            }

            // 4. Advance frame (check battle end, send tick event)
            self.AdvanceFrame();
        }

        [EntitySystem]
        private static void Destroy(this ETBattleComponent self)
        {
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
        public static void InitializeBattle(this ETBattleComponent self, BattleStartPlan plan, ITextAssetLoader textAssetLoader)
        {
            self.BattleId = IdGenerater.Instance.GenerateId();
            self.PlayerId = plan.PlayerId;
            self.PlayerActorId = 0;
            self.State = BattleState.Loading;

            Log.Info($"[ETBattle] Initializing battle {self.BattleId} for player {self.PlayerId}...");

            var scene = self.Scene();

            // Create components
            var unitComponent = scene.AddComponent<ETUnitComponent>();
            var inputComponent = scene.AddComponent<ETInputComponent>();
            var flowComponent = scene.AddComponent<ETFlowComponent>();
            var sessionComponent = scene.AddComponent<ETSessionComponent>();

            // Create BattleDriver with HostRuntime + WorldManager
            var battleDriver = scene.AddComponent<ETMobaBattleDriver>();
            self.BattleDriver = battleDriver;

            // 必须手动调用 Awake 来注册 Handlers
            battleDriver.Awake();

            // Create Entity Cache Component for ET.View
            var cacheComponent = scene.AddComponent<ETBattleEntityCacheComponent>();

            // Create ET View Event Sink and pass to BattleDriver
            var viewSink = new ETBattleViewEventSink(self);
            viewSink.InitializeCache(cacheComponent);
            battleDriver.Initialize(plan, viewSink);

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

            if (self.BattleDriver is ETMobaBattleDriver driver && !driver.RuntimeGameStarted)
            {
                Log.Warning("[ETBattle] Cannot start battle before runtime game start succeeds");
                return;
            }

            self.State = BattleState.InProgress;

            var session = self.Scene().GetComponent<ETSessionComponent>();
            if (session != null)
            {
                session.IsActive = true;
                session.StartTimeSeconds = Environment.TickCount64;
            }

            // Start battle driver
            ETBattleDriverBridge.Start(self);

            Log.Info($"[ETBattle] Battle {self.BattleId} started!");
            Log.Info("====================================");

            // Create AutoTest component (will be initialized in OnEnterGameSnapshot)
            var scene = self.Scene();
            var autoTest = scene.AddComponent<ETBattleAutoTestComponent>();
            autoTest.MoveIntervalFrames = BattleTestConfig.DefaultMoveIntervalFrames;
            autoTest.MoveSpeed = BattleTestConfig.DefaultMoveSpeed;
            autoTest.Enable();
            Log.Info($"[ETBattle] AutoTest component created");

            // Create SkillTest component
            var skillTest = scene.AddComponent<ETBattleSkillTestComponent>();
            skillTest.SkillIntervalFrames = BattleTestConfig.DefaultSkillIntervalFrames * 2; // Every 8 seconds at 30fps
            skillTest.SkillSlot = BattleTestConfig.DefaultSkillSlot;
            skillTest.Enable();
            Log.Info($"[ETBattle] SkillTest component created");

            // Notify battle start
            self.ViewSink?.OnBattleStart(new BattleStartEvent()
            {
                BattleId = self.BattleId,
                PlayerId = self.PlayerId
            });
        }

        /// <summary>
        /// End battle
        /// </summary>
        public static void EndBattle(this ETBattleComponent self, bool isVictory)
        {
            if (self.State != BattleState.InProgress)
                return;

            self.State = BattleState.Ended;

            var session = self.Scene().GetComponent<ETSessionComponent>();
            if (session != null)
            {
                session.IsActive = false;
            }

            // Stop battle driver
            ETBattleDriverBridge.Stop(self);

            Log.Info("====================================");
            Log.Info($"[ETBattle] Battle {self.BattleId} ended!");
            Log.Info($"[ETBattle] Result: {(isVictory ? "VICTORY" : "DEFEAT")}");
            Log.Info($"[ETBattle] Duration: {self.LogicTimeSeconds:F1}s");
            Log.Info("====================================");

            // Notify battle end
            self.ViewSink?.OnBattleEnd(new BattleEndEvent()
            {
                BattleId = self.BattleId,
                IsVictory = isVictory
            });
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
