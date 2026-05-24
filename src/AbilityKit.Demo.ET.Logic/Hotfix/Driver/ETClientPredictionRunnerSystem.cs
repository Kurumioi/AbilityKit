using System;
using System.Collections.Generic;

namespace ET.Logic
{
    /// <summary>
    /// ETClientPredictionRunner System
    ///
    /// Design:
    /// - Handles client prediction execution
    /// - Stores prediction snapshots for reconciliation
    /// - Handles rollback requests
    /// - 对应 moba.view 的 ClientPredictionRunner
    /// </summary>
    [EntitySystemOf(typeof(ETClientPredictionRunner))]
    [FriendOf(typeof(ETClientPredictionRunner))]
    [FriendOf(typeof(ETBattleComponent))]
    [FriendOf(typeof(ETMobaBattleDriver))]
    public static partial class ETClientPredictionRunnerSystem
    {
        [EntitySystem]
        private static void Awake(this ETClientPredictionRunner self)
        {
            Log.Info("[ETClientPredictionRunner] System awake");
        }

        [EntitySystem]
        private static void Destroy(this ETClientPredictionRunner self)
        {
            Log.Info("[ETClientPredictionRunner] System destroyed");
            Cleanup(self);
        }

        // ============== Lifecycle Methods ==============

        /// <summary>
        /// Initialize the prediction runner
        /// </summary>
        public static void Initialize(this ETClientPredictionRunner self, ETBattleComponent owner, ETPredictionConfig config)
        {
            self.Owner = owner;
            self.Config = config ?? ETPredictionConfig.Default;
            self.LocalPlayerId = (int)owner.PlayerActorId;
            self.PredictedFrame = 0;
            self.ConfirmedFrame = 0;
            self.RollbackCount = 0;

            // Subscribe to rollback events
            self.OnRollbackRequested = OnRollbackRequested;
            self.OnSnapshotApplied = OnSnapshotApplied;

            self.IsInitialized = true;
            Log.Info($"[ETClientPredictionRunner] Initialized: Mode={self.Config.Mode}, MaxAhead={self.Config.MaxPredictionAheadFrames}, RollbackHistory={self.Config.RollbackHistoryFrames}");
        }

        /// <summary>
        /// Cleanup resources
        /// </summary>
        private static void Cleanup(ETClientPredictionRunner self)
        {
            self.IsInitialized = false;
            self.OnRollbackRequested = null;
            self.OnSnapshotApplied = null;
        }

        // ============== Prediction Methods ==============

        /// <summary>
        /// Execute prediction tick
        /// Called each frame to execute local player input prediction
        /// </summary>
        public static void TickPredicted(this ETClientPredictionRunner self, int nextFrame, float fixedDelta, List<ETPlayerInputCmd> inputs)
        {
            if (!self.IsInitialized || self.Owner == null)
                return;

            var driver = self.Owner.BattleDriver;
            if (driver?.World == null)
                return;

            // Check if prediction is enabled
            if (self.Config.Mode == PredictionMode.None)
            {
                // No prediction, just tick normally
                return;
            }

            // Submit inputs
            if (inputs != null && inputs.Count > 0)
            {
                foreach (var input in inputs)
                {
                    driver.SubmitMoveInput(input.ActorId, input.TargetX, input.TargetZ);
                }
            }

            // Store inputs for rollback
            StoreInputs(self, nextFrame, inputs);

            // Tick the world
            driver.World.Tick(fixedDelta);

            // Update predicted frame
            self.PredictedFrame = nextFrame;

            Log.Debug($"[ETClientPredictionRunner] Predicted frame {nextFrame}");
        }

        /// <summary>
        /// Store inputs for later rollback
        /// </summary>
        private static void StoreInputs(ETClientPredictionRunner self, int frame, List<ETPlayerInputCmd> inputs)
        {
            if (inputs == null || inputs.Count == 0)
                return;

            // Ensure list capacity
            while (self.InputHistory.Count <= frame)
            {
                self.InputHistory.Add(default);
            }

            // Combine inputs into one command for this frame
            var combinedInput = new ETPlayerInputCmd
            {
                ActorId = inputs[0].ActorId,
                TargetX = inputs[0].TargetX,
                TargetZ = inputs[0].TargetZ
            };

            self.InputHistory[frame] = combinedInput;
        }

