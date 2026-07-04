using AbilityKit.Core.Mathematics;

namespace AbilityKit.Demo.Moba.Services.Motion
{
    public readonly struct MobaMotionHitTriggerRuntime
    {
        public MobaMotionHitTriggerRuntime(
            int triggerId,
            int sourceActorId,
            int sourceConfigId,
            MobaEffectTraceScopeSnapshot traceScope,
            MobaSkillCastRuntimeHandle skillRuntimeHandle = default)
        {
            TriggerId = triggerId;
            SourceActorId = sourceActorId;
            SourceConfigId = sourceConfigId;
            TraceScope = traceScope;
            SkillRuntimeHandle = skillRuntimeHandle;
        }

        public int TriggerId { get; }
        public int SourceActorId { get; }
        public int SourceConfigId { get; }
        public MobaEffectTraceScopeSnapshot TraceScope { get; }
        public MobaSkillCastRuntimeHandle SkillRuntimeHandle { get; }

        public bool IsValid => TriggerId > 0 && SourceActorId > 0 && TraceScope.EffectContextId != 0;

        public MobaMotionHitTriggerRuntime WithSourceActor(int sourceActorId)
        {
            return new MobaMotionHitTriggerRuntime(
                TriggerId,
                sourceActorId,
                SourceConfigId,
                TraceScope,
                SkillRuntimeHandle);
        }
    }

    public sealed class MobaMotionHitArgs : MobaTriggerInvocationContextBase, IMobaActorContextProvider, IMobaTriggerExecutionSnapshotProvider
    {
        public override EffectContextKind Kind => EffectContextKind.Trigger;
        public int SourceConfigId { get; set; }
        public int Frame { get; set; }
        public int MotionTargetId { get; set; }
        public ColliderId HitCollider { get; set; }
        public Vec3 Point { get; set; }
        public Vec3 Normal { get; set; }
        public MobaMotionHitTriggerRuntime Runtime { get; set; }

        public bool TryGetSourceActorId(out int actorId)
        {
            actorId = SourceActorId > 0 ? SourceActorId : Runtime.SourceActorId;
            return actorId > 0;
        }

        public bool TryGetTargetActorId(out int actorId)
        {
            actorId = TargetActorId;
            return actorId > 0;
        }

        public override bool TryGetLineageContext(out MobaTriggerLineageContext lineageContext)
        {
            if (Runtime.IsValid)
            {
                lineageContext = new MobaTriggerLineageContext(
                    EffectContextKind.Trigger,
                    MobaTraceKind.EffectExecution,
                    SourceActorId > 0 ? SourceActorId : Runtime.SourceActorId,
                    TargetActorId,
                    Runtime.TraceScope.EffectContextId,
                    Runtime.TraceScope.EffectContextId,
                    Runtime.TraceScope.EffectContextId,
                    SourceConfigId != 0 ? SourceConfigId : Runtime.SourceConfigId);
                return true;
            }

            lineageContext = default;
            return false;
        }

        public override bool TryGetTraceContext(out MobaTriggerTraceContext traceContext)
        {
            if (TryGetLineageContext(out var lineageContext))
            {
                traceContext = lineageContext.ToTraceContext();
                return true;
            }

            traceContext = default;
            return false;
        }

        public override bool TryGetOrigin(out MobaGameplayOrigin origin)
        {
            if (TryGetLineageContext(out var lineageContext))
            {
                origin = MobaGameplayOrigin.FromLineageContext(in lineageContext, Runtime.SkillRuntimeHandle);
                return origin.IsValid;
            }

            origin = default;
            return false;
        }

        public bool TryGetExecutionSnapshot(out MobaTriggerExecutionSnapshot snapshot)
        {
            if (!TryGetLineageContext(out var lineageContext))
            {
                snapshot = default;
                return false;
            }

            snapshot = new MobaTriggerExecutionSnapshot(
                lineageContext.ContextKind,
                lineageContext.SourceActorId,
                lineageContext.TargetActorId,
                lineageContext.SourceContextId,
                lineageContext.RootContextId,
                lineageContext.OwnerContextId,
                TriggerId,
                lineageContext.SourceConfigId,
                Frame,
                Runtime.SkillRuntimeHandle);
            return snapshot.IsValid;
        }
    }
}
