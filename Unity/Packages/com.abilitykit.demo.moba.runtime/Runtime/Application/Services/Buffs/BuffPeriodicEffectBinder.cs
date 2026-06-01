using System;
using AbilityKit.Demo.Moba.Config.BattleDemo.MO;
using AbilityKit.Core.Common.Log;
using AbilityKit.Demo.Moba.Components;

namespace AbilityKit.Demo.Moba.Services
{
    internal sealed class BuffPeriodicEffectBinder
    {
        private readonly MobaPeriodicEffectService _periodic;

        public BuffPeriodicEffectBinder(MobaPeriodicEffectService periodic, AbilityKit.Ability.Triggering.Runtime.ITriggerActionRunner actionRunner)
        {
            _periodic = periodic;
        }

        public void TryStartPeriodicEffectByBuff(BuffMO buff, BuffRuntime runtime, int sourceActorId, int targetActorId)
        {
            if (_periodic == null) return;
            if (buff == null || runtime == null) return;
            if (buff.IntervalMs <= 0) return;
            if (buff.OnIntervalEffects == null || buff.OnIntervalEffects.Count == 0) return;
            if (runtime.SourceContextId == 0) return;

            try
            {
                var remainingSeconds = runtime.Remaining;
                var durationMs = float.IsInfinity(remainingSeconds)
                    ? 0
                    : Math.Max(0, (int)Math.Ceiling(remainingSeconds * 1000f));

                var handle = _periodic.Start(new MobaContinuousPeriodicSpec
                {
                    ActionKind = MobaContinuousPeriodicActionKind.TriggerPlan,
                    ActionConfigId = buff.Id,
                    SourceActorId = sourceActorId,
                    TargetActorId = targetActorId,
                    OwnerKey = runtime.SourceContextId,
                    SourceContextId = runtime.SourceContextId,
                    SkillRuntimeHandle = runtime.SkillRuntimeHandle,
                    DurationMs = durationMs,
                    PeriodMs = buff.IntervalMs,
                    OnTickTriggerIds = buff.OnIntervalEffects,
                });

                if (handle != null)
                {
                    runtime.PeriodicInstanceId = handle.InstanceId;
                }
            }
            catch (Exception ex)
            {
                Log.Exception(ex, $"[BuffPeriodicEffectBinder] TryStartPeriodicEffectByBuff exception (buffId={buff.Id}, intervalMs={buff.IntervalMs})");
            }
        }
    }
}
