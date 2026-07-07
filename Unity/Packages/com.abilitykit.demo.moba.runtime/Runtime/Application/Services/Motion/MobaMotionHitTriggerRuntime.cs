using System;
using System.Collections.Generic;
using AbilityKit.Combat.MotionSystem.Collision;
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

    public sealed class MobaMotionCollisionWorldAdapter : IMotionCollisionWorld
    {
        private readonly ICollisionWorld _world;
        private readonly MobaActorRegistry _actors;
        private readonly List<ColliderId> _candidates = new List<ColliderId>(16);
        private readonly List<ColliderId> _sampleOverlaps = new List<ColliderId>(16);

        public MobaMotionCollisionWorldAdapter(ICollisionWorld world, MobaActorRegistry actors)
        {
            _world = world ?? throw new ArgumentNullException(nameof(world));
            _actors = actors;
        }

        public bool Sweep(
            int moverId,
            in Vec3 start,
            in Vec3 desiredDelta,
            float radius,
            int obstacleMask,
            int ignoreMask,
            out MotionHit hit,
            out Vec3 appliedDelta)
        {
            appliedDelta = desiredDelta;
            var distance = desiredDelta.Magnitude;
            if (distance <= MathUtil.Epsilon)
            {
                hit = MotionHit.None;
                return false;
            }

            var layerMask = ResolveMask(obstacleMask);
            var sweepRadius = MathUtil.Max(radius, 0.01f);
            var direction = desiredDelta / distance;
            var center = start + desiredDelta * 0.5f;
            var queryRadius = distance * 0.5f + sweepRadius;

            _candidates.Clear();
            _world.OverlapSphere(new Sphere(center, queryRadius), layerMask, _candidates);

            var ignoredCollider = ResolveIgnoredCollider(moverId);
            var bestTime = float.PositiveInfinity;
            var bestCollider = default(ColliderId);
            var bestNormal = Vec3.Zero;

            for (var i = 0; i < _candidates.Count; i++)
            {
                var collider = _candidates[i];
                if (ShouldIgnore(collider, ignoredCollider, ignoreMask)) continue;

                if (!TryResolveHitTime(start, direction, distance, sweepRadius, collider, layerMask, out var time01, out var normal)) continue;
                if (time01 < bestTime)
                {
                    bestTime = time01;
                    bestCollider = collider;
                    bestNormal = normal;
                }
            }

            _candidates.Clear();

            if (bestCollider.Value <= 0)
            {
                hit = MotionHit.None;
                return false;
            }

            var clampedTime = MathUtil.Clamp01(bestTime);
            hit = new MotionHit(true, bestCollider.Value, bestNormal, clampedTime);
            return true;
        }

        public bool Overlap(int moverId, in Vec3 position, float radius, int obstacleMask, int ignoreMask)
        {
            var layerMask = ResolveMask(obstacleMask);
            var ignoredCollider = ResolveIgnoredCollider(moverId);

            _sampleOverlaps.Clear();
            _world.OverlapSphere(new Sphere(position, MathUtil.Max(radius, 0.01f)), layerMask, _sampleOverlaps);

            for (var i = 0; i < _sampleOverlaps.Count; i++)
            {
                var collider = _sampleOverlaps[i];
                if (ShouldIgnore(collider, ignoredCollider, ignoreMask)) continue;

                _sampleOverlaps.Clear();
                return true;
            }

            _sampleOverlaps.Clear();
            return false;
        }

        public bool TryProjectToFree(int moverId, in Vec3 position, float radius, int obstacleMask, int ignoreMask, out Vec3 projectedPosition)
        {
            projectedPosition = position;
            return !Overlap(moverId, in position, radius, obstacleMask, ignoreMask);
        }

        private bool TryResolveHitTime(
            in Vec3 start,
            in Vec3 direction,
            float distance,
            float moverRadius,
            ColliderId collider,
            int layerMask,
            out float time01,
            out Vec3 normal)
        {
            const int samples = 10;
            time01 = 0f;
            normal = Vec3.Zero;

            for (var i = 0; i <= samples; i++)
            {
                var t = i / (float)samples;
                var point = start + direction * (distance * t);
                _sampleOverlaps.Clear();
                _world.OverlapSphere(new Sphere(point, moverRadius), layerMask, _sampleOverlaps);

                for (var j = 0; j < _sampleOverlaps.Count; j++)
                {
                    if (!_sampleOverlaps[j].Equals(collider)) continue;

                    time01 = t;
                    normal = -direction;
                    _sampleOverlaps.Clear();
                    return true;
                }
            }

            var ray = new Ray3(start, direction);
            if (_world.Raycast(ray, distance + moverRadius, layerMask, out var rayHit) && rayHit.Collider.Equals(collider))
            {
                time01 = distance > MathUtil.Epsilon ? MathUtil.Clamp01(rayHit.Distance / distance) : 0f;
                normal = rayHit.Normal.SqrMagnitude > MathUtil.Epsilon ? rayHit.Normal : -direction;
                return true;
            }

            return false;
        }

        private ColliderId ResolveIgnoredCollider(int moverId)
        {
            if (_actors == null) return default;
            if (!_actors.TryGet(moverId, out var entity) || entity == null) return default;
            return entity.hasCollisionId ? entity.collisionId.Value : default;
        }

        private static int ResolveMask(int mask)
        {
            return mask != 0 ? mask : -1;
        }

        private static bool ShouldIgnore(ColliderId collider, ColliderId moverCollider, int ignoreMask)
        {
            if (collider.Value <= 0) return true;
            if (moverCollider.Value > 0 && collider.Equals(moverCollider)) return true;
            return ignoreMask != 0 && (collider.Value & ignoreMask) != 0;
        }
    }
}
