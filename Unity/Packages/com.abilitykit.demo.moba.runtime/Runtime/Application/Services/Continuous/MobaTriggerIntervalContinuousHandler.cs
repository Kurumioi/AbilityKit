using AbilityKit.Core.Continuous;
using AbilityKit.Demo.Moba.Runtime.Application.Services.Triggering;
using AbilityKit.Demo.Moba.Services.Buffs.Runtime;

namespace AbilityKit.Demo.Moba.Services
{
    public sealed class MobaTriggerIntervalContinuousHandler : IMobaContinuousIntervalHandler
    {
        private readonly MobaTriggerExecutionGateway _triggers;

        public MobaTriggerIntervalContinuousHandler(MobaTriggerExecutionGateway triggers)
        {
            _triggers = triggers;
        }

        public bool CanHandle(IContinuous continuous)
        {
            return continuous != null
                && continuous is IMobaContinuousExecutionContextProvider
                && !(continuous is BuffContinuousRuntime);
        }

        public void OnInterval(IContinuous continuous, IMobaContinuousPeriodicConfig periodicConfig, in MobaCombatExecutionContext executionContext)
        {
            var triggerIds = periodicConfig?.IntervalEffectIds;
            if (continuous == null || triggerIds == null || triggerIds.Count == 0) return;

            var source = continuous is MobaTriggerIntervalContinuousRuntime
                ? "continuous.trigger_interval.interval"
                : "continuous.interval";
            for (int i = 0; i < triggerIds.Count; i++)
            {
                var triggerId = triggerIds[i];
                if (triggerId <= 0) continue;

                var request = MobaTriggerExecutionRequest<IContinuous>.Create(triggerId, continuous, source);
                _triggers?.ExecuteDirectTrigger(in request);
            }
        }
    }
}

