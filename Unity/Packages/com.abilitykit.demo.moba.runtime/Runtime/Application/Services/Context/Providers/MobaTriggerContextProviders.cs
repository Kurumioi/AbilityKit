namespace AbilityKit.Demo.Moba.Services
{
    public interface IMobaTriggerLineageContextProvider
    {
        bool TryGetLineageContext(out MobaTriggerLineageContext lineageContext);
    }

    public interface IMobaTriggerTraceContextProvider
    {
        bool TryGetTraceContext(out MobaTriggerTraceContext traceContext);
    }

    public interface IMobaContextSourceProvider
    {
        bool TryGetContextSource(out MobaContextSourceView source);
    }

    public enum MobaContextSourceBoundary
    {
        Unknown = 0,
        Snapshot = 1,
        Execution = 2,
        LiveRuntime = 3
    }

    public enum MobaContextSourceResolveKind
    {
        Unknown = 0,
        DirectProvider = 1,
        CombatExecutionContext = 2,
        Origin = 3,
        Lineage = 4,
        Trace = 5,
        ExecutionSnapshot = 6,
        RuntimeDebug = 7
    }

    public readonly struct MobaContextSourceView
    {
        public MobaContextSourceView(
            MobaContextSourceResolveKind resolveKind,
            MobaContextSourceBoundary boundary,
            EffectContextKind contextKind,
            MobaTraceKind traceKind,
            int sourceActorId,
            int targetActorId,
            long sourceContextId,
            long parentContextId,
            long rootContextId,
            long ownerContextId,
            int configId,
            int triggerId,
            int frame,
            string runtimeKind,
            int runtimeConfigId,
            bool hasLiveRuntime,
            MobaSkillCastRuntimeHandle skillRuntimeHandle)
        {
            ResolveKind = resolveKind;
            Boundary = boundary;
            ContextKind = contextKind;
            TraceKind = traceKind;
            SourceActorId = sourceActorId;
            TargetActorId = targetActorId;
            SourceContextId = sourceContextId;
            ParentContextId = parentContextId;
            RootContextId = rootContextId;
            OwnerContextId = ownerContextId;
            ConfigId = configId;
            TriggerId = triggerId;
            Frame = frame;
            RuntimeKind = runtimeKind;
            RuntimeConfigId = runtimeConfigId;
            HasLiveRuntime = hasLiveRuntime;
            SkillRuntimeHandle = skillRuntimeHandle;
        }

        public MobaContextSourceResolveKind ResolveKind { get; }
        public MobaContextSourceBoundary Boundary { get; }
        public EffectContextKind ContextKind { get; }
        public MobaTraceKind TraceKind { get; }
        public int SourceActorId { get; }
        public int TargetActorId { get; }
        public long SourceContextId { get; }
        public long ParentContextId { get; }
        public long RootContextId { get; }
        public long OwnerContextId { get; }
        public int ConfigId { get; }
        public int TriggerId { get; }
        public int Frame { get; }
        public string RuntimeKind { get; }
        public int RuntimeConfigId { get; }
        public bool HasLiveRuntime { get; }
        public MobaSkillCastRuntimeHandle SkillRuntimeHandle { get; }
        public bool IsValid => ContextKind != EffectContextKind.Unknown || SourceActorId != 0 || TargetActorId != 0 || SourceContextId != 0 || ParentContextId != 0 || RootContextId != 0 || OwnerContextId != 0 || ConfigId != 0 || TriggerId != 0 || Frame != 0 || RuntimeConfigId != 0 || SkillRuntimeHandle.IsValid;

        public static MobaContextSourceView FromOrigin(in MobaGameplayOrigin origin, MobaContextSourceResolveKind resolveKind = MobaContextSourceResolveKind.Origin, MobaContextSourceBoundary boundary = MobaContextSourceBoundary.Snapshot, bool hasLiveRuntime = false, string runtimeKind = null, int runtimeConfigId = 0)
        {
            return new MobaContextSourceView(
                resolveKind,
                boundary,
                EffectContextKind.Unknown,
                origin.ImmediateKind,
                origin.SourceActorId,
                origin.TargetActorId,
                origin.ImmediateContextId,
                origin.EffectiveParentContextId,
                origin.EffectiveRootContextId,
                origin.OwnerContextId,
                origin.ImmediateConfigId,
                0,
                0,
                runtimeKind,
                runtimeConfigId,
                hasLiveRuntime,
                origin.SkillRuntimeHandle);
        }

        public static MobaContextSourceView FromLineage(in MobaTriggerLineageContext lineageContext, MobaContextSourceResolveKind resolveKind = MobaContextSourceResolveKind.Lineage, MobaContextSourceBoundary boundary = MobaContextSourceBoundary.Snapshot, MobaSkillCastRuntimeHandle skillRuntimeHandle = default, bool hasLiveRuntime = false, string runtimeKind = null, int runtimeConfigId = 0)
        {
            return new MobaContextSourceView(
                resolveKind,
                boundary,
                lineageContext.ContextKind,
                lineageContext.OriginKind,
                lineageContext.SourceActorId,
                lineageContext.TargetActorId,
                lineageContext.SourceContextId,
                lineageContext.SourceContextId,
                lineageContext.RootContextId != 0 ? lineageContext.RootContextId : lineageContext.SourceContextId,
                lineageContext.OwnerKey,
                lineageContext.SourceConfigId,
                0,
                0,
                runtimeKind,
                runtimeConfigId,
                hasLiveRuntime,
                skillRuntimeHandle);
        }

        public static MobaContextSourceView FromTrace(in MobaTriggerTraceContext traceContext, MobaSkillCastRuntimeHandle skillRuntimeHandle = default)
        {
            var lineageContext = traceContext.ToLineageContext();
            return FromLineage(in lineageContext, MobaContextSourceResolveKind.Trace, MobaContextSourceBoundary.Snapshot, skillRuntimeHandle);
        }

        public static MobaContextSourceView FromExecutionSnapshot(in MobaTriggerExecutionSnapshot snapshot, MobaContextSourceResolveKind resolveKind = MobaContextSourceResolveKind.ExecutionSnapshot)
        {
            return new MobaContextSourceView(
                resolveKind,
                MobaContextSourceBoundary.Execution,
                snapshot.Kind,
                MobaTraceKind.EffectExecution,
                snapshot.SourceActorId,
                snapshot.TargetActorId,
                snapshot.SourceContextId,
                snapshot.SourceContextId,
                snapshot.EffectiveRootContextId,
                snapshot.OwnerContextId,
                snapshot.ConfigId,
                snapshot.TriggerId,
                snapshot.Frame,
                null,
                0,
                false,
                snapshot.SkillRuntimeHandle);
        }

        public static MobaContextSourceView FromRuntimeDebug(in MobaContinuousRuntimeDebugInfo debug)
        {
            return new MobaContextSourceView(
                MobaContextSourceResolveKind.RuntimeDebug,
                MobaContextSourceBoundary.LiveRuntime,
                EffectContextKind.Unknown,
                MobaTraceKind.None,
                debug.SourceActorId,
                debug.TargetActorId,
                debug.SourceContextId,
                debug.ParentContextId,
                debug.RootContextId,
                debug.OwnerContextId,
                debug.ConfigId,
                0,
                0,
                debug.Kind,
                debug.ConfigId,
                true,
                debug.SkillRuntimeHandle);
        }
    }
}
