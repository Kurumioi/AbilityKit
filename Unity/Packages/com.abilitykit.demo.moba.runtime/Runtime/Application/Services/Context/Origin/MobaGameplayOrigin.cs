namespace AbilityKit.Demo.Moba.Services
{
    public interface IMobaOriginContextProvider
    {
        bool TryGetOrigin(out MobaGameplayOrigin origin);
    }

    /// <summary>
    /// Attribution-only gameplay origin model. It records the immediate source event and the effective lineage boundaries derived from it.
    /// </summary>
    public readonly struct MobaGameplayOrigin
    {
        public MobaGameplayOrigin(
            int sourceActorId,
            int targetActorId,
            MobaTraceKind immediateKind,
            int immediateConfigId,
            long immediateContextId,
            long parentContextId,
            long rootContextId,
            long ownerContextId,
            MobaSkillCastRuntimeHandle skillRuntimeHandle = default)
        {
            SourceActorId = sourceActorId;
            TargetActorId = targetActorId;
            ImmediateKind = immediateKind;
            ImmediateConfigId = immediateConfigId;
            ImmediateContextId = immediateContextId;
            ParentContextId = parentContextId;
            RootContextId = rootContextId;
            OwnerContextId = ownerContextId;
            SkillRuntimeHandle = skillRuntimeHandle;
        }

        /// <summary>Actor that produced the origin event.</summary>
        public int SourceActorId { get; }
        /// <summary>Actor targeted by the origin event.</summary>
        public int TargetActorId { get; }
        /// <summary>Immediate trace kind of the event that produced this origin.</summary>
        public MobaTraceKind ImmediateKind { get; }
        /// <summary>Configuration identifier for the immediate origin event.</summary>
        public int ImmediateConfigId { get; }
        /// <summary>Immediate trace context node for the event itself.</summary>
        public long ImmediateContextId { get; }
        /// <summary>Parent context used when the immediate context attaches into a larger chain.</summary>
        public long ParentContextId { get; }
        /// <summary>Effective root context for the chain carried by this origin.</summary>
        public long RootContextId { get; }
        /// <summary>Ownership context identity propagated across the origin chain.</summary>
        public long OwnerContextId { get; }
        /// <summary>Skill runtime handle related to the origin, when available.</summary>
        public MobaSkillCastRuntimeHandle SkillRuntimeHandle { get; }

        public bool IsValid => SourceActorId > 0 || TargetActorId > 0 || ParentContextId != 0 || ImmediateContextId != 0 || RootContextId != 0 || OwnerContextId != 0 || SkillRuntimeHandle.IsValid;
        public bool HasExecutionSource => SourceActorId > 0 && EffectiveParentContextId != 0;
        public long EffectiveParentContextId => ParentContextId != 0 ? ParentContextId : ImmediateContextId;
        public long EffectiveRootContextId => RootContextId != 0 ? RootContextId : EffectiveParentContextId;

        public MobaTriggerLineageContext ToLineageContext(EffectContextKind contextKind)
        {
            return new MobaTriggerLineageContext(
                contextKind,
                ImmediateKind != MobaTraceKind.None ? ImmediateKind : MobaTraceKind.EffectExecution,
                SourceActorId,
                TargetActorId,
                EffectiveParentContextId,
                EffectiveRootContextId,
                OwnerContextId,
                ImmediateConfigId);
        }

        public MobaTriggerTraceContext ToTriggerTraceContext(EffectContextKind contextKind)
        {
            return ToLineageContext(contextKind).ToTraceContext();
        }

        public MobaGameplayOrigin WithImmediate(MobaTraceKind kind, int configId, long contextId, long ownerContextId = 0)
        {
            return MobaGameplayOriginBuilder.Create()
                .FromOrigin(in this)
                .WithImmediate(kind, configId, contextId)
                .WithOwnerContext(ownerContextId != 0 ? ownerContextId : OwnerContextId)
                .Build();
        }

        public MobaGameplayOrigin WithActors(int sourceActorId, int targetActorId)
        {
            return MobaGameplayOriginBuilder.Create()
                .FromOrigin(in this)
                .WithActors(sourceActorId, targetActorId)
                .Build();
        }

        public MobaGameplayOrigin WithSkillRuntime(in MobaSkillCastRuntimeHandle handle)
        {
            return MobaGameplayOriginBuilder.Create()
                .FromOrigin(in this)
                .WithSkillRuntime(in handle)
                .Build();
        }

        public static MobaGameplayOrigin FromLineageContext(in MobaTriggerLineageContext lineageContext, in MobaSkillCastRuntimeHandle skillRuntimeHandle = default)
        {
            return MobaGameplayOriginBuilder.Create()
                .FromLineageContext(in lineageContext)
                .WithSkillRuntime(in skillRuntimeHandle)
                .Build();
        }

        public static MobaGameplayOrigin FromTraceContext(in MobaTriggerTraceContext traceContext, in MobaSkillCastRuntimeHandle skillRuntimeHandle = default)
        {
            var lineageContext = traceContext.ToLineageContext();
            return FromLineageContext(in lineageContext, in skillRuntimeHandle);
        }

    }
}
