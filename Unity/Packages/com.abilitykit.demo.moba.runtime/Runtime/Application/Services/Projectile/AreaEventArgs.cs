using AbilityKit.Core.Math;
using AbilityKit.Core.Common.Projectile;

namespace AbilityKit.Demo.Moba.Services.Projectile
{
    public sealed class AreaEventArgs : IMobaActorContextProvider, IMobaOriginContextProvider, IMobaTriggerLineageContextProvider, IMobaContextSourceProvider
    {
        public string EventId;
        public int AreaId;
        public int TemplateId;
        public int OwnerActorId;
        public int TargetActorId;
        public int Frame;
        public long SourceContextId;
        public long RootContextId;
        public long OwnerContextId;
        public MobaTraceKind TraceKind;

        public Vec3 Center;
        public float Radius;
        public ColliderId Collider;

        public int CollisionLayerMask;
        public int MaxTargets;

        public object Raw;

        public bool TryGetSourceActorId(out int actorId)
        {
            actorId = OwnerActorId;
            return actorId > 0;
        }

        public bool TryGetTargetActorId(out int actorId)
        {
            actorId = TargetActorId;
            return actorId > 0;
        }

        public bool TryGetOrigin(out MobaGameplayOrigin origin)
        {
            var traceKind = TraceKind != MobaTraceKind.None ? TraceKind : MobaTraceKind.AreaSpawn;
            origin = MobaGameplayOrigin.FromLegacy(OwnerActorId, TargetActorId, traceKind, TemplateId, SourceContextId);
            return origin.IsValid;
        }

        public bool TryGetLineageContext(out MobaTriggerLineageContext lineageContext)
        {
            if (TryGetOrigin(out var origin) && origin.IsValid)
            {
                lineageContext = new MobaTriggerLineageContext(
                    EffectContextKind.Area,
                    origin.ImmediateKind,
                    origin.SourceActorId,
                    origin.TargetActorId,
                    origin.EffectiveParentContextId,
                    RootContextId != 0 ? RootContextId : origin.EffectiveRootContextId,
                    OwnerContextId,
                    origin.ImmediateConfigId);
                return true;
            }

            var traceKind = TraceKind != MobaTraceKind.None ? TraceKind : MobaTraceKind.AreaSpawn;
            lineageContext = new MobaTriggerLineageContext(EffectContextKind.Area, traceKind, OwnerActorId, TargetActorId, SourceContextId, RootContextId, OwnerContextId, TemplateId);
            return OwnerActorId > 0 || TargetActorId > 0 || TemplateId > 0 || SourceContextId != 0;
        }

        public bool TryGetContextSource(out MobaContextSourceView source)
        {
            if (TryGetLineageContext(out var lineageContext))
            {
                source = MobaContextSourceView.FromLineage(
                    in lineageContext,
                    MobaContextSourceResolveKind.DirectProvider,
                    MobaContextSourceBoundary.Snapshot,
                    runtimeKind: "Area",
                    runtimeConfigId: TemplateId);
                return source.IsValid;
            }

            source = default;
            return false;
        }
    }
}
