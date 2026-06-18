using AbilityKit.Demo.Moba.Components;
using AbilityKit.Demo.Moba.Config.BattleDemo.MO;
using AbilityKit.Demo.Moba.Services.Buffs.Presentation;
using AbilityKit.Demo.Moba.Services.Buffs.Triggering;
using AbilityKit.Trace;

namespace AbilityKit.Demo.Moba.Services.Buffs.Lifecycle
{
    /// <summary>
    /// Buff 生命周期派发器：统一维护事件、表现提示和 stage effect 的对外通知顺序。
    /// </summary>
    internal sealed class BuffLifecycleNotifier
    {
        private readonly BuffEventPublisher _events;
        private readonly BuffStageEffectExecutor _stageEffects;
        private readonly MobaBuffPresentationCueReporter _presentationCues;

        public BuffLifecycleNotifier(
            BuffEventPublisher events,
            BuffStageEffectExecutor stageEffects,
            MobaBuffPresentationCueReporter presentationCues)
        {
            _events = events;
            _stageEffects = stageEffects;
            _presentationCues = presentationCues;
        }

        public void AppliedNew(BuffMO buff, int sourceActorId, int targetActorId, float durationSeconds, BuffRuntime runtime)
        {
            if (buff == null || runtime == null) return;

            _events?.PublishApplyOrRefresh(buff, sourceActorId, targetActorId, durationSeconds, runtime);
            _presentationCues?.Started(buff, sourceActorId, targetActorId, runtime);
            DispatchAddEffects(buff, sourceActorId, targetActorId, durationSeconds, runtime);
        }

        public void AppliedExisting(BuffMO buff, int sourceActorId, int targetActorId, float durationSeconds, BuffRuntime runtime, int oldStackCount, bool applied)
        {
            if (buff == null || runtime == null) return;

            _events?.PublishApplyOrRefresh(buff, sourceActorId, targetActorId, durationSeconds, runtime);
            ReportExistingApplied(buff, sourceActorId, targetActorId, runtime, oldStackCount, applied);
            if (applied)
            {
                DispatchAddEffects(buff, sourceActorId, targetActorId, durationSeconds, runtime);
            }
        }

        public void Removed(BuffMO buff, int sourceActorId, int targetActorId, BuffRuntime runtime, TraceLifecycleReason reason)
        {
            if (buff == null || runtime == null) return;

            _events?.PublishRemove(buff, sourceActorId, targetActorId, runtime, reason);
            _presentationCues?.Ended(buff, sourceActorId, targetActorId, runtime, reason);
            _stageEffects?.Execute(buff.OnRemoveEffects, buff.Id, sourceActorId, targetActorId, runtime.SourceContextId, MobaBuffTriggering.Stages.Remove, runtime, reason);
        }

        private void DispatchAddEffects(BuffMO buff, int sourceActorId, int targetActorId, float durationSeconds, BuffRuntime runtime)
        {
            _stageEffects?.Execute(buff.OnAddEffects, buff.Id, sourceActorId, targetActorId, runtime.SourceContextId, MobaBuffTriggering.Stages.Add, runtime, durationSeconds: durationSeconds);
            _events?.PublishPerEffect(MobaBuffTriggering.Events.ApplyOrRefresh, buff.OnAddEffects, MobaBuffTriggering.Stages.Add, sourceActorId, targetActorId, runtime);
        }

        private void ReportExistingApplied(BuffMO buff, int sourceActorId, int targetActorId, BuffRuntime runtime, int oldStackCount, bool applied)
        {
            if (!applied) return;

            if (runtime.StackCount != oldStackCount)
            {
                _presentationCues?.StackChanged(buff, sourceActorId, targetActorId, runtime);
                return;
            }

            _presentationCues?.Refreshed(buff, sourceActorId, targetActorId, runtime);
        }
    }
}
