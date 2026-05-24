namespace ET.Logic
{
    using PredictionConfig = ETPredictionConfig;

    /// <summary>
    /// ET Prediction Config
    ///
    /// Design:
    /// - Configuration for client prediction
    /// </summary>
    public sealed class ETPredictionConfig
    {
        /// <summary>
        /// Is prediction enabled
        /// </summary>
        public bool EnablePrediction { get; set; }

        /// <summary>
        /// Is client prediction enabled
        /// </summary>
        public bool EnableClientPrediction { get; set; }

        /// <summary>
        /// Prediction mode
        /// </summary>
        public PredictionMode Mode { get; set; }

        /// <summary>
        /// Max prediction frames
        /// </summary>
        public int MaxPredictionFrames { get; set; }

        /// <summary>
        /// Max prediction ahead frames
        /// </summary>
        public int MaxPredictionAheadFrames { get; set; }

        /// <summary>
        /// Rollback history frames
        /// </summary>
        public int RollbackHistoryFrames { get; set; }

        /// <summary>
        /// Reconcile threshold
        /// </summary>
        public float ReconcileThreshold { get; set; }

        /// <summary>
        /// Default configuration
        /// </summary>
        public static ETPredictionConfig Default { get; } = new ETPredictionConfig
        {
            EnablePrediction = true,
            EnableClientPrediction = true,
            Mode = PredictionMode.LocalPrediction,
            MaxPredictionFrames = 5,
            MaxPredictionAheadFrames = 3,
            RollbackHistoryFrames = 30,
            ReconcileThreshold = 0.1f
        };
    }

    /// <summary>
    /// Prediction mode enum
    /// </summary>
    public enum PredictionMode
    {
        None = 0,
        LocalPrediction = 1,
        ServerReconciliation = 2,
        Hybrid = 3
    }
}
