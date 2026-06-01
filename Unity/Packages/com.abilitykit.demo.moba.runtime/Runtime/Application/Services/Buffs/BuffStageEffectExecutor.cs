using System.Collections.Generic;
using AbilityKit.Demo.Moba;
using AbilityKit.Demo.Moba.Components;
using AbilityKit.Trace;

namespace AbilityKit.Demo.Moba.Services
{
    internal sealed class BuffStageEffectExecutor
    {
        private readonly MobaEffectExecutionService _effects;

        public BuffStageEffectExecutor(MobaEffectExecutionService effects)
        {
            _effects = effects;
        }

        public void Execute(IReadOnlyList<int> triggerIds, int buffId, int sourceActorId, int targetActorId, long sourceContextId, string stage, BuffRuntime runtime, TraceLifecycleReason removeReason = TraceLifecycleReason.None, float durationSeconds = 0f)
        {
            if (_effects == null) return;
            if (triggerIds == null || triggerIds.Count == 0) return;

            for (int i = 0; i < triggerIds.Count; i++)
            {
                var triggerId = triggerIds[i];
                if (triggerId <= 0) continue;

                _effects.ExecuteTriggerId(triggerId, CreatePayload(triggerId, buffId, sourceActorId, targetActorId, sourceContextId, stage, runtime, removeReason, durationSeconds));
            }
        }

        private static BuffTriggerContext CreatePayload(int triggerId, int buffId, int sourceActorId, int targetActorId, long sourceContextId, string stage, BuffRuntime runtime, TraceLifecycleReason removeReason, float durationSeconds)
        {
            return new BuffTriggerContext
            {
                SourceActorId = sourceActorId,
                TargetActorId = targetActorId,
                SourceContextId = sourceContextId,
                TriggerId = triggerId,
                BuffId = buffId,
                Stage = stage,
                StackCountSnapshot = runtime != null ? runtime.StackCount : 0,
                RemainingSecondsSnapshot = runtime != null ? runtime.Remaining : 0f,
                DurationSecondsSnapshot = durationSeconds,
                RemoveReason = removeReason,
                Runtime = runtime,
            };
        }
    }

    internal interface IBuffTriggerContext : IMobaTriggerInvocationContext, IMobaActorContextProvider
    {
        int BuffId { get; }
        string Stage { get; }
        int StackCountSnapshot { get; }
        float RemainingSecondsSnapshot { get; }
        float DurationSecondsSnapshot { get; }
        int StackCount { get; }
        float DurationSeconds { get; }
        TraceLifecycleReason RemoveReason { get; }
        bool TryGetBuffRuntime(out BuffRuntime runtime);
    }

    internal sealed class BuffTriggerContext : IBuffTriggerContext, IMobaTriggerLineageContextProvider, IMobaTriggerTraceContextProvider, IMobaTriggerRuntimeContext<BuffRuntime>, IMobaTriggerSkillRuntimeContext, IMobaOriginContextProvider
    {
        public int TriggerId { get; set; }
        public EffectContextKind Kind => EffectContextKind.Buff;
        public int SourceActorId { get; set; }
        public int TargetActorId { get; set; }
        public long SourceContextId { get; set; }
        public int BuffId { get; set; }
        public string Stage { get; set; }
        public int StackCountSnapshot { get; set; }
        public float RemainingSecondsSnapshot { get; set; }
        public float DurationSecondsSnapshot { get; set; }
        public int StackCount
        {
            get => StackCountSnapshot;
            set => StackCountSnapshot = value;
        }
        public float DurationSeconds
        {
            get => DurationSecondsSnapshot;
            set => DurationSecondsSnapshot = value;
        }
        public TraceLifecycleReason RemoveReason { get; set; }
        public BuffRuntime Runtime { get; set; }
        public MobaSkillCastRuntimeHandle SkillRuntimeHandle => Runtime != null ? Runtime.SkillRuntimeHandle : default;
        public MobaTriggerLineageContext LineageContext => new MobaTriggerLineageContext(Kind, MobaBuffTriggering.Stages.IsRemove(Stage) ? MobaTraceKind.BuffRemove : MobaTraceKind.BuffApply, SourceActorId, TargetActorId, SourceContextId, SourceContextId, SourceContextId, BuffId);
        public MobaTriggerTraceContext TraceContext => LineageContext.ToTraceContext();
        public MobaGameplayOrigin Origin
        {
            get
            {
                var lineageContext = LineageContext;
                var handle = SkillRuntimeHandle;
                return MobaGameplayOrigin.FromLineageContext(in lineageContext, in handle);
            }
        }

        public bool TryGetLineageContext(out MobaTriggerLineageContext lineageContext)
        {
            lineageContext = LineageContext;
            return true;
        }

        public bool TryGetTraceContext(out MobaTriggerTraceContext traceContext)
        {
            traceContext = TraceContext;
            return true;
        }

        public bool TryGetRuntime(out BuffRuntime runtime)
        {
            runtime = Runtime;
            return runtime != null;
        }

        public bool TryGetBuffRuntime(out BuffRuntime runtime) => TryGetRuntime(out runtime);

        public bool TryGetSourceActorId(out int actorId)
        {
            actorId = SourceActorId;
            return actorId > 0;
        }

        public bool TryGetTargetActorId(out int actorId)
        {
            actorId = TargetActorId;
            return actorId > 0;
        }

        public bool TryGetOrigin(out MobaGameplayOrigin origin)
        {
            origin = Origin;
            return origin.IsValid;
        }

        public bool TryGetSkillRuntimeHandle(out MobaSkillCastRuntimeHandle handle)
        {
            handle = SkillRuntimeHandle;
            return handle.IsValid;
        }
    }
}
