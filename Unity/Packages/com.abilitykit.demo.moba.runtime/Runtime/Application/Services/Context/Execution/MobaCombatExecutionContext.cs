namespace AbilityKit.Demo.Moba.Services
{
    public interface IMobaCombatExecutionContextProvider
    {
        bool TryGetCombatExecutionContext(out MobaCombatExecutionContext context);
    }

    /// <summary>
    /// Canonical execution-time context for MOBA combat/effect/action/condition execution.
    /// Business execution code should normalize trigger payloads into this model before reading origin, lineage, snapshot, skill runtime, or frame data.
    /// </summary>
    public readonly struct MobaCombatExecutionContext : IMobaContextSourceProvider
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

        /// <summary>Original trigger payload before normalization into lineage/origin/snapshot data.</summary>
        public object Payload { get; }
        /// <summary>Lineage input that determines how the execution attaches to the current trace chain.</summary>
        public MobaEffectLineageInput LineageInput { get; }
        /// <summary>Origin attribution for the immediate gameplay event that led to this execution.</summary>
        public MobaGameplayOrigin Origin { get; }
        /// <summary>Execution snapshot capturing the normalized runtime state at the moment of execution.</summary>
        public MobaTriggerExecutionSnapshot ExecutionSnapshot { get; }
        /// <summary>Skill runtime handle associated with the execution, if any.</summary>
        public MobaSkillCastRuntimeHandle SkillRuntimeHandle { get; }
        /// <summary>Frame index used for trace/debug correlation.</summary>
        public int Frame { get; }

        /// <summary>Effective execution kind resolved from lineage input first, then snapshot.</summary>
        public EffectContextKind ContextKind => LineageInput.ContextKind != EffectContextKind.Unknown ? LineageInput.ContextKind : ExecutionSnapshot.Kind;
        /// <summary>Origin trace kind carried from the lineage layer.</summary>
        public MobaTraceKind OriginKind => LineageInput.OriginKind;
        /// <summary>Effective source actor resolved from lineage, origin, then snapshot.</summary>
        public int SourceActorId => LineageInput.SourceActorId != 0 ? LineageInput.SourceActorId : Origin.SourceActorId != 0 ? Origin.SourceActorId : ExecutionSnapshot.SourceActorId;
        /// <summary>Effective target actor resolved from lineage, origin, then snapshot.</summary>
        public int TargetActorId => LineageInput.TargetActorId != 0 ? LineageInput.TargetActorId : Origin.TargetActorId != 0 ? Origin.TargetActorId : ExecutionSnapshot.TargetActorId;
        /// <summary>Parent context for the current execution. Falls back to origin or snapshot source context when lineage is missing.</summary>
        public long ParentContextId => LineageInput.ParentContextId != 0 ? LineageInput.ParentContextId : Origin.EffectiveParentContextId != 0 ? Origin.EffectiveParentContextId : ExecutionSnapshot.SourceContextId;
        /// <summary>Effective root context for the current execution chain.</summary>
        public long RootContextId => LineageInput.EffectiveRootContextId != 0 ? LineageInput.EffectiveRootContextId : Origin.EffectiveRootContextId != 0 ? Origin.EffectiveRootContextId : ExecutionSnapshot.EffectiveRootContextId;
        /// <summary>Ownership context identity propagated through lineage/origin/snapshot.</summary>
        public long OwnerContextId => LineageInput.OwnerContextId != 0 ? LineageInput.OwnerContextId : Origin.OwnerContextId != 0 ? Origin.OwnerContextId : ExecutionSnapshot.OwnerContextId;
        public int TriggerId => ExecutionSnapshot.TriggerId;
        public int ConfigId => ExecutionSnapshot.ConfigId;
        public bool IsValid => LineageInput.SourceActorId != 0 || LineageInput.TargetActorId != 0 || Origin.IsValid || ExecutionSnapshot.IsValid || SkillRuntimeHandle.IsValid || Payload != null;
        public bool HasExecutionSource => SourceActorId > 0 && ParentContextId != 0;

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
            if (Origin.IsValid)
            {
                origin = Origin;
                return true;
            }

            if (TryGetLineageContext(out var lineageContext))
            {
                var handle = SkillRuntimeHandle;
                origin = MobaGameplayOrigin.FromLineageContext(in lineageContext, in handle);
                return origin.IsValid;
            }

            origin = default;
            return false;
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
            return lineageContext.HasExecutionSource;
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

        public bool TryGetContextSource(out MobaContextSourceView source)
        {
            source = new MobaContextSourceView(
                MobaContextSourceResolveKind.CombatExecutionContext,
                MobaContextSourceBoundary.Execution,
                ContextKind,
                OriginKind,
                SourceActorId,
                TargetActorId,
                ParentContextId,
                ParentContextId,
                RootContextId,
                OwnerContextId,
                ConfigId,
                TriggerId,
                Frame,
                null,
                0,
                false,
                SkillRuntimeHandle);
            return source.IsValid;
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
            if (payload is MobaCombatExecutionContext direct && direct.HasExecutionSource)
            {
                context = direct;
                return true;
            }

            if (payload is IMobaCombatContextSource sourceProvider
                && sourceProvider.TryGetCombatContextSource(out var source)
                && source.HasExecutionSource)
            {
                context = MobaCombatContextBuilder.FromSource(payload, in source);
                return context.HasExecutionSource;
            }

            return payload is IMobaCombatExecutionContextProvider provider
                   && provider.TryGetCombatExecutionContext(out context)
                   && context.HasExecutionSource;
        }
    }
}
