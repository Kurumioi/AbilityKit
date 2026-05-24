using System;
using ET.AbilityKit.Demo.ET.Share;

namespace ET.Logic
{
    /// <summary>
    /// ETBattleAutoTestComponent System
    /// Handles auto test logic
    /// </summary>
    [EntitySystemOf(typeof(ETBattleAutoTestComponent))]
    [FriendOf(typeof(ETBattleAutoTestComponent))]
    [FriendOf(typeof(ETInputComponent))]
    public static partial class ETBattleAutoTestComponentSystem
    {
        [EntitySystem]
        private static void Awake(this ETBattleAutoTestComponent self)
        {
            Log.Info("[ETBattleAutoTest] System awake");
        }

        [EntitySystem]
        private static void Update(this ETBattleAutoTestComponent self)
        {
        }

        [EntitySystem]
        private static void Destroy(this ETBattleAutoTestComponent self)
        {
            Log.Info($"[ETBattleAutoTest] System destroyed. Total move commands: {self.MoveCommandCount}, Distance: {self.MoveDistance:F2}");
        }

        /// <summary>
        /// Every frame update - send auto move command
        /// </summary>
        public static void OnUpdate(this ETBattleAutoTestComponent self, int frame)
        {
            if (!self.IsEnabled)
                return;

            // Send move command every N frames
            if (frame % self.MoveIntervalFrames == 0)
            {
                self.SendAutoMoveCommand(frame);
            }
        }

        /// <summary>
        /// Send auto move command
        /// </summary>
        private static void SendAutoMoveCommand(this ETBattleAutoTestComponent self, int frame)
        {
            // Calculate new target (time-based dynamic target)
            float time = frame * 0.1f;
            float newTargetX = 25f + (float)Math.Sin(time) * 20f; // X range 5-45
            float newTargetY = (float)Math.Cos(time) * 15f; // Y range -15 to 15

            // Get input component and send command
            var inputComponent = self.Scene().GetComponent<ETInputComponent>();
            if (inputComponent != null)
            {
                inputComponent.AddMoveCommand(frame, (int)self.TestActorId, newTargetX, newTargetY);

                Log.Debug($"[ETBattleAutoTest] Frame={frame}: Move command sent, ActorId={self.TestActorId}, Target=({newTargetX:F2}, {newTargetY:F2})");
            }
            else
            {
                Log.Warning("[ETBattleAutoTest] ETInputComponent not found!");
            }
        }

        /// <summary>
        /// Enable auto test
        /// </summary>
        public static void Enable(this ETBattleAutoTestComponent self)
        {
            self.IsEnabled = true;
            Log.Info("[ETBattleAutoTest] Auto test enabled");
        }

        /// <summary>
        /// Disable auto test
        /// </summary>
        public static void Disable(this ETBattleAutoTestComponent self)
        {
            self.IsEnabled = false;
            Log.Info("[ETBattleAutoTest] Auto test disabled");
        }

        /// <summary>
        /// Reset stats
        /// </summary>
        public static void ResetStats(this ETBattleAutoTestComponent self)
        {
            Log.Info($"[ETBattleAutoTest] Stats reset. Previous: Commands={self.MoveCommandCount}, Distance={self.MoveDistance:F2}");
        }
    }
}
