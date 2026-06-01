using System;
using System.Collections.Generic;
using AbilityKit.Attributes.Core;
using AbilityKit.Core.Common.Log;
using AbilityKit.Core.Continuous;
using AbilityKit.Demo.Moba.Config.BattleDemo.MO;
using AbilityKit.Demo.Moba.Config.Core;
using AbilityKit.Demo.Moba.Components;
using AbilityKit.GameplayTags;
using AbilityKit.Trace;

namespace AbilityKit.Demo.Moba.Services
{
    internal sealed class BuffLifecycleExecutor
    {
        private readonly MobaConfigDatabase _configs;
        private readonly MobaActorLookupService _actors;
        private readonly MobaPeriodicEffectService _ongoing;
        private readonly IGameplayTagService _tags;
        private readonly IMobaContinuousTagTemplateRegistry _tagTemplates;
        private readonly BuffRepository _repo;
        private readonly BuffContextService _ctx;
        private readonly BuffEventPublisher _events;
        private readonly BuffPeriodicEffectBinder _periodicBinder;
        private readonly BuffStageEffectExecutor _stageEffects;
        private readonly BuffStackingPolicyApplier _stacking;
        private readonly IContinuousManager _continuous;
        private readonly MobaSkillCastRuntimeService _skillRuntimes;

        public BuffLifecycleExecutor(
            MobaConfigDatabase configs,
            MobaActorLookupService actors,
            MobaPeriodicEffectService ongoing,
            IGameplayTagService tags,
            IMobaContinuousTagTemplateRegistry tagTemplates,
            BuffRepository repo,
            BuffContextService ctx,
            BuffEventPublisher events,
            BuffPeriodicEffectBinder periodicBinder,
            BuffStageEffectExecutor stageEffects,
            BuffStackingPolicyApplier stacking,
            IContinuousManager continuous,
            MobaSkillCastRuntimeService skillRuntimes)
        {
            _configs = configs;
            _actors = actors;
            _ongoing = ongoing;
            _tags = tags;
            _tagTemplates = tagTemplates;
            _repo = repo ?? new BuffRepository();
            _ctx = ctx;
            _events = events;
            _periodicBinder = periodicBinder;
            _stageEffects = stageEffects;
            _stacking = stacking ?? new BuffStackingPolicyApplier();
            _continuous = continuous;
            _skillRuntimes = skillRuntimes;
        }

        public bool Apply(BuffApplyRequest request)
        {
            if (request == null || !request.IsValid) return false;
            if (_configs == null) return false;
            if (!_configs.TryGetBuff(request.BuffId, out var buff) || buff == null) return false;

            if (!TryGetTarget(request.TargetActorId, out var target)) return false;

            var targetActorId = request.TargetActorId;
            var requirements = BuffTagLifecycle.ResolveRequirements(buff, _tagTemplates);
            if (!BuffTagLifecycle.CanActivate(_tags, targetActorId, requirements)) return false;

            var list = _repo.GetOrCreateList(target);
            if (list == null) return false;

            var duration = request.DurationOverrideMs > 0 ? request.DurationOverrideMs : buff.DurationMs;
            var context = new BuffOperationContext
            {
                ApplyRequest = request,
                Buff = buff,
                TargetActorId = targetActorId,
                DurationSeconds = duration > 0 ? duration / 1000f : 0f,
                Requirements = requirements,
            };

            var existingIndex = BuffRepository.FindExistingBuffIndex(list, buff.Id);
            if (existingIndex >= 0)
            {
                context.Runtime = list[existingIndex];
                context.IsExistingRuntime = true;
                return ApplyToExisting(target, context);
            }

            return ApplyNew(target, list, context);
        }

