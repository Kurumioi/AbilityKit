using System.Collections.Generic;
using AbilityKit.Core.Logging;
using AbilityKit.Demo.Moba.Components;
using AbilityKit.Demo.Moba.Config.Core;
using AbilityKit.Demo.Moba.Services.Buffs.Core;
using AbilityKit.Demo.Moba.Services.Buffs.Presentation;
using AbilityKit.Demo.Moba.Services.Buffs.Runtime;
using AbilityKit.Demo.Moba.Services.Buffs.Triggering;
using AbilityKit.Trace;

namespace AbilityKit.Demo.Moba.Services.Buffs.Lifecycle
{
    /// <summary>
    /// Buff 结束流程：集中维护持续行为停止、上下文收尾、事件表现派发、技能运行时释放和对象池回收顺序。
    /// </summary>
    internal sealed class BuffEndFlow
    {
        private readonly MobaConfigDatabase _configs;
        private readonly BuffContextRegistry _ctx;
        private readonly BuffLifecycleNotifier _notifier;
        private readonly BuffRuntimeBindingCoordinator _bindings;

        public BuffEndFlow(
            MobaConfigDatabase configs,
            BuffContextRegistry ctx,
            BuffLifecycleNotifier notifier,
            BuffRuntimeBindingCoordinator bindings)
        {
            _configs = configs;
            _ctx = ctx;
            _notifier = notifier;
            _bindings = bindings;
        }

        public void EndRuntime(global::ActorEntity target, List<BuffRuntime> list, int index, BuffRuntime runtime, int sourceActorId, TraceLifecycleReason reason)
        {
            if (target == null) return;
            if (!target.hasActorId) return;
            if (runtime == null) return;

            var targetActorId = target.actorId.Value;
            var normalizedReason = reason == TraceLifecycleReason.None ? TraceLifecycleReason.Expired : reason;
            var buffId = runtime.BuffId;
            var sourceContextId = runtime.SourceContextId;
            var hadContinuous = runtime.Continuous != null;
            var hadSkillRuntimeRetain = runtime.SkillRuntimeRetainHandle.IsValid;
            var hadModifierBindings = runtime.ModifierBindings != null && runtime.ModifierBindings.Count > 0;

            _bindings?.EndContinuous(runtime, normalizedReason);
            _bindings?.CleanupContinuous(target, targetActorId, runtime, applyRemovalTags: true);
            _ctx?.EndByRuntimeNoClear(runtime, normalizedReason);
            CleanupOwnerBindings(target, runtime.SourceContextId);
            NotifyRemoved(targetActorId, sourceActorId, runtime, normalizedReason);
            ReleaseSkillRuntime(runtime);
            NotifyLifecycle(runtime, MobaRuntimeLifecycleEventKind.Ended, "buff.lifecycle.ended");
 
            var removedFromList = BuffRepository.RemoveAt(list, index, runtime);
            if (removedFromList)
            {
                new BuffRuntimeView(runtime).ClearRuntimeBindings();
                BuffRepository.ReleaseRuntime(runtime);
            }

            LogBuffCleanup(buffId, targetActorId, sourceActorId, sourceContextId, normalizedReason, hadContinuous, removedFromList, hadSkillRuntimeRetain, removedFromList, hadModifierBindings, removedFromList, removedFromList);
        }

        public void CleanupOwnerBindings(global::ActorEntity target, long ownerKey)
        {
            if (target == null) return;
            if (ownerKey == 0) return;
 
            RemoveEffectListeners(target, ownerKey);
        }

        public void ReleaseSkillRuntime(BuffRuntime runtime)
        {
            _bindings?.ReleaseSkillRuntime(runtime);
        }

        public void NotifyLifecycle(BuffRuntime runtime, MobaRuntimeLifecycleEventKind kind, string reason)
        {
            _bindings?.NotifyLifecycle(runtime, kind, reason);
        }

        private void NotifyRemoved(int targetActorId, int sourceActorId, BuffRuntime runtime, TraceLifecycleReason reason)
        {
            if (_configs == null) return;
            if (runtime == null) return;
            if (!_configs.TryGetBuff(runtime.BuffId, out var buff) || buff == null) return;

            _notifier?.Removed(buff, sourceActorId, targetActorId, runtime, reason);
        }

        private static void LogBuffCleanup(int buffId, int targetActorId, int sourceActorId, long sourceContextId, TraceLifecycleReason reason, bool hadContinuous, bool continuousCleared, bool hadSkillRuntimeRetain, bool skillRuntimeCleared, bool hadModifierBindings, bool modifierBindingsCleared, bool removedFromList)
        {
            if (IsExpectedLifecycleEnd(reason)) return;

            Log.Warning($"[MobaBuffCleanup] buff ended unexpectedly. buffId={buffId}, target={targetActorId}, source={sourceActorId}, sourceContextId={sourceContextId}, reason={reason}, hadContinuous={hadContinuous}, continuousCleared={continuousCleared}, hadSkillRuntimeRetain={hadSkillRuntimeRetain}, skillRuntimeCleared={skillRuntimeCleared}, hadModifierBindings={hadModifierBindings}, modifierBindingsCleared={modifierBindingsCleared}, removedFromList={removedFromList}");
        }

        private static bool IsExpectedLifecycleEnd(TraceLifecycleReason reason)
        {
            return reason == TraceLifecycleReason.Expired || reason == TraceLifecycleReason.Completed;
        }

        private static void RemoveEffectListeners(global::ActorEntity e, long ownerKey)
        {
            if (e == null) return;
            if (ownerKey == 0) return;
            if (!e.hasEffectListeners) return;

            var listeners = e.effectListeners.Active;
            if (listeners == null || listeners.Count == 0) return;

            for (int i = listeners.Count - 1; i >= 0; i--)
            {
                var listener = listeners[i];
                if (listener == null) continue;
                if (listener.SourceContextId != ownerKey) continue;
                listeners.RemoveAt(i);
            }
        }
    }
}
