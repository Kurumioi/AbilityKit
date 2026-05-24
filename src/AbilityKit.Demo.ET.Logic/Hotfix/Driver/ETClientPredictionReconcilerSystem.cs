using System;
using System.Collections.Generic;

namespace ET.Logic
{
    /// <summary>
    /// ETClientPredictionReconciler System
    ///
    /// Design:
    /// - Handles reconciliation between predicted and authoritative state
    /// - Triggers rollback when hash mismatch detected
    /// - 对应 moba.view 的 ClientPredictionReconciler
    /// </summary>
    [EntitySystemOf(typeof(ETClientPredictionReconciler))]
    [FriendOf(typeof(ETClientPredictionReconciler))]
    [FriendOf(typeof(ETBattleComponent))]
    public static partial class ETClientPredictionReconcilerSystem
    {
        [EntitySystem]
        private static void Awake(this ETClientPredictionReconciler self)
        {
            Log.Info("[ETClientPredictionReconciler] System awake");
        }

        [EntitySystem]
        private static void Destroy(this ETClientPredictionReconciler self)
        {
            Log.Info("[ETClientPredictionReconciler] System destroyed");
            Cleanup(self);
        }

        // ============== Lifecycle Methods ==============

        /// <summary>
        /// Initialize the reconciler
        /// </summary>
        public static void Initialize(this ETClientPredictionReconciler self, ETBattleComponent owner, int maxHistoryFrames = 300)
        {
            self.Owner = owner;
            self.MaxHistoryFrames = maxHistoryFrames;
            self.ConfirmedFrame = 0;
            self.LastReconcileFrame = 0;
            self.ReconcileSuccessCount = 0;
            self.ReconcileMismatchCount = 0;

            // Subscribe to rollback events
            self.OnRollbackRequested = OnRollbackRequested;
            self.OnReconcileCompleted = OnReconcileCompleted;

            self.IsInitialized = true;
            Log.Info($"[ETClientPredictionReconciler] Initialized: MaxHistoryFrames={maxHistoryFrames}");
        }

        /// <summary>
        /// Cleanup resources
        /// </summary>
        private static void Cleanup(ETClientPredictionReconciler self)
        {
            self.IsInitialized = false;
            self.PredictedHashes?.Clear();
            self.OnRollbackRequested = null;
            self.OnReconcileCompleted = null;
        }

        // ============== Reconciliation Methods ==============

        /// <summary>
        /// Record a predicted state hash
        /// </summary>
        public static void RecordPredictedHash(this ETClientPredictionReconciler self, int frame, uint hash)
        {
            if (!self.IsInitialized)
                return;

            // Store hash
            self.PredictedHashes[frame] = hash;

            // Cleanup old entries
            CleanupOldEntries(self, frame);

            Log.Debug($"[ETClientPredictionReconciler] Recorded predicted hash: frame={frame}, hash={hash}");
        }

        /// <summary>
        /// Handle authoritative state hash from server
        /// Returns true if rollback is needed
        /// </summary>
        public static bool OnAuthoritativeHash(this ETClientPredictionReconciler self, int frame, uint authoritativeHash)
        {
            if (!self.IsInitialized)
                return false;

            // Update confirmed frame
            if (frame > self.ConfirmedFrame)
            {
                self.ConfirmedFrame = frame;
            }

            // Get predicted hash
            if (!self.PredictedHashes.TryGetValue(frame, out var predictedHash))
            {
                // No predicted hash for this frame, just confirm
                self.OnReconcileCompleted?.Invoke(frame, true);
                return false;
            }

            // Compare hashes
            bool matches = predictedHash == authoritativeHash;
            self.LastReconcileFrame = frame;

            if (matches)
            {
                self.ReconcileSuccessCount++;
                self.OnReconcileCompleted?.Invoke(frame, true);
                Log.Debug($"[ETClientPredictionReconciler] Hash matches: frame={frame}, hash={predictedHash}");
                return false;
            }
            else
            {
                self.ReconcileMismatchCount++;
                Log.Warning($"[ETClientPredictionReconciler] Hash mismatch: frame={frame}, predicted={predictedHash}, authoritative={authoritativeHash}");

                // Trigger rollback
                self.OnRollbackRequested?.Invoke(frame);
                self.OnReconcileCompleted?.Invoke(frame, false);
                return true;
            }
        }

        /// <summary>
        /// Cleanup old hash entries
        /// </summary>
        private static void CleanupOldEntries(ETClientPredictionReconciler self, int currentFrame)
        {
            // Remove entries older than confirmed frame
            var toRemove = new List<int>();
            foreach (var kvp in self.PredictedHashes)
            {
                if (kvp.Key < self.ConfirmedFrame)
                {
                    toRemove.Add(kvp.Key);
                }
            }

            foreach (var frame in toRemove)
            {
                self.PredictedHashes.Remove(frame);
            }

            // Also limit total entries
            if (self.PredictedHashes.Count > self.MaxHistoryFrames)
            {
                // Remove oldest entries
                var frames = new List<int>(self.PredictedHashes.Keys);
                frames.Sort();
                int removeCount = self.PredictedHashes.Count - self.MaxHistoryFrames;
                for (int i = 0; i < removeCount && i < frames.Count; i++)
                {
                    self.PredictedHashes.Remove(frames[i]);
                }
            }
        }

        /// <summary>
        /// Handle rollback request
        /// </summary>
        private static void OnRollbackRequested(int rollbackFrame)
        {
            Log.Info($"[ETClientPredictionReconciler] Rollback requested to frame {rollbackFrame}");
        }

        /// <summary>
        /// Handle reconcile completed
        /// </summary>
        private static void OnReconcileCompleted(int frame, bool success)
        {
            if (success)
            {
                Log.Debug($"[ETClientPredictionReconciler] Reconcile completed: frame={frame}, success=true");
            }
            else
            {
                Log.Warning($"[ETClientPredictionReconciler] Reconcile completed: frame={frame}, success=false");
            }
        }

        // ============== Query Methods ==============

        /// <summary>
        /// Get predicted hash for a frame
        /// </summary>
        public static bool TryGetPredictedHash(this ETClientPredictionReconciler self, int frame, out uint hash)
        {
            return self.PredictedHashes.TryGetValue(frame, out hash);
        }

        /// <summary>
        /// Get reconciliation statistics
        /// </summary>
        public static (int Success, int Mismatch, int Confirmed) GetStats(this ETClientPredictionReconciler self)
        {
            return (self.ReconcileSuccessCount, self.ReconcileMismatchCount, self.ConfirmedFrame);
        }

        /// <summary>
        /// Get match rate
        /// </summary>
        public static float GetMatchRate(this ETClientPredictionReconciler self)
        {
            int total = self.ReconcileSuccessCount + self.ReconcileMismatchCount;
            if (total == 0)
                return 1f;
            return (float)self.ReconcileSuccessCount / total;
        }

        /// <summary>
        /// Clear all predicted hashes
        /// </summary>
        public static void Clear(this ETClientPredictionReconciler self)
        {
            self.PredictedHashes?.Clear();
            self.ConfirmedFrame = 0;
            self.LastReconcileFrame = 0;
            Log.Info("[ETClientPredictionReconciler] Cleared");
        }
    }
}