        public bool Remove(BuffRemoveRequest request)
        {
            if (request == null || !request.IsValid) return false;
            if (!TryGetTarget(request.TargetActorId, out var target)) return false;
            if (!target.hasBuffs) return false;

            var list = target.buffs.Active;
            if (list == null || list.Count == 0) return false;

            var removed = false;
            var normalizedReason = NormalizeRemoveReason(request.Reason);
            for (int i = list.Count - 1; i >= 0; i--)
            {
                var runtime = list[i];
                if (runtime == null) continue;
                if (runtime.BuffId != request.BuffId) continue;

                removed = true;
                EndRuntime(target, list, i, runtime, request.SourceActorId, normalizedReason);
            }

            return removed;
        }

        public void EndRuntime(global::ActorEntity target, List<BuffRuntime> list, int index, BuffRuntime runtime, int sourceActorId, TraceLifecycleReason reason)
        {
            if (target == null) return;
            if (!target.hasActorId) return;
            if (runtime == null) return;

            var targetActorId = target.actorId.Value;
            var normalizedReason = reason == TraceLifecycleReason.None ? TraceLifecycleReason.Expired : reason;

            var continuousReason = ToContinuousEndReason(normalizedReason);
            EndContinuous(runtime, continuousReason);
            CleanupContinuousBindings(target, targetActorId, runtime, applyRemovalTags: true);
            _ctx?.EndByRuntimeNoClear(runtime, normalizedReason);
            CleanupOwnerBindings(target, targetActorId, runtime.SourceContextId);
            PublishRemove(targetActorId, sourceActorId, runtime, normalizedReason);
            ReleaseSkillRuntime(runtime);

            new BuffRuntimeView(runtime).ClearRuntimeBindings();
            if (list != null && index >= 0 && index < list.Count && ReferenceEquals(list[index], runtime))
            {
                list.RemoveAt(index);
            }
        }

        private bool ApplyToExisting(global::ActorEntity target, BuffOperationContext context)
        {
            if (context == null) return false;
            var runtime = context.Runtime;
            var buff = context.Buff;
            var request = context.ApplyRequest;
            if (runtime == null) return false;
            if (buff == null) return false;
            if (request == null) return false;

            var runtimeState = context.RuntimeView;
            var isReplace = buff.StackingPolicy == BuffStackingPolicy.Replace;
            var oldOwnerKey = runtime.SourceContextId;
            if (isReplace)
            {
                EndContinuous(runtime, ContinuousEndReason.Interrupted);
                CleanupContinuousBindings(target, context.TargetActorId, runtime, applyRemovalTags: false);
                _ctx?.CancelAndEnd(runtime);
                CleanupOwnerBindings(target, context.TargetActorId, oldOwnerKey);
                ReleaseSkillRuntime(runtime);
                runtimeState.ClearRuntimeBindings();
            }

            var applied = _stacking.ApplyToExisting(runtime, buff, request.SourceActorId, context.DurationSeconds);
            _ctx?.EnsureBuffContext(runtime, buff.Id, request.SourceActorId, context.TargetActorId, request.Origin);
            BindSkillRuntime(runtime, request);
            if (applied || runtime.TagRequirements == null)
            {
                runtimeState.SetTagRequirements(context.Requirements);
            }

            if (applied && !EnsureContinuousRuntime(runtime, buff, request.SourceActorId, context.TargetActorId, context.DurationSeconds, context.Requirements))
            {
                return false;
            }

            _periodicBinder?.TryStartPeriodicEffectByBuff(buff, runtime, request.SourceActorId, context.TargetActorId);
            UpsertOngoingTriggerPlans(target, runtime.SourceContextId, buff);
            _events?.PublishApplyOrRefresh(buff, request.SourceActorId, context.TargetActorId, context.DurationSeconds, runtime);
            if (applied)
            {
                BuffStackingPolicyApplier.ResetInterval(runtime, buff);
                _stageEffects?.Execute(buff.OnAddEffects, buff.Id, request.SourceActorId, context.TargetActorId, runtime.SourceContextId, MobaBuffTriggering.Stages.Add, runtime, durationSeconds: context.DurationSeconds);
                _events?.PublishPerEffect(MobaBuffTriggering.Events.ApplyOrRefresh, buff.OnAddEffects, MobaBuffTriggering.Stages.Add, request.SourceActorId, context.TargetActorId, runtime);
            }

            return true;
        }

