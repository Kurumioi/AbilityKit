using System;
using System.Collections.Generic;
using AbilityKit.Ability.FrameSync;
using AbilityKit.Ability.Triggering.Runtime;
using AbilityKit.Ability.World.DI;
using AbilityKit.Core.Logging;
using AbilityKit.Core.Continuous;
using AbilityKit.Demo.Moba.Config.BattleDemo.MO;
using AbilityKit.Demo.Moba.Config.Core;
using AbilityKit.Demo.Moba.Components;
using AbilityKit.GameplayTags;
using AbilityKit.Trace;

namespace AbilityKit.Demo.Moba.Services
{
    internal readonly struct BuffLifecycleRejectResult
    {
        public static readonly BuffLifecycleRejectResult None = new BuffLifecycleRejectResult(null, null);

        public BuffLifecycleRejectResult(string code, string message)
        {
            Code = code;
            Message = message;
        }

        public string Code { get; }
        public string Message { get; }
        public bool HasValue => !string.IsNullOrEmpty(Code) || !string.IsNullOrEmpty(Message);

        public override string ToString()
        {
            if (string.IsNullOrEmpty(Code)) return string.IsNullOrEmpty(Message) ? "unknown" : Message;
            if (string.IsNullOrEmpty(Message)) return Code;
            return Code + ": " + Message;
        }
    }

    internal sealed class BuffLifecycleExecutor
    {
        private readonly MobaConfigDatabase _configs;
        private readonly MobaActorLookupService _actors;
        private readonly IMobaEffectiveTagQueryService _tags;
        private readonly IMobaContinuousTagTemplateRegistry _tagTemplates;
        private readonly BuffRepository _repo;
        private readonly BuffContextService _ctx;
        private readonly BuffEventPublisher _events;
        private readonly BuffStageEffectExecutor _stageEffects;
        private readonly BuffStackingPolicyApplier _stacking;
        private readonly IContinuousManager _continuous;
        private readonly MobaSkillCastRuntimeService _skillRuntimes;
        private readonly MobaBuffPresentationCueReporter _presentationCues;

        public BuffLifecycleRejectResult LastReject { get; private set; }
        public string LastRejectReason => LastReject.Message;

        public BuffLifecycleExecutor(
            MobaConfigDatabase configs,
            MobaActorLookupService actors,
            IMobaEffectiveTagQueryService tags,
            IMobaContinuousTagTemplateRegistry tagTemplates,
            BuffRepository repo,
            BuffContextService ctx,
            BuffEventPublisher events,
            BuffStageEffectExecutor stageEffects,
            BuffStackingPolicyApplier stacking,
            IContinuousManager continuous,
            MobaSkillCastRuntimeService skillRuntimes,
            MobaBuffPresentationCueReporter presentationCues)
        {
            _configs = configs;
            _actors = actors;
            _tags = tags;
            _tagTemplates = tagTemplates;
            _repo = repo ?? new BuffRepository();
            _ctx = ctx;
            _events = events;
            _stageEffects = stageEffects;
            _stacking = stacking ?? new BuffStackingPolicyApplier();
            _continuous = continuous;
            _skillRuntimes = skillRuntimes;
            _presentationCues = presentationCues;
        }

