using System;
using System.Collections.Generic;

namespace ET.Logic
{
    /// <summary>
    /// ET Client Prediction Runner
    ///
    /// Design:
    /// - Runs client-side prediction for hybrid sync mode
    /// - Stores predicted inputs and states
    /// </summary>
    [ComponentOf(typeof(Scene))]
    public class ETClientPredictionRunner : Entity, IAwake, IDestroy
    {
        /// <summary>
        /// Is prediction enabled
        /// </summary>
        public bool IsEnabled { get; set; }

        /// <summary>
        /// Is initialized
        /// </summary>
        public bool IsInitialized { get; set; }

        /// <summary>
        /// Local player ID for prediction
        /// </summary>
        public int LocalPlayerId { get; set; }

        /// <summary>
        /// Owner battle component
        /// </summary>
        public ETBattleComponent Owner { get; set; }

        /// <summary>
        /// Prediction config
        /// </summary>
        public ETPredictionConfig Config { get; set; }

        /// <summary>
        /// Predicted frame
        /// </summary>
        public int PredictedFrame { get; set; }

        /// <summary>
        /// Confirmed frame
        /// </summary>
        public int ConfirmedFrame { get; set; }

        /// <summary>
        /// Rollback count
        /// </summary>
        public int RollbackCount { get; set; }

        /// <summary>
        /// Input history
        /// </summary>
        public List<ETPlayerInputCmd> InputHistory { get; set; } = new List<ETPlayerInputCmd>();

        /// <summary>
        /// On rollback requested callback
        /// </summary>
        public Action<int> OnRollbackRequested { get; set; }

        /// <summary>
        /// On snapshot applied callback
        /// </summary>
        public Action<int> OnSnapshotApplied { get; set; }

        public void Awake()
        {
            IsEnabled = false;
            IsInitialized = false;
            LocalPlayerId = 0;
            PredictedFrame = 0;
            ConfirmedFrame = 0;
            RollbackCount = 0;
        }

        public void Destroy()
        {
            OnRollbackRequested = null;
            OnSnapshotApplied = null;
            InputHistory?.Clear();
            InputHistory = null;
        }
    }
}
