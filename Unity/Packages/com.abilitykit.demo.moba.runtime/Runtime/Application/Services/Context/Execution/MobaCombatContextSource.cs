namespace AbilityKit.Demo.Moba.Services
{
    /// <summary>
    /// Canonical source data for building combat execution context without re-entering payload provider resolution.
    /// </summary>
    public interface IMobaCombatContextSource
    {
        bool TryGetCombatContextSource(out MobaCombatContextSource source);
    }

    public readonly struct MobaCombatContextSource
    {
        public MobaCombatContextSource(
            EffectContextKind contextKind,
            MobaTraceKind traceKind,
            int sourceActorId,
            int targetActorId,
            long sourceContextId,
            long rootContextId,
            long ownerContextId,
            int configId,
            int triggerId = 0,
            int frame = 0,
            MobaSkillCastRuntimeHandle skillRuntimeHandle = default,
            string runtimeKind = null,
            int runtimeConfigId = 0,
            bool hasLiveRuntime = false)
        {
            ContextKind = contextKind;
            TraceKind = traceKind;
            SourceActorId = sourceActorId;
            TargetActorId = targetActorId;
            SourceContextId = sourceContextId;
            RootContextId = rootContextId;
            OwnerContextId = ownerContextId;
            ConfigId = configId;
            TriggerId = triggerId;
            Frame = frame;
            SkillRuntimeHandle = skillRuntimeHandle;
            RuntimeKind = runtimeKind;
            RuntimeConfigId = runtimeConfigId;
            HasLiveRuntime = hasLiveRuntime;
        }

        public EffectContextKind ContextKind { get; }
        public MobaTraceKind TraceKind { get; }
        public int SourceActorId { get; }
        public int TargetActorId { get; }
        public long SourceContextId { get; }
        public long RootContextId { get; }
        public long OwnerContextId { get; }
        public int ConfigId { get; }
        public int TriggerId { get; }
        public int Frame { get; }
        public MobaSkillCastRuntimeHandle SkillRuntimeHandle { get; }
        public string RuntimeKind { get; }
        public int RuntimeConfigId { get; }
        public bool HasLiveRuntime { get; }

        public bool IsValid => ContextKind != EffectContextKind.Unknown
                               || TraceKind != MobaTraceKind.None
                               || SourceActorId != 0
                               || TargetActorId != 0
                               || SourceContextId != 0
                               || RootContextId != 0
                               || OwnerContextId != 0
                               || ConfigId != 0
                               || TriggerId != 0
                               || Frame != 0
                               || RuntimeConfigId != 0
                               || SkillRuntimeHandle.IsValid;
        public bool HasExecutionSource => SourceActorId > 0 && SourceContextId != 0;

        public MobaTriggerLineageContext ToLineageContext()
        {
            return new MobaTriggerLineageContext(
                ContextKind,
                TraceKind,
                SourceActorId,
                TargetActorId,
                SourceContextId,
                RootContextId != 0 ? RootContextId : SourceContextId,
                OwnerContextId,
                ConfigId);
        }

        public MobaTriggerExecutionSnapshot ToExecutionSnapshot()
        {
            return new MobaTriggerExecutionSnapshot(
                ContextKind,
                SourceActorId,
                TargetActorId,
                SourceContextId,
                RootContextId != 0 ? RootContextId : SourceContextId,
                OwnerContextId,
                TriggerId,
                ConfigId,
                Frame,
                SkillRuntimeHandle);
        }

        public MobaGameplayOrigin ToOrigin()
        {
            var lineageContext = ToLineageContext();
            var handle = SkillRuntimeHandle;
            return MobaGameplayOrigin.FromLineageContext(in lineageContext, in handle);
        }

        public MobaContextSourceView ToContextSourceView(MobaContextSourceResolveKind resolveKind, MobaContextSourceBoundary boundary)
        {
            return new MobaContextSourceView(
                resolveKind,
                boundary,
                ContextKind,
                TraceKind,
                SourceActorId,
                TargetActorId,
                SourceContextId,
                SourceContextId,
                RootContextId != 0 ? RootContextId : SourceContextId,
                OwnerContextId,
                ConfigId,
                TriggerId,
                Frame,
                RuntimeKind,
                RuntimeConfigId,
                HasLiveRuntime,
                SkillRuntimeHandle);
        }
    }

    public static class MobaCombatContextBuilder
    {
        public static MobaCombatExecutionContext FromSource(object payload, in MobaCombatContextSource source)
        {
            var lineageInput = source.ToLineageContext().ToLineageInput();
            var origin = source.ToOrigin();
            var snapshot = source.ToExecutionSnapshot();
            return new MobaCombatExecutionContext(payload, lineageInput, origin, snapshot, source.SkillRuntimeHandle, source.Frame);
        }

        public static bool TryFromSource(object payload, out MobaCombatExecutionContext context)
        {
            context = default;
            if (payload is not IMobaCombatContextSource provider
                || !provider.TryGetCombatContextSource(out var source)
                || !source.HasExecutionSource)
            {
                return false;
            }

            context = FromSource(payload, in source);
            return context.HasExecutionSource;
        }

        public static MobaCombatContextSource SkillCast(
            int skillId,
            int casterActorId,
            int targetActorId,
            long sourceContextId,
            int frame,
            in MobaSkillCastRuntimeHandle skillRuntimeHandle)
        {
            return new MobaCombatContextSource(
                EffectContextKind.Skill,
                MobaTraceKind.SkillCast,
                casterActorId,
                targetActorId,
                sourceContextId,
                sourceContextId,
                sourceContextId,
                skillId,
                triggerId: 0,
                frame: frame,
                skillRuntimeHandle: skillRuntimeHandle,
                runtimeKind: "SkillPipeline",
                runtimeConfigId: skillId,
                hasLiveRuntime: true);
        }
    }
}