        public bool Apply(BuffApplyRequest request)
        {
            LastReject = BuffLifecycleRejectResult.None;
            if (request == null) return Reject("buff.apply.nullRequest", "apply request is null.");
            if (!request.IsValid) return Reject("buff.apply.invalidRequest", $"apply request is invalid. target={request.TargetActorId} buffId={request.BuffId} source={request.SourceActorId}.");
            if (_configs == null) return Reject("buff.apply.configDatabaseMissing", $"config database is missing. target={request.TargetActorId} buffId={request.BuffId} source={request.SourceActorId}.");
            if (!_configs.TryGetBuff(request.BuffId, out var buff) || buff == null) return Reject("buff.apply.configNotFound", $"buff config not found. target={request.TargetActorId} buffId={request.BuffId} source={request.SourceActorId}.");

            if (!TryGetTarget(request.TargetActorId, out var target)) return Reject("buff.apply.targetNotFound", $"target actor not found. target={request.TargetActorId} buffId={request.BuffId} source={request.SourceActorId}.");

            var targetActorId = request.TargetActorId;
            var requirements = BuffTagLifecycle.ResolveRequirements(buff, _tagTemplates);
            if (!BuffTagLifecycle.CanActivate(_tags, targetActorId, requirements)) return Reject("buff.apply.tagRequirementsBlocked", $"tag requirements blocked activation. target={targetActorId} buffId={request.BuffId} source={request.SourceActorId}.");

            var list = _repo.GetOrCreateList(target);
            if (list == null) return Reject("buff.apply.runtimeListUnavailable", $"buff runtime list is unavailable. target={targetActorId} buffId={request.BuffId} source={request.SourceActorId}.");

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
            LastReject = BuffLifecycleRejectResult.None;
            if (request == null) return Reject("buff.remove.nullRequest", "remove request is null.");
            if (!request.IsValid) return Reject("buff.remove.invalidRequest", $"remove request is invalid. target={request.TargetActorId} buffId={request.BuffId} source={request.SourceActorId} reason={request.Reason}.");
            if (!TryGetTarget(request.TargetActorId, out var target)) return Reject("buff.remove.targetNotFound", $"target actor not found. target={request.TargetActorId} buffId={request.BuffId} source={request.SourceActorId} reason={request.Reason}.");
            if (!target.hasBuffs) return Reject("buff.remove.buffsComponentMissing", $"target has no buffs component. target={request.TargetActorId} buffId={request.BuffId} source={request.SourceActorId} reason={request.Reason}.");

            var list = target.buffs.Active;
            if (list == null || list.Count == 0) return Reject("buff.remove.noActiveRuntimes", $"target has no active buff runtimes. target={request.TargetActorId} buffId={request.BuffId} source={request.SourceActorId} reason={request.Reason}.");

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

            if (!removed) return Reject("buff.remove.runtimeNotFound", $"buff runtime not found. target={request.TargetActorId} buffId={request.BuffId} source={request.SourceActorId} reason={request.Reason}.");
            return true;
        }

        private bool Reject(string code, string message)
        {
            LastReject = new BuffLifecycleRejectResult(code, message);
            return false;
        }

        private bool Reject(string message)
        {
            return Reject("buff.lifecycle.rejected", message);
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

            var continuousReason = ToContinuousEndReason(normalizedReason);
            EndContinuous(runtime, continuousReason);
            CleanupContinuousBindings(target, targetActorId, runtime, applyRemovalTags: true);
            _ctx?.EndByRuntimeNoClear(runtime, normalizedReason);
            CleanupOwnerBindings(target, targetActorId, runtime.SourceContextId);
            PublishRemove(targetActorId, sourceActorId, runtime, normalizedReason);
            ReportEnded(targetActorId, sourceActorId, runtime, normalizedReason);
            ReleaseSkillRuntime(runtime);

            new BuffRuntimeView(runtime).ClearRuntimeBindings();
            var removedFromList = false;
            if (list != null && index >= 0 && index < list.Count && ReferenceEquals(list[index], runtime))
            {
                list.RemoveAt(index);
                removedFromList = true;
            }

            LogBuffCleanup(buffId, targetActorId, sourceActorId, sourceContextId, normalizedReason, hadContinuous, runtime.Continuous == null, hadSkillRuntimeRetain, !runtime.SkillRuntimeRetainHandle.IsValid, hadModifierBindings, runtime.ModifierBindings == null, removedFromList);
        }

