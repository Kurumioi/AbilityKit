using AbilityKit.Core.Mathematics;
using AbilityKit.Core.Logging;
using AbilityKit.Demo.Moba.Services;
using AbilityKit.Demo.Moba.Services.Projectile;

namespace AbilityKit.Demo.Moba.Services.Triggering.PlanActions
{
    internal interface IMobaPlanActionExecutionInput
    {
        MobaCombatExecutionContext ExecutionContext { get; }
    }

    internal interface IMobaPlanActionTraceInput
    {
        MobaEffectTraceScopeSnapshot TraceScope { get; }
        bool HasTraceScope { get; }
    }

    internal interface IMobaPlanActionActorInput
    {
        int CasterActorId { get; }
        int TargetActorId { get; }
        bool HasCasterActor { get; }
        bool HasTargetActor { get; }
    }

    internal interface IMobaPlanActionAimInput
    {
        Vec3 AimPosition { get; }
        Vec3 AimDirection { get; }
        bool HasAimPosition { get; }
        bool HasAimDirection { get; }
    }

    /// <summary>
    /// 计划动作共享的核心执行事实。
    /// 领域专属的动作数据应保留在动作参数对象或诸如 MobaMovementActionInput 之类的专用输入中，不要继续扩展该类型。
    /// </summary>
    internal readonly struct MobaPlanActionInput : IMobaPlanActionExecutionInput, IMobaPlanActionTraceInput, IMobaPlanActionActorInput, IMobaPlanActionAimInput
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
        public bool HasExecutionSource => ExecutionContext.HasExecutionSource;
        public bool IsValid => HasExecutionSource || HasCasterActor || HasTargetActor || HasAimPosition || HasAimDirection || HasTraceScope;
    }

    internal readonly struct MobaEffectActionInput : IMobaPlanActionExecutionInput, IMobaPlanActionTraceInput, IMobaPlanActionActorInput
    {
        private readonly MobaPlanActionInput _core;

        public MobaEffectActionInput(in MobaPlanActionInput core)
        {
            _core = core;
        }

        public MobaCombatExecutionContext ExecutionContext => _core.ExecutionContext;
        public MobaEffectTraceScopeSnapshot TraceScope => _core.TraceScope;
        public int CasterActorId => _core.CasterActorId;
        public int TargetActorId => _core.TargetActorId;
        public bool HasCasterActor => _core.HasCasterActor;
        public bool HasTargetActor => _core.HasTargetActor;
        public bool HasTraceScope => _core.HasTraceScope;
        public bool HasExecutionSource => _core.HasExecutionSource;
        public bool IsValid => _core.IsValid;

        public MobaGameplayOrigin BuildOrigin(int sourceActorId, int targetActorId, MobaTraceKind fallbackKind, int fallbackConfigId)
        {
            var executionContext = ExecutionContext;
            var traceScope = TraceScope;
            return MobaActionOriginBuilder.Build(
                in executionContext,
                in traceScope,
                sourceActorId,
                targetActorId,
                fallbackKind,
                fallbackConfigId);
        }

        public MobaGameplayOrigin BuildFromOrigin(in MobaGameplayOrigin sourceOrigin, int sourceActorId, int targetActorId)
        {
            var executionContext = ExecutionContext;
            var traceScope = TraceScope;
            return MobaActionOriginBuilder.BuildFromOrigin(
                in sourceOrigin,
                in executionContext,
                in traceScope,
                sourceActorId,
                targetActorId);
        }
    }

    internal readonly struct MobaProjectileActionInput : IMobaPlanActionExecutionInput, IMobaPlanActionTraceInput, IMobaPlanActionActorInput, IMobaPlanActionAimInput
    {
        private readonly MobaEffectActionInput _effect;
        private readonly MobaPlanActionInput _core;

        public MobaProjectileActionInput(in MobaEffectActionInput effect, in MobaPlanActionInput core)
        {
            _effect = effect;
            _core = core;
        }

        public MobaCombatExecutionContext ExecutionContext => _effect.ExecutionContext;
        public MobaEffectTraceScopeSnapshot TraceScope => _effect.TraceScope;
        public int CasterActorId => _effect.CasterActorId;
        public int TargetActorId => _effect.TargetActorId;
        public Vec3 AimPosition => _core.AimPosition;
        public Vec3 AimDirection => _core.AimDirection;
        public bool HasCasterActor => _effect.HasCasterActor;
        public bool HasTargetActor => _effect.HasTargetActor;
        public bool HasTraceScope => _effect.HasTraceScope;
        public bool HasAimPosition => _core.HasAimPosition;
        public bool HasAimDirection => _core.HasAimDirection;
        public bool HasExecutionSource => _effect.HasExecutionSource;
        public bool IsValid => _effect.IsValid;

        public ProjectileSourceContext CreateSourceContext(int sourceActorId, int targetActorId, int projectileConfigId)
        {
            var origin = _effect.BuildOrigin(sourceActorId, targetActorId, MobaTraceKind.ProjectileLaunch, projectileConfigId);
            Log.Warning(
                $"[AK_TRACE_DIAG] projectile source origin projectileConfigId={projectileConfigId} sourceActorId={sourceActorId} targetActorId={targetActorId} immediateContextId={origin.ImmediateContextId} parentContextId={origin.EffectiveParentContextId} rootContextId={origin.EffectiveRootContextId} ownerContextId={origin.OwnerContextId}");
            return ProjectileSourceContextBuilder.Create()
                .WithActors(sourceActorId, targetActorId)
                .WithProjectileConfig(projectileConfigId)
                .WithSourceContext(origin.ImmediateContextId)
                .WithRootContext(origin.EffectiveRootContextId)
                .WithOwnerContext(origin.OwnerContextId)
                .WithOrigin(in origin)
                .Build();
        }
    }

    internal readonly struct MobaSummonActionInput : IMobaPlanActionExecutionInput, IMobaPlanActionTraceInput, IMobaPlanActionActorInput, IMobaPlanActionAimInput
    {
        private readonly MobaEffectActionInput _effect;
        private readonly MobaPlanActionInput _core;
        private readonly MobaActorLookupService _actors;

        public MobaSummonActionInput(in MobaEffectActionInput effect, in MobaPlanActionInput core, MobaActorLookupService actors)
        {
            _effect = effect;
            _core = core;
            _actors = actors;
        }

        public MobaCombatExecutionContext ExecutionContext => _effect.ExecutionContext;
        public MobaEffectTraceScopeSnapshot TraceScope => _effect.TraceScope;
        public int CasterActorId => _effect.CasterActorId;
        public int TargetActorId => _effect.TargetActorId;
        public Vec3 AimPosition => _core.AimPosition;
        public Vec3 AimDirection => _core.AimDirection;
        public bool HasCasterActor => _effect.HasCasterActor;
        public bool HasTargetActor => _effect.HasTargetActor;
        public bool HasTraceScope => _effect.HasTraceScope;
        public bool HasAimPosition => _core.HasAimPosition;
        public bool HasAimDirection => _core.HasAimDirection;
        public bool HasExecutionSource => _effect.HasExecutionSource;
        public bool IsValid => _effect.IsValid;

        public bool TryResolveSpawnPosition(SpawnSummonPositionMode positionMode, out Vec3 spawnPosition)
        {
            spawnPosition = Vec3.Zero;
            switch (positionMode)
            {
                case SpawnSummonPositionMode.Caster:
                    return TryGetActorPosition(CasterActorId, out spawnPosition);
                case SpawnSummonPositionMode.Target:
                    return TryGetActorPosition(TargetActorId, out spawnPosition);
                case SpawnSummonPositionMode.AimPos:
                    if (!HasAimPosition) return false;
                    spawnPosition = new Vec3(AimPosition.X, 0, AimPosition.Y);
                    return true;
                case SpawnSummonPositionMode.Fixed:
                    return true;
                default:
                    return false;
            }
        }

        public SummonSourceContext CreateSourceContext(int sourceActorId, int summonConfigId)
        {
            var origin = _effect.BuildOrigin(sourceActorId, 0, MobaTraceKind.SummonSpawn, summonConfigId);
            return SummonSourceContextBuilder.Create()
                .WithActors(sourceActorId, 0)
                .WithSummonConfig(summonConfigId)
                .WithRootContext(origin.EffectiveRootContextId)
                .WithOwnerContext(origin.OwnerContextId)
                .WithOrigin(in origin)
                .Build();
        }

        public MobaGameplayOrigin BuildOrigin(int sourceActorId, int targetActorId, MobaTraceKind fallbackKind, int fallbackConfigId)
        {
            return _effect.BuildOrigin(sourceActorId, targetActorId, fallbackKind, fallbackConfigId);
        }

        private bool TryGetActorPosition(int actorId, out Vec3 position)
        {
            position = Vec3.Zero;
            if (actorId <= 0 || _actors == null) return false;
            if (!_actors.TryGetActorEntity(actorId, out var actor) || actor == null || !actor.hasTransform) return false;
            position = actor.transform.Value.Position;
            return true;
        }
    }
}
