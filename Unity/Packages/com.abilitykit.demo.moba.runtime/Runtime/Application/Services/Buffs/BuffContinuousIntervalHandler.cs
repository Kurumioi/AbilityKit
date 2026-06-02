using AbilityKit.Core.Continuous;
using AbilityKit.Demo.Moba.Config.Core;
using AbilityKit.Demo.Moba.Components;

namespace AbilityKit.Demo.Moba.Services
{
    internal sealed class BuffContinuousIntervalHandler : IMobaContinuousIntervalHandler
    {
        private readonly MobaConfigDatabase _configs;
        private readonly BuffEventPublisher _events;
        private readonly BuffStageEffectExecutor _stageEffects;

        public BuffContinuousIntervalHandler(MobaConfigDatabase configs, BuffEventPublisher events, BuffStageEffectExecutor stageEffects)
        {
            _configs = configs;
            _events = events;
            _stageEffects = stageEffects;
        }

        public bool CanHandle(IContinuous continuous)
        {
            return continuous is BuffContinuousRuntime;
        }

        public void OnInterval(IContinuous continuous, IMobaContinuousPeriodicConfig periodicConfig)
        {
            var buffContinuous = continuous as BuffContinuousRuntime;
            if (buffContinuous == null || periodicConfig == null) return;
            if (_configs == null || !_configs.TryGetBuff(buffContinuous.BuffId, out var buff) || buff == null) return;

            var runtime = buffContinuous.Runtime;
            if (runtime == null) return;

            _events?.PublishInterval(buff, runtime.SourceId, buffContinuous.TargetActorId, runtime);
            _stageEffects?.Execute(periodicConfig.IntervalEffectIds, buff.Id, runtime.SourceId, buffContinuous.TargetActorId, runtime.SourceContextId, MobaBuffTriggering.Stages.Interval, runtime);
        }
    }
}
