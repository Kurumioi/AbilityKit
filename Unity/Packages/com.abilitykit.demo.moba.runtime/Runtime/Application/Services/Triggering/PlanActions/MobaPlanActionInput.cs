using AbilityKit.Core.Math;

namespace AbilityKit.Demo.Moba.Services.Triggering.PlanActions
{
    /// <summary>
    /// Core action execution facts shared by plan actions. Keep domain-specific action data in
    /// action args or specialized inputs such as MobaMovementActionInput instead of expanding this type.
    /// </summary>
    internal readonly struct MobaPlanActionInput
    {
        public MobaPlanActionInput(
            MobaCombatExecutionContext executionContext,
            MobaEffectTraceScopeSnapshot traceScope,
            int casterActorId,
            int targetActorId,
            Vec3 aimPosition,
            Vec3 aimDirection,
            bool hasAimPosition,
            bool hasAimDirection)
        {
            ExecutionContext = executionContext;
            TraceScope = traceScope;
            CasterActorId = casterActorId;
            TargetActorId = targetActorId;
            AimPosition = aimPosition;
            AimDirection = aimDirection;
            HasAimPosition = hasAimPosition;
            HasAimDirection = hasAimDirection;
        }

        public MobaCombatExecutionContext ExecutionContext { get; }
        public MobaEffectTraceScopeSnapshot TraceScope { get; }
        public int CasterActorId { get; }
        public int TargetActorId { get; }
        public Vec3 AimPosition { get; }
        public Vec3 AimDirection { get; }
        public bool HasAimPosition { get; }
        public bool HasAimDirection { get; }

        public bool HasCasterActor => CasterActorId > 0;
        public bool HasTargetActor => TargetActorId > 0;
        public bool HasTraceScope => TraceScope.EffectContextId != 0;
        public bool IsValid => ExecutionContext.IsValid || HasCasterActor || HasTargetActor || HasAimPosition || HasAimDirection || HasTraceScope;
    }
}
