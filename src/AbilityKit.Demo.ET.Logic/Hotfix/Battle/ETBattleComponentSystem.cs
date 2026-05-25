using System;
using AbilityKit.Ability.Config;
using AbilityKit.Ability.World.Abstractions;
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
            self.PlayerActorId = plan.PlayerId;
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
            autoTest.MoveIntervalFrames = 5;
            autoTest.MoveSpeed = 5f;
            autoTest.Enable();
            Log.Info($"[ETBattle] AutoTest component created");

            // Create SkillTest component
            var skillTest = scene.AddComponent<ETBattleSkillTestComponent>();
            skillTest.SkillIntervalFrames = 120; // Every 4 seconds at 30fps
            skillTest.SkillSlot = 0; // First skill
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

        #region Input

        /// <summary>
        /// Submit move input
        /// </summary>
        public static void SubmitMoveInput(this ETBattleComponent self, long actorId, float targetX, float targetZ)
        {
            ETBattleDriverBridge.SubmitMoveInput(self, actorId, targetX, targetZ);
        }

        /// <summary>
        /// Submit skill input
        /// </summary>
        public static void SubmitSkillInput(this ETBattleComponent self, long actorId, int slot, float targetX, float targetZ)
        {
            ETBattleDriverBridge.SubmitSkillInput(self, actorId, slot, targetX, targetZ);
        }

        #endregion

        #region Services

        /// <summary>
        /// Get World
        /// </summary>
        public static IWorld GetWorld(this ETBattleComponent self)
        {
            return ETBattleDriverBridge.GetWorld(self);
        }

        /// <summary>
        /// Try resolve service
        /// </summary>
        public static bool TryResolve<T>(this ETBattleComponent self, out T service) where T : class
        {
            return ETBattleDriverBridge.TryResolve(self, out service);
        }

        #endregion
    }
}
