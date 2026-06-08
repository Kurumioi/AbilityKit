using AbilityKit.Ability.Host.Extensions.FrameSync;

namespace AbilityKit.Game.Flow
{
    public sealed partial class BattleContext
    {
        public IClientPredictionDriverStats PredictionStats;
        public IClientPredictionReconcileTarget PredictionReconcileTarget;
        public IClientPredictionReconcileControl PredictionReconcileControl;
        public IClientPredictionTuningControl PredictionTuningControl;
    }
}
