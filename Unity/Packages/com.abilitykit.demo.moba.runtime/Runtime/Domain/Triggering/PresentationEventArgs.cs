using AbilityKit.Core.Math;
using AbilityKit.Demo.Moba.Services;

namespace AbilityKit.Demo.Moba.Triggering
{
    public sealed class PresentationEventArgs : IMobaActorContextProvider, IMobaOriginContextProvider, IMobaTriggerLineageContextProvider, IMobaContextSourceProvider
    {
        public string EventId;

        public int TemplateId;
        public string RequestKey;
        public int DurationMsOverride;

        public int[] Targets;
        public Vec3[] Positions;

        public int SourceActorId;
        public int TargetActorId;
        public long SourceContextId;
        public long RootContextId;
        public long OwnerContextId;
        public MobaTraceKind TraceKind;

        public object Scale;
        public object Radius;
        public object Color;

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
            var traceKind = TraceKind != MobaTraceKind.None ? TraceKind : MobaTraceKind.PresentationPlay;
            origin = MobaGameplayOrigin.FromLegacy(SourceActorId, TargetActorId, traceKind, TemplateId, SourceContextId);
            return origin.IsValid;
        }

        public bool TryGetLineageContext(out MobaTriggerLineageContext lineageContext)
        {
            if (TryGetOrigin(out var origin) && origin.IsValid)
            {
                lineageContext = new MobaTriggerLineageContext(
                    EffectContextKind.Trigger,
                    origin.ImmediateKind,
                    origin.SourceActorId,
                    origin.TargetActorId,
                    origin.EffectiveParentContextId,
                    RootContextId != 0 ? RootContextId : origin.EffectiveRootContextId,
                    OwnerContextId,
                    origin.ImmediateConfigId);
                return true;
            }

            var traceKind = TraceKind != MobaTraceKind.None ? TraceKind : MobaTraceKind.PresentationPlay;
            lineageContext = new MobaTriggerLineageContext(EffectContextKind.Trigger, traceKind, SourceActorId, TargetActorId, SourceContextId, RootContextId, OwnerContextId, TemplateId);
            return SourceActorId > 0 || TargetActorId > 0 || SourceContextId != 0 || TemplateId > 0;
        }

        public bool TryGetContextSource(out MobaContextSourceView source)
        {
            if (TryGetLineageContext(out var lineageContext))
            {
                source = MobaContextSourceView.FromLineage(
                    in lineageContext,
                    MobaContextSourceResolveKind.DirectProvider,
                    MobaContextSourceBoundary.Snapshot,
                    runtimeKind: "Presentation",
                    runtimeConfigId: TemplateId);
                return source.IsValid;
            }

            source = default;
            return false;
        }
    }
}