        private bool ApplyNew(global::ActorEntity target, List<BuffRuntime> list, BuffOperationContext context)
        {
            if (context == null) return false;
            var buff = context.Buff;
            var request = context.ApplyRequest;
            if (buff == null) return false;
            if (request == null) return false;

            var runtime = _stacking.CreateNewRuntime(buff, request.SourceActorId, context.DurationSeconds);
            context.Runtime = runtime;
            _ctx?.EnsureBuffContext(runtime, buff.Id, request.SourceActorId, context.TargetActorId, request.Origin);
            BindSkillRuntime(runtime, request);
            context.RuntimeView.SetTagRequirements(context.Requirements);
            if (!EnsureContinuousRuntime(runtime, buff, request.SourceActorId, context.TargetActorId, context.DurationSeconds, context.Requirements))
            {
                _ctx?.CancelAndEnd(runtime);
                ReleaseSkillRuntime(runtime);
                return false;
            }

            list.Add(runtime);

            _periodicBinder?.TryStartPeriodicEffectByBuff(buff, runtime, request.SourceActorId, context.TargetActorId);
            UpsertOngoingTriggerPlans(target, runtime.SourceContextId, buff);
            _events?.PublishApplyOrRefresh(buff, request.SourceActorId, context.TargetActorId, context.DurationSeconds, runtime);
            _stageEffects?.Execute(buff.OnAddEffects, buff.Id, request.SourceActorId, context.TargetActorId, runtime.SourceContextId, MobaBuffTriggering.Stages.Add, runtime, durationSeconds: context.DurationSeconds);
            _events?.PublishPerEffect(MobaBuffTriggering.Events.ApplyOrRefresh, buff.OnAddEffects, MobaBuffTriggering.Stages.Add, request.SourceActorId, context.TargetActorId, runtime);
            return true;
        }

        private void PublishRemove(int targetActorId, int sourceActorId, BuffRuntime runtime, TraceLifecycleReason reason)
        {
            if (_configs == null) return;
            if (!_configs.TryGetBuff(runtime.BuffId, out var buff) || buff == null) return;

            _events?.PublishRemove(buff, sourceActorId, targetActorId, runtime, reason);
            _stageEffects?.Execute(buff.OnRemoveEffects, buff.Id, sourceActorId, targetActorId, runtime.SourceContextId, MobaBuffTriggering.Stages.Remove, runtime, reason);
        }

        private bool BindSkillRuntime(BuffRuntime runtime, BuffApplyRequest request)
        {
            if (runtime == null) return false;
            if (request == null) return false;
            if (!request.SkillRuntimeHandle.IsValid) return true;
            if (runtime.SkillRuntimeRetainHandle.IsValid) return true;

            var childId = runtime.SourceContextId != 0L ? runtime.SourceContextId : runtime.PeriodicInstanceId;
            if (childId == 0L) return false;

            var child = new MobaSkillRuntimeChildRef(MobaSkillRuntimeChildKind.Buff, childId, runtime.SourceContextId, runtime.BuffId);
            var runtimeHandle = request.SkillRuntimeHandle;
            if (_skillRuntimes != null && _skillRuntimes.RetainChild(in runtimeHandle, in child, out var retainHandle))
            {
                new BuffRuntimeView(runtime).BindSkillRuntime(in runtimeHandle, in retainHandle);
                return true;
            }

            return false;
        }

        private void ReleaseSkillRuntime(BuffRuntime runtime)
        {
            if (runtime == null) return;
            var retainHandle = runtime.SkillRuntimeRetainHandle;
            if (!retainHandle.IsValid) return;

            try
            {
                _skillRuntimes?.ReleaseChild(in retainHandle);
            }
            catch (Exception ex)
            {
                Log.Exception(ex, $"[BuffLifecycleExecutor] Release skill runtime retain failed (buffId={runtime.BuffId}, sourceContextId={runtime.SourceContextId})");
            }

            new BuffRuntimeView(runtime).ClearSkillRuntimeBinding();
        }

