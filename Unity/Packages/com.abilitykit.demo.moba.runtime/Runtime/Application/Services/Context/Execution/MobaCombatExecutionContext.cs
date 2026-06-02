namespace AbilityKit.Demo.Moba.Services
{
    public interface IMobaCombatExecutionContextProvider
    {
        bool TryGetCombatExecutionContext(out MobaCombatExecutionContext context);
    }

    public readonly struct MobaCombatExecutionContext
    {
        public MobaCombatExecutionContext(
            object payload,
            MobaEffectLineageInput lineageInput,
            MobaGameplayOrigin origin,
            MobaTriggerExecutionSnapshot executionSnapshot,
            MobaSkillCastRuntimeHandle skillRuntimeHandle,
            int frame)
        {
            Payload = payload;
            LineageInput = lineageInput;
            Origin = origin;
            ExecutionSnapshot = executionSnapshot;
            SkillRuntimeHandle = skillRuntimeHandle;
            Frame = frame != 0 ? frame : executionSnapshot.Frame;
        }

        public object Payload { get; }
        public MobaEffectLineageInput LineageInput { get; }
        public MobaGameplayOrigin Origin { get; }
        public MobaTriggerExecutionSnapshot ExecutionSnapshot { get; }
        public MobaSkillCastRuntimeHandle SkillRuntimeHandle { get; }
        public int Frame { get; }

        public EffectContextKind ContextKind => LineageInput.ContextKind != EffectContextKind.Unknown ? LineageInput.ContextKind : ExecutionSnapshot.Kind;
        public MobaTraceKind OriginKind => LineageInput.OriginKind;
        public int SourceActorId => LineageInput.SourceActorId != 0 ? LineageInput.SourceActorId : Origin.SourceActorId != 0 ? Origin.SourceActorId : ExecutionSnapshot.SourceActorId;
        public int TargetActorId => LineageInput.TargetActorId != 0 ? LineageInput.TargetActorId : Origin.TargetActorId != 0 ? Origin.TargetActorId : ExecutionSnapshot.TargetActorId;
        public long ParentContextId => LineageInput.ParentContextId != 0 ? LineageInput.ParentContextId : Origin.EffectiveParentContextId != 0 ? Origin.EffectiveParentContextId : ExecutionSnapshot.SourceContextId;
        public long RootContextId => LineageInput.EffectiveRootContextId != 0 ? LineageInput.EffectiveRootContextId : Origin.EffectiveRootContextId != 0 ? Origin.EffectiveRootContextId : ExecutionSnapshot.EffectiveRootContextId;
        public long OwnerContextId => LineageInput.OwnerKey != 0 ? LineageInput.OwnerKey : Origin.OwnerContextId != 0 ? Origin.OwnerContextId : ExecutionSnapshot.OwnerContextId;
        public int TriggerId => ExecutionSnapshot.TriggerId;
        public int ConfigId => ExecutionSnapshot.ConfigId;
        public bool IsValid => LineageInput.SourceActorId != 0 || LineageInput.TargetActorId != 0 || Origin.IsValid || ExecutionSnapshot.IsValid || SkillRuntimeHandle.IsValid || Payload != null;

        public bool TryGetCombatExecutionContext(out MobaCombatExecutionContext context)
        {
            context = this;
            return IsValid;
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
            var handle = SkillRuntimeHandle;
            origin = Origin.IsValid
                ? Origin
                : MobaGameplayOrigin.FromLegacy(SourceActorId, TargetActorId, OriginKind, ConfigId, ParentContextId, in handle);
            return origin.IsValid;
        }

        public bool TryGetLineageContext(out MobaTriggerLineageContext lineageContext)
        {
            lineageContext = new MobaTriggerLineageContext(
                ContextKind,
                OriginKind,
                SourceActorId,
                TargetActorId,
                ParentContextId,
                RootContextId,
                OwnerContextId,
                ConfigId);
            return SourceActorId > 0 || TargetActorId > 0 || ParentContextId != 0;
        }

        public bool TryGetSkillRuntimeHandle(out MobaSkillCastRuntimeHandle handle)
        {
            handle = SkillRuntimeHandle;
            return handle.IsValid;
        }

        public bool TryGetExecutionSnapshot(out MobaTriggerExecutionSnapshot snapshot)
        {
            snapshot = ExecutionSnapshot;
            return snapshot.IsValid;
        }

        public static MobaCombatExecutionContext Create(
            object payload,
            in MobaEffectLineageInput lineageInput,
            in MobaTriggerExecutionSnapshot executionSnapshot,
            int frame)
        {
            return MobaCombatExecutionContextFactory.Create(payload, in lineageInput, in executionSnapshot, frame);
        }

        public MobaCombatExecutionContext WithSnapshot(in MobaTriggerExecutionSnapshot executionSnapshot, int frame)
        {
            return MobaCombatExecutionContextFactory.WithSnapshot(in this, in executionSnapshot, frame);
        }
    }

    public static class MobaCombatExecutionContextResolveExtensions
    {
        public static bool TryResolveCombatExecutionContext(this object payload, out MobaCombatExecutionContext context)
        {
            context = default;
            if (payload is MobaCombatExecutionContext direct && direct.IsValid)
            {
                context = direct;
                return true;
            }

            return payload is IMobaCombatExecutionContextProvider provider
                   && provider.TryGetCombatExecutionContext(out context)
                   && context.IsValid;
        }
    }
}
