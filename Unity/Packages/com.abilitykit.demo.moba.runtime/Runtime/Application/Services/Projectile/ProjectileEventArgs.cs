using AbilityKit.Combat.Projectile;
using AbilityKit.Core.Mathematics;

namespace AbilityKit.Demo.Moba.Services.Projectile
{
    public sealed class ProjectileEventArgs : IMobaActorContextProvider, IMobaTriggerInvocationContext, IMobaTriggerLineageContextProvider, IMobaTriggerTraceContextProvider, IMobaOriginContextProvider, IMobaTriggerSkillRuntimeContext, IMobaTriggerExecutionSnapshotProvider, IMobaContextSourceProvider, IMobaPersistentContextSourceProvider
    {
        public int TriggerId { get; set; }
        public EffectContextKind Kind => EffectContextKind.Projectile;
        public string EventId { get; set; }
        public int SourceActorId { get; set; }
        public int TargetActorId { get; set; }
        public long SourceContextId { get; set; }
        public int SourceConfigId { get; set; }
        public int Frame { get; set; }
        public object Raw { get; set; }
        public ProjectileSourceContext SourceContext { get; set; }

        public int CasterActorId;
        public int ProjectileTemplateId;
        public ProjectileId ProjectileId;
        public Vec3 Position;
        public Vec3 Direction;
        public ProjectileExitReason ExitReason;

        public bool TryGetSourceActorId(out int actorId)
        {
            actorId = SourceActorId > 0 ? SourceActorId : CasterActorId;
            return actorId > 0;
        }

        public bool TryGetTargetActorId(out int actorId)
        {
            actorId = TargetActorId;
            return actorId > 0;
        }

        public bool TryGetLineageContext(out MobaTriggerLineageContext lineageContext)
        {
            if (SourceContext.IsValid && SourceContext.TryGetLineageContext(out lineageContext))
            {
                return true;
            }

            lineageContext = default;
            return false;
        }

        public bool TryGetTraceContext(out MobaTriggerTraceContext traceContext)
        {
            if (TryGetLineageContext(out var lineageContext))
            {
                traceContext = lineageContext.ToTraceContext();
                return true;
            }

            traceContext = default;
            return false;
        }

        public bool TryGetOrigin(out MobaGameplayOrigin origin)
        {
            if (SourceContext.TryGetOrigin(out origin))
            {
                origin = origin.WithActors(SourceActorId > 0 ? SourceActorId : CasterActorId, TargetActorId);
                return origin.IsValid;
            }

            if (TryGetLineageContext(out var lineageContext))
            {
                origin = MobaGameplayOrigin.FromLineageContext(in lineageContext);
                return origin.IsValid;
            }

            origin = default;
            return false;
        }

        public bool TryGetSkillRuntimeHandle(out MobaSkillCastRuntimeHandle handle)
        {
            handle = SourceContext.SkillRuntimeHandle;
            return handle.IsValid;
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
                lineageContext.OwnerKey,
                TriggerId,
                ProjectileTemplateId != 0 ? ProjectileTemplateId : SourceConfigId,
                Frame,
                SourceContext.SkillRuntimeHandle);
            return snapshot.IsValid;
        }

        public bool TryGetContextSource(out MobaContextSourceView source)
        {
            if (TryGetLineageContext(out var lineageContext))
            {
                source = MobaContextSourceView.FromLineage(
                    in lineageContext,
                    MobaContextSourceResolveKind.DirectProvider,
                    MobaContextSourceBoundary.Snapshot,
                    SourceContext.SkillRuntimeHandle,
                    false,
                    EventId ?? "Projectile",
                    ProjectileTemplateId);
                return source.IsValid;
            }

            source = default;
            return false;
        }

        public bool TryGetPersistentContextSource(out MobaPersistentContextSourceSnapshot snapshot)
        {
            if (TryGetContextSource(out var source))
            {
                snapshot = MobaPersistentContextSourceSnapshotFactory.FromContextSource(in source);
                return snapshot.HasExecutionSource;
            }

            snapshot = default;
            return false;
        }
    }
}