        /// <summary>
        /// Handle authoritative state hash from server
        /// </summary>
        public static bool OnAuthoritativeStateHash(this ETClientPredictionRunner self, int frame, uint hashValue)
        {
            if (!self.IsInitialized)
                return false;

            // Update confirmed frame
            if (frame > self.ConfirmedFrame)
            {
                var oldConfirmed = self.ConfirmedFrame;
                self.ConfirmedFrame = frame;
                self.OnSnapshotApplied?.Invoke(oldConfirmed, frame);
            }

            return false;
        }

        /// <summary>
        /// Handle rollback request
        /// </summary>
        private static void OnRollbackRequested(int rollbackFrame)
        {
            Log.Info($"[ETClientPredictionRunner] Rollback requested to frame {rollbackFrame}");
        }

        /// <summary>
        /// Handle snapshot applied
        /// </summary>
        private static void OnSnapshotApplied(int oldFrame, int newFrame)
        {
            Log.Debug($"[ETClientPredictionRunner] Snapshot applied: {oldFrame} -> {newFrame}");
        }

        /// <summary>
        /// Execute rollback and replay
        /// Note: Rollback requires integration with moba.core RollbackCoordinator
        /// This is a simplified implementation for now
        /// </summary>
        public static void ExecuteRollback(this ETClientPredictionRunner self, int rollbackFrame, float fixedDelta)
        {
            if (!self.IsInitialized || !self.Config.EnableRollback)
                return;

            var driver = self.Owner?.BattleDriver;
            if (driver?.World == null)
                return;

            Log.Info($"[ETClientPredictionRunner] Executing rollback to frame {rollbackFrame}");

            // TODO: Integrate with moba.core RollbackCoordinator for actual rollback
            // For now, just update the confirmed frame
            self.ConfirmedFrame = rollbackFrame;

            // Replay frames after rollback
            for (int f = rollbackFrame + 1; f <= self.PredictedFrame; f++)
            {
                // Get stored inputs
                if (f < self.InputHistory.Count)
                {
                    var input = self.InputHistory[f];
                    driver.SubmitMoveInput(input.ActorId, input.TargetX, input.TargetZ);
                }

                // Tick world
                driver.World.Tick(fixedDelta);
            }

            // Update stats
            self.RollbackCount++;
            self.LastRollbackFrame = rollbackFrame;

            Log.Info($"[ETClientPredictionRunner] Rollback replay finished, total rollbacks: {self.RollbackCount}");
        }

        /// <summary>
        /// Reset prediction state
        /// </summary>
        public static void Reset(this ETClientPredictionRunner self)
        {
            self.PredictedFrame = 0;
            self.ConfirmedFrame = 0;
            self.RollbackCount = 0;
            self.LastRollbackFrame = 0;
            self.InputHistory?.Clear();
            self.PendingFrames?.Clear();

            // TODO: Integrate with moba.core RollbackCoordinator.ClearHistory()

            Log.Info("[ETClientPredictionRunner] Reset");
        }

        // ============== Query Methods ==============

        /// <summary>
        /// Get prediction ahead frames
        /// </summary>
        public static int GetPredictionAheadFrames(this ETClientPredictionRunner self)
        {
            return self.PredictedFrame - self.ConfirmedFrame;
        }

        /// <summary>
        /// Check if prediction is ahead of confirmed
        /// </summary>
        public static bool IsPredictionAhead(this ETClientPredictionRunner self)
        {
            return self.PredictedFrame > self.ConfirmedFrame;
        }

        /// <summary>
        /// Get rollback statistics
        /// </summary>
        public static (int TotalRollbacks, int LastRollbackFrame, int PredictionAhead) GetStats(this ETClientPredictionRunner self)
        {
            return (self.RollbackCount, self.LastRollbackFrame, GetPredictionAheadFrames(self));
        }
    }
}
