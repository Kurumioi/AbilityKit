using System.Collections.Generic;
using AbilityKit.Demo.Moba.Config.BattleDemo.MO;
using AbilityKit.Demo.Moba;
using AbilityKit.Demo.Moba.Components;
using AbilityKit.Effect;
using AbilityKit.Core.Eventing;
using AbilityKit.Demo.Moba.Events.Buff;
using AbilityKit.Trace;

namespace AbilityKit.Demo.Moba.Services
{
    internal sealed class BuffEventPublisher
    {
        private readonly AbilityKit.Triggering.Eventing.IEventBus _eventBus;

        public BuffEventPublisher(AbilityKit.Triggering.Eventing.IEventBus eventBus)
        {
            _eventBus = eventBus;
        }

        public void PublishApplyOrRefresh(BuffMO buff, int sourceActorId, int targetActorId, float durationSeconds, BuffRuntime runtime)
        {
            if (_eventBus == null) return;
            if (buff == null) return;

            PublishBaseEvent(MobaBuffTriggering.Events.ApplyOrRefresh, buff.Id, sourceActorId, targetActorId, durationSeconds, runtime);
        }

        public void PublishRemove(BuffMO buff, int sourceActorId, int targetActorId, BuffRuntime runtime, TraceLifecycleReason reason)
        {
            if (_eventBus == null) return;
            if (buff == null) return;

            PublishStageEvent(MobaBuffTriggering.Events.Remove, buff.OnRemoveEffects, MobaBuffTriggering.Stages.Remove, buff.Id, sourceActorId, targetActorId, runtime, reason);
        }

        public void PublishPerEffect(string baseEventId, IReadOnlyList<int> effectIds, string stage, int sourceActorId, int targetActorId, BuffRuntime runtime)
        {
            if (_eventBus == null) return;
            if (string.IsNullOrEmpty(baseEventId)) return;
            if (effectIds == null || effectIds.Count == 0) return;

            for (int i = 0; i < effectIds.Count; i++)
            {
                var effectId = effectIds[i];
                if (effectId <= 0) continue;

                var eventId = MobaBuffTriggering.Events.WithEffect(baseEventId, effectId);
                PublishEvent(CreateArgs(eventId, runtime != null ? runtime.BuffId : 0, effectId, stage, sourceActorId, targetActorId, 0f, TraceLifecycleReason.None, runtime));
            }
        }

        public void PublishInterval(BuffMO buff, int sourceActorId, int targetActorId, BuffRuntime runtime)
        {
            if (_eventBus == null) return;
            if (buff == null) return;

            PublishEvent(CreateArgs(MobaBuffTriggering.Events.Interval, runtime != null ? runtime.BuffId : 0, 0, MobaBuffTriggering.Stages.Interval, sourceActorId, targetActorId, 0f, TraceLifecycleReason.None, runtime));
        }

        private void PublishBaseEvent(string eventId, int buffId, int sourceActorId, int targetActorId, float durationSeconds, BuffRuntime runtime)
        {
            PublishEvent(CreateArgs(eventId, buffId, 0, null, sourceActorId, targetActorId, durationSeconds, TraceLifecycleReason.None, runtime));
        }

        private void PublishStageEvent(string baseEventId, IReadOnlyList<int> effectIds, string stage, int buffId, int sourceActorId, int targetActorId, BuffRuntime runtime, TraceLifecycleReason reason)
        {
            if (_eventBus == null) return;
            if (string.IsNullOrEmpty(baseEventId)) return;

            PublishEvent(CreateArgs(baseEventId, buffId, 0, stage, sourceActorId, targetActorId, 0f, reason, runtime));

            if (effectIds == null || effectIds.Count == 0) return;

            for (int i = 0; i < effectIds.Count; i++)
            {
                var effectId = effectIds[i];
                if (effectId <= 0) continue;

                var eventId = MobaBuffTriggering.Events.WithEffect(baseEventId, effectId);
                PublishEvent(CreateArgs(eventId, buffId, effectId, stage, sourceActorId, targetActorId, 0f, reason, runtime));
            }
        }

        private static BuffEventArgs CreateArgs(string eventId, int buffId, int effectId, string stage, int sourceActorId, int targetActorId, float durationSeconds, TraceLifecycleReason reason, BuffRuntime runtime)
        {
            return new BuffEventArgs
            {
                EventId = eventId,
                SourceActorId = sourceActorId,
                TargetActorId = targetActorId,
                BuffId = buffId,
                EffectId = effectId,
                Stage = stage,
                StackCount = runtime != null ? runtime.StackCount : 0,
                DurationSeconds = durationSeconds,
                RemoveReason = reason,
                SourceContextId = runtime != null ? runtime.SourceContextId : 0,
                Runtime = runtime,
            };
        }

        private void PublishEvent(BuffEventArgs args)
        {
            var eid = AbilityKit.Demo.Moba.Services.TriggeringIdUtil.GetEventEid(args.EventId);
            var key = new EventKey<BuffEventArgs>(eid);
            _eventBus.Publish(key, in args);
            object boxed = args;
            _eventBus.Publish(new EventKey<object>(eid), in boxed);
        }
    }
}
