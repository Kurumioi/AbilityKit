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

    public interface IMobaPersistentContextSourceProvider
    {
        bool TryGetPersistentContextSource(out MobaPersistentContextSourceSnapshot snapshot);
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

    /// <summary>
    /// Resolved context source view for query, snapshot, retention, diagnostics, and debug usage.
    /// This is intentionally broad, but it should not replace MobaCombatExecutionContext as the canonical execution-time model.
    /// </summary>
    /// <remarks>
    /// SourceContextId is the source/attachment node supplied by the provider. ParentContextId is the parent node to use when creating a downstream execution node. RootContextId is the known root of the trace chain. OwnerContextId identifies the ownership context and should be preferred over legacy OwnerKey terminology in new typed context models.
    /// </remarks>
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

        /// <summary>How the source view was resolved.</summary>
        public MobaContextSourceResolveKind ResolveKind { get; }
        /// <summary>Lifecycle boundary represented by this source view.</summary>
        public MobaContextSourceBoundary Boundary { get; }
        /// <summary>Normalized context kind, if the provider can determine one.</summary>
        public EffectContextKind ContextKind { get; }
        /// <summary>Trace kind associated with the source view.</summary>
        public MobaTraceKind TraceKind { get; }
        /// <summary>Actor that produced the source context.</summary>
        public int SourceActorId { get; }
        /// <summary>Actor targeted by the source context.</summary>
        public int TargetActorId { get; }
        /// <summary>Provider source/attachment node. For lineage and snapshots it usually becomes the downstream parent context.</summary>
        public long SourceContextId { get; }
        /// <summary>Parent node to use when creating a downstream execution node.</summary>
        public long ParentContextId { get; }
        /// <summary>Known root node of the trace chain.</summary>
        public long RootContextId { get; }
        /// <summary>Ownership context identity propagated by typed context models.</summary>
        public long OwnerContextId { get; }
        /// <summary>Configuration id represented by the source.</summary>
        public int ConfigId { get; }
        /// <summary>Trigger id represented by the source, when available.</summary>
        public int TriggerId { get; }
        /// <summary>Frame index represented by the source, when available.</summary>
        public int Frame { get; }
        /// <summary>Runtime kind string for live-runtime diagnostics.</summary>
        public string RuntimeKind { get; }
        /// <summary>Runtime config id for live-runtime diagnostics.</summary>
        public int RuntimeConfigId { get; }
        /// <summary>Whether this view is backed by a live runtime instance.</summary>
        public bool HasLiveRuntime { get; }
        /// <summary>Skill runtime handle associated with the source, when available.</summary>
        public MobaSkillCastRuntimeHandle SkillRuntimeHandle { get; }
        public bool IsValid => ContextKind != EffectContextKind.Unknown || SourceActorId != 0 || TargetActorId != 0 || SourceContextId != 0 || ParentContextId != 0 || RootContextId != 0 || OwnerContextId != 0 || ConfigId != 0 || TriggerId != 0 || Frame != 0 || RuntimeConfigId != 0 || SkillRuntimeHandle.IsValid;
        public bool HasExecutionSource => SourceActorId > 0 && SourceContextId != 0;

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
                lineageContext.OwnerContextId,
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
