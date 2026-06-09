namespace AbilityKit.Demo.Moba.Services
{
    public interface IMobaOriginContextProvider
    {
        bool TryGetOrigin(out MobaGameplayOrigin origin);
    }

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

        public int SourceActorId { get; }
        public int TargetActorId { get; }
        public MobaTraceKind ImmediateKind { get; }
        public int ImmediateConfigId { get; }
        public long ImmediateContextId { get; }
        public long ParentContextId { get; }
        public long RootContextId { get; }
        public long OwnerContextId { get; }
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

        /// <summary>
        /// Bridge API for payloads that still carry only actor/config/context primitives.
        /// Prefer FromLineageContext, FromTraceContext, or propagating an existing origin in new runtime code.
        /// </summary>
        public static MobaGameplayOrigin FromLegacy(
            int sourceActorId,
            int targetActorId,
            MobaTraceKind kind,
            int configId,
            long contextId,
            in MobaSkillCastRuntimeHandle skillRuntimeHandle = default)
        {
            return MobaGameplayOriginBuilder.Create()
                .FromLegacy(sourceActorId, targetActorId, kind, configId, contextId)
                .WithSkillRuntime(in skillRuntimeHandle)
                .Build();
        }
    }
}
