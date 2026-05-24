using System;
using AbilityKit.Core.Common.Log;
using AbilityKit.Demo.Moba.Share;

namespace ET.Logic
{
    /// <summary>
    /// ETBattleSessionHooks System
    ///
    /// Design:
    /// - Handles hook invocation for battle session lifecycle
    /// - All business logic is in this System (遵循 ET 规范)
    /// - Component only stores hook Action delegates
    /// </summary>
    [EntitySystemOf(typeof(ETBattleSessionHooks))]
    [FriendOf(typeof(ETBattleSessionHooks))]
    public static partial class ETBattleSessionHooksSystem
    {
        [EntitySystem]
        private static void Awake(this ETBattleSessionHooks self)
        {
            Log.Info("[ETBattleSessionHooks] System awake");
        }

        [EntitySystem]
        private static void Destroy(this ETBattleSessionHooks self)
        {
            Log.Info("[ETBattleSessionHooks] System destroyed");
        }

        // ============== Hook Invocation Methods ==============

        /// <summary>
        /// Invoke PreTick hook
        /// </summary>
        public static void InvokePreTick(this ETBattleSessionHooks self, float deltaTime)
        {
            try
            {
                self.OnPreTick?.Invoke(deltaTime);
            }
            catch (Exception ex)
            {
                Log.Error($"[ETBattleSessionHooks] OnPreTick exception: {ex.Message}");
            }
        }

        /// <summary>
        /// Invoke PostTick hook
        /// </summary>
        public static void InvokePostTick(this ETBattleSessionHooks self, float deltaTime)
        {
            try
            {
                self.OnPostTick?.Invoke(deltaTime);
            }
            catch (Exception ex)
            {
                Log.Error($"[ETBattleSessionHooks] OnPostTick exception: {ex.Message}");
            }
        }

        /// <summary>
        /// Invoke FirstFrameReceived hook
        /// </summary>
        public static void InvokeFirstFrameReceived(this ETBattleSessionHooks self)
        {
            try
            {
                self.OnFirstFrameReceived?.Invoke();
            }
            catch (Exception ex)
            {
                Log.Error($"[ETBattleSessionHooks] OnFirstFrameReceived exception: {ex.Message}");
            }
        }

        /// <summary>
        /// Invoke SessionStarted hook
        /// </summary>
        public static void InvokeSessionStarted(this ETBattleSessionHooks self, in BattleStartPlan plan)
        {
            try
            {
                self.OnSessionStarted?.Invoke(plan);
            }
            catch (Exception ex)
            {
                Log.Error($"[ETBattleSessionHooks] OnSessionStarted exception: {ex.Message}");
            }
        }

        /// <summary>
        /// Invoke SessionFailed hook
        /// </summary>
        public static void InvokeSessionFailed(this ETBattleSessionHooks self, Exception ex)
        {
            try
            {
                self.OnSessionFailed?.Invoke(ex);
            }
            catch (Exception innerEx)
            {
                Log.Error($"[ETBattleSessionHooks] OnSessionFailed exception: {innerEx.Message}");
            }
        }

        /// <summary>
        /// Invoke SessionStarting hook
        /// </summary>
        public static void InvokeSessionStarting(this ETBattleSessionHooks self)
        {
            try
            {
                self.OnSessionStarting?.Invoke();
            }
            catch (Exception ex)
            {
                Log.Error($"[ETBattleSessionHooks] OnSessionStarting exception: {ex.Message}");
            }
        }

        /// <summary>
        /// Invoke SessionStopping hook
        /// </summary>
        public static void InvokeSessionStopping(this ETBattleSessionHooks self)
        {
            try
            {
                self.OnSessionStopping?.Invoke();
            }
            catch (Exception ex)
            {
                Log.Error($"[ETBattleSessionHooks] OnSessionStopping exception: {ex.Message}");
            }
        }
    }
}