        private bool EnsureContinuousRuntime(BuffRuntime runtime, BuffMO buff, int sourceActorId, int targetActorId, float remainingSeconds, ContinuousTagRequirements requirements)
        {
            if (runtime == null) return false;
            if (buff == null) return false;

            if (runtime.Continuous == null || runtime.Continuous.IsTerminated)
            {
                runtime.Continuous = new BuffContinuousRuntime(buff, sourceActorId, targetActorId, remainingSeconds, requirements);
            }

            var wasActive = runtime.Continuous.IsActive;
            runtime.Continuous.BindSourceContext(runtime.SourceContextId);
            runtime.Continuous.Refresh(sourceActorId, remainingSeconds, runtime.StackCount, buff.MaxStacks, requirements);

            var activated = false;
            if (_continuous == null)
            {
                runtime.Continuous.Activate();
                activated = runtime.Continuous.IsActive;
            }
            else
            {
                activated = _continuous.TryActivate(runtime.Continuous);
            }

            if (_continuous == null && activated && !wasActive)
            {
                ApplyContinuousBindings(runtime, targetActorId);
            }

            return activated;
        }

        private void EndContinuous(BuffRuntime runtime, ContinuousEndReason reason)
        {
            var continuous = runtime?.Continuous;
            if (continuous == null) return;

            if (!continuous.IsTerminated)
            {
                continuous.End(reason);
            }

            _continuous?.Unregister(continuous, reason);
        }

        private void ApplyContinuousBindings(BuffRuntime runtime, int targetActorId)
        {
            if (runtime == null) return;
            BuffTagLifecycle.ApplyApplicationTags(_tags, targetActorId, runtime);
        }

        private void CleanupContinuousBindings(global::ActorEntity target, int targetActorId, BuffRuntime runtime, bool applyRemovalTags)
        {
            if (runtime == null) return;
            if (_continuous != null) return;

            BuffTagLifecycle.RemoveApplicationTags(_tags, targetActorId, runtime);
            if (applyRemovalTags)
            {
                BuffTagLifecycle.ApplyRemovalTags(_tags, targetActorId, runtime);
            }

            ClearModifierBindings(target, runtime);
        }

        private static void ClearModifierBindings(global::ActorEntity target, BuffRuntime runtime)
        {
            if (target == null) return;
            if (runtime == null) return;

            var modifierSourceId = runtime.Continuous?.ModifierSourceId ?? 0;
            if (modifierSourceId != 0 && target.hasAttributeGroup)
            {
                var ctx = target.attributeGroup.Ctx;
                if (ctx != null) ctx.ClearModifiers(modifierSourceId);
                else target.attributeGroup.Group?.ClearModifiers(modifierSourceId);
            }

            var bindings = runtime.ModifierBindings;
            if (bindings == null || bindings.Count == 0) return;

            if (!target.hasAttributeGroup || target.attributeGroup.Group == null)
            {
                bindings.Clear();
                return;
            }

            for (int i = 0; i < bindings.Count; i++)
            {
                var binding = bindings[i];
                if (binding == null) continue;
                target.attributeGroup.Group.RemoveModifier(AttributeId.FromRaw(binding.AttributeType), binding.Handle);
            }

            bindings.Clear();
        }

        private static ContinuousEndReason ToContinuousEndReason(TraceLifecycleReason reason)
        {
            switch (reason)
            {
                case TraceLifecycleReason.Expired:
                case TraceLifecycleReason.Completed:
                    return ContinuousEndReason.Completed;
                case TraceLifecycleReason.Dispelled:
                case TraceLifecycleReason.Interrupted:
                case TraceLifecycleReason.Cancelled:
                case TraceLifecycleReason.Dead:
                case TraceLifecycleReason.Replaced:
                case TraceLifecycleReason.Overridden:
                case TraceLifecycleReason.Failed:
                    return ContinuousEndReason.Interrupted;
                case TraceLifecycleReason.None:
                default:
                    return ContinuousEndReason.CleanedUp;
            }
        }

