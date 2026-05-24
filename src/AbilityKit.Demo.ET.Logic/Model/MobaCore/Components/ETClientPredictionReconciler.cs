using System;
using System.Collections.Generic;

namespace ET.Logic
{
    /// <summary>
    /// ET Client Prediction Reconciler
    ///
    /// Design:
    /// - Reconciliates predicted states with server states
    /// - Handles rollback and resimulation
    /// </summary>
    [ComponentOf(typeof(Scene))]
    public class ETClientPredictionReconciler : Entity, IAwake, IDestroy
    {
        /// <summary>
        /// Is reconciliation enabled
        /// </summary>
        public bool IsEnabled { get; set; }

        /// <summary>
        /// Is initialized
        /// </summary>
        public bool IsInitialized { get; set; }

        /// <summary>
        /// Owner battle component
        /// </summary>
        public ETBattleComponent Owner { get; set; }

        /// <summary>
        /// Number of mispredictions detected
        /// </summary>
        public int MispredictionCount { get; set; }

        /// <summary>
        /// Rollback history frames
        /// </summary>
        public int MaxHistoryFrames { get; set; }

        /// <summary>
        /// Confirmed frame
        /// </summary>
        public int ConfirmedFrame { get; set; }

        /// <summary>
        /// Last reconcile frame
        /// </summary>
        public int LastReconcileFrame { get; set; }

        /// <summary>
        /// Reconcile success count
        /// </summary>
        public int ReconcileSuccessCount { get; set; }

        /// <summary>
        /// Reconcile mismatch count
        /// </summary>
        public int ReconcileMismatchCount { get; set; }

        /// <summary>
        /// Predicted hashes
        /// </summary>
        public Dictionary<int, int> PredictedHashes { get; set; } = new Dictionary<int, int>();

        /// <summary>
        /// Reconciliation threshold
        /// </summary>
        public float ReconcileThreshold { get; set; }

        /// <summary>
        /// On rollback requested callback
        /// </summary>
        public Action<int> OnRollbackRequested { get; set; }

        /// <summary>
        /// On reconcile completed callback
        /// </summary>
        public Action<int, bool> OnReconcileCompleted { get; set; }

        public void Awake()
        {
            IsEnabled = false;
            IsInitialized = false;
            MispredictionCount = 0;
            MaxHistoryFrames = 300;
            ReconcileThreshold = 0.1f;
        }

        public void Destroy()
        {
            OnRollbackRequested = null;
            OnReconcileCompleted = null;
            PredictedHashes?.Clear();
            PredictedHashes = null;
        }
    }
}