        private static void LogBuffCleanup(int buffId, int targetActorId, int sourceActorId, long sourceContextId, TraceLifecycleReason reason, bool hadContinuous, bool continuousCleared, bool hadSkillRuntimeRetain, bool skillRuntimeCleared, bool hadModifierBindings, bool modifierBindingsCleared, bool removedFromList)
        {
            var message = $"[MobaBuffCleanup] buff ended. buffId={buffId}, target={targetActorId}, source={sourceActorId}, sourceContextId={sourceContextId}, reason={reason}, hadContinuous={hadContinuous}, continuousCleared={continuousCleared}, hadSkillRuntimeRetain={hadSkillRuntimeRetain}, skillRuntimeCleared={skillRuntimeCleared}, hadModifierBindings={hadModifierBindings}, modifierBindingsCleared={modifierBindingsCleared}, removedFromList={removedFromList}";
            if (IsExpectedLifecycleEnd(reason))
            {
                Log.Info(message);
                return;
            }

            Log.Warning(message);
        }

        private static bool IsExpectedLifecycleEnd(TraceLifecycleReason reason)
        {
            return reason == TraceLifecycleReason.Expired || reason == TraceLifecycleReason.Completed;
        }

        private bool ApplyToExisting(global::ActorEntity target, BuffOperationContext context)
        {
            if (context == null) return Reject("apply existing context is null.");
            var runtime = context.Runtime;
            var buff = context.Buff;
            var request = context.ApplyRequest;
            if (runtime == null) return Reject("existing buff runtime is null.");
            if (buff == null) return Reject("existing buff config is null.");
            if (request == null) return Reject("existing buff apply request is null.");

            var runtimeState = context.RuntimeView;
            var oldStackCount = runtime.StackCount;
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
                return Reject($"continuous runtime activation failed for existing buff. target={context.TargetActorId} buffId={buff.Id} source={request.SourceActorId} sourceContextId={runtime.SourceContextId}.");
            }

            UpsertOngoingTriggerPlans(target, runtime.SourceContextId, buff);
            _events?.PublishApplyOrRefresh(buff, request.SourceActorId, context.TargetActorId, context.DurationSeconds, runtime);
            ReportExistingApplied(buff, request.SourceActorId, context.TargetActorId, runtime, oldStackCount, applied);
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
            if (context == null) return Reject("apply new context is null.");
            var buff = context.Buff;
            var request = context.ApplyRequest;
            if (buff == null) return Reject("new buff config is null.");
            if (request == null) return Reject("new buff apply request is null.");

            var runtime = _stacking.CreateNewRuntime(buff, request.SourceActorId, context.DurationSeconds);
            context.Runtime = runtime;
            _ctx?.EnsureBuffContext(runtime, buff.Id, request.SourceActorId, context.TargetActorId, request.Origin);
            BindSkillRuntime(runtime, request);
            context.RuntimeView.SetTagRequirements(context.Requirements);
            if (!EnsureContinuousRuntime(runtime, buff, request.SourceActorId, context.TargetActorId, context.DurationSeconds, context.Requirements))
            {
                _ctx?.CancelAndEnd(runtime);
                ReleaseSkillRuntime(runtime);
                return Reject($"continuous runtime activation failed for new buff. target={context.TargetActorId} buffId={buff.Id} source={request.SourceActorId} sourceContextId={runtime.SourceContextId}.");
            }

            list.Add(runtime);
 
            UpsertOngoingTriggerPlans(target, runtime.SourceContextId, buff);
            _events?.PublishApplyOrRefresh(buff, request.SourceActorId, context.TargetActorId, context.DurationSeconds, runtime);
            _presentationCues?.Started(buff, request.SourceActorId, context.TargetActorId, runtime);
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

        private void ReportExistingApplied(BuffMO buff, int sourceActorId, int targetActorId, BuffRuntime runtime, int oldStackCount, bool applied)
        {
            if (!applied) return;
            if (runtime == null) return;

            if (runtime.StackCount != oldStackCount)
            {
                _presentationCues?.StackChanged(buff, sourceActorId, targetActorId, runtime);
                return;
            }

            _presentationCues?.Refreshed(buff, sourceActorId, targetActorId, runtime);
        }