        private void CleanupOwnerBindings(global::ActorEntity target, int targetActorId, long ownerKey)
        {
            if (target == null) return;
            if (ownerKey == 0) return;

            try
            {
                _ongoing?.StopByOwnerKey(targetActorId, ownerKey);
            }
            catch (Exception ex)
            {
                Log.Exception(ex, $"[BuffLifecycleExecutor] StopByOwnerKey exception (ownerKey={ownerKey})");
            }

            RemoveOngoingTriggerPlansEntry(target, ownerKey);
            RemoveEffectListeners(target, ownerKey);
        }

        private bool TryGetTarget(int actorId, out global::ActorEntity target)
        {
            target = null;
            if (actorId <= 0) return false;
            if (_actors == null) return false;
            return _actors.TryGetActorEntity(actorId, out target) && target != null && target.hasActorId;
        }

        private static TraceLifecycleReason NormalizeRemoveReason(TraceLifecycleReason reason)
        {
            return reason == TraceLifecycleReason.None ? TraceLifecycleReason.Dispelled : reason;
        }

        private static void UpsertOngoingTriggerPlans(global::ActorEntity e, long ownerKey, BuffMO buff)
        {
            if (e == null) return;
            if (ownerKey == 0) return;
            if (buff == null) return;

            if (buff.TriggerIds == null || buff.TriggerIds.Count == 0)
            {
                RemoveOngoingTriggerPlansEntry(e, ownerKey);
                return;
            }

            var ids = new int[buff.TriggerIds.Count];
            for (int i = 0; i < buff.TriggerIds.Count; i++) ids[i] = buff.TriggerIds[i];

            var oldList = e.hasOngoingTriggerPlans ? e.ongoingTriggerPlans.Active : null;
            var newList = oldList != null && oldList.Count > 0 ? new List<OngoingTriggerPlanEntry>(oldList.Count + 1) : new List<OngoingTriggerPlanEntry>(1);
            var replaced = false;

            if (oldList != null)
            {
                for (int i = 0; i < oldList.Count; i++)
                {
                    var it = oldList[i];
                    if (it == null) continue;
                    if (it.OwnerKey == ownerKey)
                    {
                        newList.Add(new OngoingTriggerPlanEntry { OwnerKey = ownerKey, TriggerIds = ids });
                        replaced = true;
                    }
                    else
                    {
                        newList.Add(new OngoingTriggerPlanEntry { OwnerKey = it.OwnerKey, TriggerIds = it.TriggerIds });
                    }
                }
            }

            if (!replaced)
            {
                newList.Add(new OngoingTriggerPlanEntry { OwnerKey = ownerKey, TriggerIds = ids });
            }

            var rev = e.hasOngoingTriggerPlans ? e.ongoingTriggerPlans.Revision + 1 : 1;
            if (e.hasOngoingTriggerPlans) e.ReplaceOngoingTriggerPlans(newList, rev);
            else e.AddOngoingTriggerPlans(newList, rev);
        }

        private static void RemoveOngoingTriggerPlansEntry(global::ActorEntity e, long ownerKey)
        {
            if (e == null) return;
            if (ownerKey == 0) return;
            if (!e.hasOngoingTriggerPlans) return;

            var oldList = e.ongoingTriggerPlans.Active;
            if (oldList == null || oldList.Count == 0) return;

            var newList = new List<OngoingTriggerPlanEntry>(oldList.Count);
            var removedAny = false;

            for (int i = 0; i < oldList.Count; i++)
            {
                var it = oldList[i];
                if (it == null) continue;
                if (it.OwnerKey == ownerKey)
                {
                    removedAny = true;
                    continue;
                }

                newList.Add(new OngoingTriggerPlanEntry { OwnerKey = it.OwnerKey, TriggerIds = it.TriggerIds });
            }

            if (!removedAny) return;

            var rev = e.ongoingTriggerPlans.Revision + 1;
            if (newList.Count == 0) e.RemoveOngoingTriggerPlans();
            else e.ReplaceOngoingTriggerPlans(newList, rev);
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
