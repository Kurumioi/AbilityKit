using AbilityKit.Core.Continuous;
using AbilityKit.Demo.Moba.Runtime.Application.Services.Triggering;

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
            return continuous is MobaTriggerIntervalContinuousRuntime;
        }

        public void OnInterval(IContinuous continuous, IMobaContinuousPeriodicConfig periodicConfig, in MobaCombatExecutionContext executionContext)
        {
            var runtime = continuous as MobaTriggerIntervalContinuousRuntime;
            var triggerIds = periodicConfig?.IntervalEffectIds;
            if (runtime == null || triggerIds == null || triggerIds.Count == 0) return;

            for (int i = 0; i < triggerIds.Count; i++)
            {
                var triggerId = triggerIds[i];
                if (triggerId <= 0) continue;

                var request = MobaTriggerExecutionRequest<MobaTriggerIntervalContinuousRuntime>.Create(triggerId, runtime, "continuous.trigger_interval.interval");
                _triggers?.ExecuteDirectTrigger(in request);
            }
        }
    }
}

