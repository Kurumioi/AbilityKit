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

            ValidateSource(buffId, sourceActorId, targetActorId, sourceContextId, stage, runtime);

            for (int i = 0; i < triggerIds.Count; i++)
            {
                var triggerId = triggerIds[i];
                if (triggerId <= 0) continue;

                _effects.ExecuteTriggerId(triggerId, CreatePayload(triggerId, buffId, sourceActorId, targetActorId, sourceContextId, stage, runtime, removeReason, durationSeconds));
            }
        }

        private static void ValidateSource(int buffId, int sourceActorId, int targetActorId, long sourceContextId, string stage, BuffRuntime runtime)
        {
            if (runtime == null)
            {
                throw new System.InvalidOperationException($"Buff stage effect requires live runtime context. buffId={buffId} stage={stage} sourceActorId={sourceActorId} targetActorId={targetActorId} sourceContextId={sourceContextId}");
            }

            if (sourceActorId <= 0 || targetActorId <= 0 || sourceContextId == 0L)
            {
                throw new System.InvalidOperationException($"Buff stage effect source context is incomplete. buffId={buffId} stage={stage} sourceActorId={sourceActorId} targetActorId={targetActorId} sourceContextId={sourceContextId}");
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

    internal interface IBuffTriggerContext : IMobaTriggerInvocationContext, IMobaActorContextProvider, IBuffLiveViewProvider
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

    internal sealed class BuffTriggerContext : IBuffTriggerContext, IMobaTriggerLineageContextProvider, IMobaTriggerTraceContextProvider, IMobaTriggerRuntimeContext<BuffRuntime>, IMobaTriggerSkillRuntimeContext, IMobaOriginContextProvider, IMobaTriggerStageSnapshotProvider, IMobaContextSourceProvider
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
        public MobaTriggerLineageContext LineageContext => ResolveLineageContext();
        public MobaTriggerTraceContext TraceContext => LineageContext.ToTraceContext();
        public MobaGameplayOrigin Origin
        {
            get
            {
                if (Runtime != null && Runtime.Origin.IsValid) return Runtime.Origin;

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

        public bool TryGetLiveBuffView(out BuffRuntimeView view)
        {
            view = new BuffRuntimeView(Runtime);
            return view.IsValid;
        }

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

        public bool TryGetStageSnapshot(out MobaTriggerStageSnapshot snapshot)
        {
            snapshot = new MobaTriggerStageSnapshot(
                StackCountSnapshot,
                0f,
                RemainingSecondsSnapshot,
                DurationSecondsSnapshot);
            return snapshot.IsValid;
        }

        public bool TryGetContextSource(out MobaContextSourceView source)
        {
            if (Runtime != null && Runtime.ContextSource.IsValid)
            {
                source = new MobaContextSourceView(
                    MobaContextSourceResolveKind.DirectProvider,
                    MobaContextSourceBoundary.LiveRuntime,
                    Runtime.ContextSource.ContextKind != EffectContextKind.Unknown ? Runtime.ContextSource.ContextKind : EffectContextKind.Buff,
                    Runtime.ContextSource.TraceKind,
                    Runtime.ContextSource.SourceActorId != 0 ? Runtime.ContextSource.SourceActorId : SourceActorId,
                    Runtime.ContextSource.TargetActorId != 0 ? Runtime.ContextSource.TargetActorId : TargetActorId,
                    Runtime.ContextSource.SourceContextId != 0 ? Runtime.ContextSource.SourceContextId : SourceContextId,
                    Runtime.ContextSource.ParentContextId,
                    Runtime.ContextSource.RootContextId,
                    Runtime.ContextSource.OwnerContextId,
                    Runtime.ContextSource.ConfigId != 0 ? Runtime.ContextSource.ConfigId : BuffId,
                    TriggerId,
                    Runtime.ContextSource.Frame,
                    "Buff",
                    BuffId,
                    true,
                    Runtime.ContextSource.SkillRuntimeHandle.IsValid ? Runtime.ContextSource.SkillRuntimeHandle : SkillRuntimeHandle);
                return source.IsValid;
            }

            var lineageContext = LineageContext;
            source = MobaContextSourceView.FromLineage(
                in lineageContext,
                MobaContextSourceResolveKind.DirectProvider,
                Runtime != null ? MobaContextSourceBoundary.LiveRuntime : MobaContextSourceBoundary.Snapshot,
                SkillRuntimeHandle,
                Runtime != null,
                "Buff",
                BuffId);
            return source.IsValid;
        }

        private MobaTriggerLineageContext ResolveLineageContext()
        {
            if (Runtime != null && Runtime.Origin.IsValid)
            {
                var origin = Runtime.Origin;
                return new MobaTriggerLineageContext(
                    Kind,
                    ResolveTraceKind(Stage),
                    origin.SourceActorId != 0 ? origin.SourceActorId : SourceActorId,
                    origin.TargetActorId != 0 ? origin.TargetActorId : TargetActorId,
                    origin.EffectiveParentContextId != 0 ? origin.EffectiveParentContextId : SourceContextId,
                    origin.EffectiveRootContextId != 0 ? origin.EffectiveRootContextId : SourceContextId,
                    origin.OwnerContextId,
                    BuffId);
            }

            return new MobaTriggerLineageContext(Kind, ResolveTraceKind(Stage), SourceActorId, TargetActorId, SourceContextId, SourceContextId, SourceContextId, BuffId);
        }

        private static MobaTraceKind ResolveTraceKind(string stage)
        {
            if (MobaBuffTriggering.Stages.IsRemove(stage)) return MobaTraceKind.BuffRemove;
            if (MobaBuffTriggering.Stages.IsInterval(stage)) return MobaTraceKind.BuffTick;
            return MobaTraceKind.BuffApply;
        }
    }
}
