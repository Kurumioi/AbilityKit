using System.Collections.Generic;
using AbilityKit.Trace;

namespace AbilityKit.Demo.Moba.Services
{
    public interface IMobaTraceEndpointResolver
    {
        TraceEndpoint ResolveEndpoint(MobaTraceKind kind, int configId);
    }

    public interface IMobaTraceWriter
    {
        long CreateRootContext(MobaTraceKind kind, int configId, long sourceActorId = 0, long targetActorId = 0, object originSource = null, object originTarget = null);
        long CreateChildContext(long parentContextId, MobaTraceKind kind, int configId, long sourceActorId = 0, long targetActorId = 0, object originSource = null, object originTarget = null);
        TraceRootScope CreateEffectRoot(int effectConfigId, int triggerPlanId, int sourceActorId, int targetActorId, EffectContextKind contextKind);
        TraceTreeScope CreateActionChild(long parentRootId, int actionId, int sourceActorId, int targetActorId);
    }

    public interface IMobaTraceLifecycle
    {
        bool EndContext(long contextId, TraceLifecycleReason reason);
        bool EndContext(long contextId, int reason = 0);
    }

    public interface IMobaTraceQuery
    {
        List<TraceSnapshot<MobaTraceMetadata>> GetChain(long rootId);
        bool ValidateChain(long rootId);
    }

    public sealed class MobaTraceEndpointResolver : IMobaTraceEndpointResolver
    {
        public TraceEndpoint ResolveEndpoint(MobaTraceKind kind, int configId)
        {
            switch (kind)
            {
                case MobaTraceKind.SkillPhase:
                    return TraceEndpoint.Config(MobaRuntimeKindNames.Skill, configId);
                case MobaTraceKind.EffectExecution:
                    return TraceEndpoint.Config(MobaRuntimeKindNames.Effect, configId);
                case MobaTraceKind.EffectAction:
                    return TraceEndpoint.Config(MobaRuntimeKindNames.Action, configId);
                case MobaTraceKind.BuffApply:
                case MobaTraceKind.BuffTick:
                case MobaTraceKind.BuffRemove:
                    return TraceEndpoint.Config(MobaRuntimeKindNames.Buff, configId);
                case MobaTraceKind.ProjectileLaunch:
                    return TraceEndpoint.Config(MobaRuntimeKindNames.Projectile, configId);
                case MobaTraceKind.ProjectileHit:
                    return TraceEndpoint.Config(MobaRuntimeKindNames.ProjectileHit, configId);
                case MobaTraceKind.AreaSpawn:
                case MobaTraceKind.AreaExpire:
                    return TraceEndpoint.Config(MobaRuntimeKindNames.Area, configId);
                case MobaTraceKind.AreaEnter:
                    return TraceEndpoint.Config(MobaRuntimeKindNames.AreaEnter, configId);
                case MobaTraceKind.SummonSpawn:
                case MobaTraceKind.SummonDeath:
                    return TraceEndpoint.Config(MobaRuntimeKindNames.Summon, configId);
                case MobaTraceKind.PresentationPlay:
                case MobaTraceKind.PresentationStop:
                    return TraceEndpoint.Config(MobaRuntimeKindNames.Presentation, configId);
                case MobaTraceKind.DamageAttack:
                    return TraceEndpoint.Config(MobaRuntimeKindNames.DamageAttack, configId);
                case MobaTraceKind.DamageCalc:
                    return TraceEndpoint.Config(MobaRuntimeKindNames.DamageCalc, configId);
                case MobaTraceKind.DamageApply:
                    return TraceEndpoint.Config(MobaRuntimeKindNames.DamageResult, configId);
                default:
                    return TraceEndpoint.Config(MobaRuntimeKindNames.Action, configId);
            }
        }
    }

    public sealed class MobaTraceWriter : IMobaTraceWriter
    {
        private readonly MobaTraceRegistry _registry;

        public MobaTraceWriter(MobaTraceRegistry registry)
        {
            _registry = registry;
        }

        public long CreateRootContext(MobaTraceKind kind, int configId, long sourceActorId = 0, long targetActorId = 0, object originSource = null, object originTarget = null)
        {
            return _registry.CreateRoot((int)kind, sourceActorId, targetActorId, originSource, originTarget, configId);
        }

        public long CreateChildContext(long parentContextId, MobaTraceKind kind, int configId, long sourceActorId = 0, long targetActorId = 0, object originSource = null, object originTarget = null)
        {
            return _registry.CreateChild(parentContextId, (int)kind, sourceActorId, targetActorId, originSource, originTarget, configId);
        }

        public TraceRootScope CreateEffectRoot(int effectConfigId, int triggerPlanId, int sourceActorId, int targetActorId, EffectContextKind contextKind)
        {
            var configId = effectConfigId > 0 ? effectConfigId : triggerPlanId;
            return _registry.CreateRootScope(
                (int)MobaTraceKind.EffectExecution,
                sourceActorId,
                targetActorId,
                TraceEndpoint.Config(MobaRuntimeKindNames.Effect, configId),
                TraceEndpoint.Actor(targetActorId),
                configId);
        }

        public TraceTreeScope CreateActionChild(long parentRootId, int actionId, int sourceActorId, int targetActorId)
        {
            return _registry.CreateChildScope(
                parentRootId,
                (int)MobaTraceKind.EffectAction,
                sourceActorId,
                targetActorId,
                TraceEndpoint.Config(MobaRuntimeKindNames.Action, actionId),
                TraceEndpoint.Actor(targetActorId),
                actionId);
        }
    }

    public sealed class MobaTraceLifecycle : IMobaTraceLifecycle
    {
        private readonly MobaTraceRegistry _registry;

        public MobaTraceLifecycle(MobaTraceRegistry registry)
        {
            _registry = registry;
        }

        public bool EndContext(long contextId, TraceLifecycleReason reason)
        {
            return _registry.End(contextId, (int)reason);
        }

        public bool EndContext(long contextId, int reason = 0)
        {
            return _registry.End(contextId, reason);
        }
    }

    public sealed class MobaTraceQuery : IMobaTraceQuery
    {
        private readonly MobaTraceRegistry _registry;

        public MobaTraceQuery(MobaTraceRegistry registry)
        {
            _registry = registry;
        }

        public List<TraceSnapshot<MobaTraceMetadata>> GetChain(long rootId)
        {
            var list = new List<TraceSnapshot<MobaTraceMetadata>>();
            foreach (var snapshot in _registry.GetNodesByRoot(rootId))
            {
                list.Add(snapshot);
            }

            return list;
        }

        public bool ValidateChain(long rootId)
        {
            return _registry.Contains(rootId);
        }
    }
}