        private void ReportEnded(int targetActorId, int sourceActorId, BuffRuntime runtime, TraceLifecycleReason reason)
        {
            if (_configs == null) return;
            if (runtime == null) return;
            if (!_configs.TryGetBuff(runtime.BuffId, out var buff) || buff == null) return;

            _presentationCues?.Ended(buff, sourceActorId, targetActorId, runtime, reason);
        }

        private bool BindSkillRuntime(BuffRuntime runtime, BuffApplyRequest request)
        {
            if (runtime == null) return false;
            if (request == null) return false;
            if (!request.SkillRuntimeHandle.IsValid) return true;
            if (runtime.SkillRuntimeRetainHandle.IsValid) return true;

            var childId = runtime.SourceContextId;
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
            runtime.Continuous.BindRuntime(runtime);
            runtime.Continuous.BindSourceContext(runtime.SourceContextId);
            runtime.Continuous.Refresh(sourceActorId, remainingSeconds, runtime.StackCount, buff.MaxStacks, requirements);
            runtime.Continuous.IntervalRemainingSeconds = runtime.IntervalRemainingSeconds;
 
            var activated = false;
            if (_continuous == null)
            {
                activated = false;
            }
            else if (wasActive)
            {
                activated = true;
                if (_continuous is MobaContinuousManager mobaContinuous)
                {
                    mobaContinuous.Reproject(runtime.Continuous);
                }
            }
            else
            {
                activated = _continuous.TryActivate(runtime.Continuous);
            }

            return activated;
        }

        private void EndContinuous(BuffRuntime runtime, ContinuousEndReason reason)
        {
            var continuous = runtime?.Continuous;
            if (continuous == null) return;

            _continuous?.TryEnd(continuous, reason);
        }

        private void CleanupContinuousBindings(global::ActorEntity target, int targetActorId, BuffRuntime runtime, bool applyRemovalTags)
        {
            if (runtime == null) return;

            var continuous = runtime.Continuous;
            if (continuous != null)
            {
                if (!continuous.IsTerminated)
                {
                    EndContinuous(runtime, ContinuousEndReason.CleanedUp);
                }

                if (ReferenceEquals(continuous.Runtime, runtime))
                {
                    continuous.BindRuntime(null);
                }
            }

            runtime.Continuous = null;
            runtime.TagRequirements = null;

            if (applyRemovalTags && targetActorId > 0)
            {
                _tags?.MarkDirty(targetActorId);
            }
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

    internal static class BuffLifecycleExecutorFactory
    {
        public static BuffLifecycleExecutor Create(IWorldResolver services)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services), "Buff lifecycle executor requires a world service resolver.");
            }

            services.TryResolve(out MobaConfigDatabase configs);
            services.TryResolve(out AbilityKit.Triggering.Eventing.IEventBus eventBus);
            services.TryResolve(out ITriggerActionRunner actionRunner);
            services.TryResolve(out MobaTraceRegistry trace);
            services.TryResolve(out MobaEffectExecutionService effects);
            services.TryResolve(out IMobaEffectiveTagQueryService tags);
            services.TryResolve(out IMobaContinuousTagTemplateRegistry tagTemplates);
            services.TryResolve(out IFrameTime frameTime);
            services.TryResolve(out IContinuousManager continuous);
            services.TryResolve(out MobaActorLookupService actors);
            services.TryResolve(out MobaSkillCastRuntimeService skillRuntimes);
            services.TryResolve(out MobaPresentationCueSnapshotService cueSnapshots);
 
            var repo = new BuffRepository();
            var ctx = new BuffContextService(trace, actionRunner, frameTime);
            var events = new BuffEventPublisher(eventBus);
            var stageEffects = new BuffStageEffectExecutor(effects);
            var stacking = new BuffStackingPolicyApplier();
            var presentationCues = new MobaBuffPresentationCueReporter(configs, cueSnapshots);

            return new BuffLifecycleExecutor(
                configs,
                actors,
                tags,
                tagTemplates,
                repo,
                ctx,
                events,
                stageEffects,
                stacking,
                continuous,
                skillRuntimes,
                presentationCues);
        }
    }
}
