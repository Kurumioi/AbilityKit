using AbilityKit.Core.Common.Projectile;
using AbilityKit.Trace;

namespace AbilityKit.Demo.Moba.Services.Projectile
{
    public readonly struct ProjectileSourceContext : IMobaOriginContextProvider, IMobaTriggerLineageContextProvider
    {
        public readonly int SourceActorId;
        public readonly int InitialTargetActorId;
        public readonly int ProjectileConfigId;
        public readonly long SourceContextId;
        public readonly long RootContextId;
        public readonly long OwnerContextId;
        public readonly MobaSkillCastRuntimeHandle SkillRuntimeHandle;
        public readonly MobaGameplayOrigin Origin;

        public ProjectileSourceContext(
            int sourceActorId,
            int initialTargetActorId,
            int projectileConfigId,
            long sourceContextId,
            long rootContextId,
            long ownerContextId,
            in MobaSkillCastRuntimeHandle skillRuntimeHandle,
            in MobaGameplayOrigin origin)
        {
            SourceActorId = sourceActorId;
            InitialTargetActorId = initialTargetActorId;
            ProjectileConfigId = projectileConfigId;
            SourceContextId = sourceContextId;
            RootContextId = rootContextId;
            OwnerContextId = ownerContextId;
            SkillRuntimeHandle = skillRuntimeHandle;
            Origin = origin.IsValid
                ? origin
                : MobaGameplayOrigin.FromLegacy(sourceActorId, initialTargetActorId, MobaTraceKind.ProjectileLaunch, projectileConfigId, sourceContextId, in skillRuntimeHandle);
        }

        public bool IsValid => SourceActorId > 0 || SourceContextId != 0 || Origin.IsValid || SkillRuntimeHandle.IsValid;

        public bool TryGetOrigin(out MobaGameplayOrigin origin)
        {
            origin = Origin.IsValid
                ? Origin
                : MobaGameplayOrigin.FromLegacy(SourceActorId, InitialTargetActorId, MobaTraceKind.ProjectileLaunch, ProjectileConfigId, SourceContextId, in SkillRuntimeHandle);
            return origin.IsValid;
        }

        public MobaTriggerLineageContext ToHitLineageContext(ProjectileId projectileId, int targetActorId)
        {
            var sourceActorId = SourceActorId;
            var target = targetActorId > 0 ? targetActorId : InitialTargetActorId;
            var rootContextId = RootContextId != 0 ? RootContextId : SourceContextId;
            var ownerContextId = OwnerContextId != 0 ? OwnerContextId : SourceContextId;
            return new MobaTriggerLineageContext(
                EffectContextKind.Projectile,
                MobaTraceKind.ProjectileHit,
                sourceActorId,
                target,
                SourceContextId,
                rootContextId,
                ownerContextId,
                ProjectileConfigId);
        }

        public MobaTriggerTraceContext ToHitTraceContext(ProjectileId projectileId, int targetActorId)
        {
            return ToHitLineageContext(projectileId, targetActorId).ToTraceContext();
        }

        public bool TryGetLineageContext(out MobaTriggerLineageContext lineageContext)
        {
            lineageContext = new MobaTriggerLineageContext(
                EffectContextKind.Projectile,
                MobaTraceKind.ProjectileLaunch,
                SourceActorId,
                InitialTargetActorId,
                SourceContextId,
                RootContextId != 0 ? RootContextId : SourceContextId,
                OwnerContextId != 0 ? OwnerContextId : SourceContextId,
                ProjectileConfigId);
            return lineageContext.SourceActorId > 0 || lineageContext.SourceContextId != 0;
        }

        public ProjectileSourceContext WithLaunchContext(long sourceContextId)
        {
            return ProjectileSourceContextBuilder.Create()
                .FromSourceContext(in this)
                .WithLaunchContext(sourceContextId)
                .Build();
        }
    }
}
