using System;
using ET.AbilityKit.Demo.ET.Share;

namespace ET.Logic
{
    /// <summary>
    /// ETBattleViewFeature System
    ///
    /// Design:
    /// - Handles view feature lifecycle and SubFeature management
    /// - Coordinates with BattleViewEventSink for event routing
    /// - Manages entity binding lifecycle
    /// </summary>
    [EntitySystemOf(typeof(ETBattleViewFeature))]
    [FriendOf(typeof(ETBattleViewFeature))]
    [FriendOf(typeof(ETBattleComponent))]
    [FriendOf(typeof(ETUnitViewComponent))]
    [FriendOf(typeof(ETUnitComponent))]
    public static partial class ETBattleViewFeatureSystem
    {
        [EntitySystem]
        private static void Awake(this ETBattleViewFeature self)
        {
            Log.Info("[ETBattleViewFeature] System awake");
        }

        [EntitySystem]
        private static void Destroy(this ETBattleViewFeature self)
        {
            Log.Info("[ETBattleViewFeature] System destroyed");
            Cleanup(self);
        }

        // ============== Lifecycle Methods ==============

        /// <summary>
        /// Attach to battle component
        /// </summary>
        public static void OnAttach(this ETBattleViewFeature self, ETBattleComponent battle)
        {
            self.Owner = battle;
        }

        /// <summary>
        /// Initialize view feature
        /// </summary>
        public static void Initialize(this ETBattleViewFeature self)
        {
            var scene = self.Scene();

            // Create UnitViewComponent if not exists
            var unitViewComponent = scene.GetComponent<ETUnitViewComponent>();
            if (unitViewComponent == null)
            {
                unitViewComponent = scene.AddComponent<ETUnitViewComponent>();
                Log.Info("[ETBattleViewFeature] UnitViewComponent created");
            }

            // Create ViewBinder ready event handler
            self.OnViewBinderReady = OnViewBinderReady;
            self.OnViewsRebound = OnViewsRebound;
            self.OnViewFrameAligned = OnViewFrameAligned;

            self.IsInitialized = true;
            Log.Info("[ETBattleViewFeature] View feature initialized");
        }

        /// <summary>
        /// Handle view binder ready event
        /// </summary>
        private static void OnViewBinderReady(ViewBinderReadyEvent evt)
        {
            Log.Debug($"[ETBattleViewFeature] View binder ready at frame {evt.Frame}");
        }

        /// <summary>
        /// Handle views rebound event
        /// </summary>
        private static void OnViewsRebound(ViewsReboundEvent evt)
        {
            Log.Debug($"[ETBattleViewFeature] Views rebound at frame {evt.Frame}");
        }

        /// <summary>
        /// Handle view frame aligned event
        /// </summary>
        private static void OnViewFrameAligned(ET.AbilityKit.Demo.ET.Share.ViewFrameAlignedEvent evt)
        {
            Log.Debug($"[ETBattleViewFeature] View frame aligned at frame {evt.Frame}");
        }

        /// <summary>
        /// Cleanup resources
        /// </summary>
        private static void Cleanup(ETBattleViewFeature self)
        {
            self.IsInitialized = false;
            self.OnViewBinderReady = null;
            self.OnViewsRebound = null;
            self.OnViewFrameAligned = null;
        }

        // ============== View Event Handlers ==============

        /// <summary>
        /// Handle actor spawn event from BattleViewEventSink
        /// </summary>
        public static void OnActorSpawn(this ETBattleViewFeature self, ActorSpawnEvent evt)
        {
            if (!self.IsInitialized)
            {
                Log.Warning("[ETBattleViewFeature] View feature not initialized, cannot handle actor spawn");
                return;
            }

            var scene = self.Scene();
            var unitViewComponent = scene?.GetComponent<ETUnitViewComponent>();
            unitViewComponent?.CreateUnitView(evt);

            Log.Debug($"[ETBattleViewFeature] Actor spawn handled: {evt.Name} ({evt.EntityCode})");
        }

        /// <summary>
        /// Handle actor move event from BattleViewEventSink
        /// </summary>
        public static void OnActorMove(this ETBattleViewFeature self, ActorMoveEvent evt)
        {
            if (!self.IsInitialized)
                return;

            var scene = self.Scene();
            var unitViewComponent = scene?.GetComponent<ETUnitViewComponent>();
            unitViewComponent?.UpdateUnitPosition(evt);
        }

        /// <summary>
        /// Handle actor damage event from BattleViewEventSink
        /// </summary>
        public static void OnActorDamage(this ETBattleViewFeature self, ActorDamageEvent evt)
        {
            if (!self.IsInitialized)
                return;

            var scene = self.Scene();
            var unitViewComponent = scene?.GetComponent<ETUnitViewComponent>();

            // Update HP
            unitViewComponent?.UpdateUnitHp(evt);

            Log.Debug($"[ETBattleViewFeature] Damage: {evt.ActorId}, dmg={evt.Damage}, hp={evt.CurrentHp}/{evt.MaxHp}");
        }

        /// <summary>
        /// Handle actor dead event from BattleViewEventSink
        /// </summary>
        public static void OnActorDead(this ETBattleViewFeature self, ActorDeadEvent evt)
        {
            if (!self.IsInitialized)
                return;

            var scene = self.Scene();
            var unitViewComponent = scene?.GetComponent<ETUnitViewComponent>();
            unitViewComponent?.DestroyUnitView(evt.ActorId);

            Log.Debug($"[ETBattleViewFeature] Actor dead: {evt.ActorId}, killer={evt.KillerId}");
        }

        /// <summary>
        /// Handle battle start event
        /// </summary>
        public static void OnBattleStart(this ETBattleViewFeature self, BattleStartEvent evt)
        {
            Log.Info($"[ETBattleViewFeature] Battle start: {evt.BattleId}");
        }

        /// <summary>
        /// Handle battle end event
        /// </summary>
        public static void OnBattleEnd(this ETBattleViewFeature self, BattleEndEvent evt)
        {
            Log.Info($"[ETBattleViewFeature] Battle end: {evt.BattleId}, Victory={evt.IsVictory}");
        }

        // ============== Frame Tick ==============

        /// <summary>
        /// Handle frame tick event
        /// </summary>
        public static void OnFrameTick(this ETBattleViewFeature self, FrameTickEvent evt)
        {
            if (!self.IsInitialized)
                return;

            // Notify frame aligned
            self.OnViewFrameAligned?.Invoke(new ET.AbilityKit.Demo.ET.Share.ViewFrameAlignedEvent { Frame = evt.Frame, BattleId = 0 });
        }
    }
}
