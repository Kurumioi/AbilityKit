using UnityEngine;
using EC = AbilityKit.World.ECS;

namespace AbilityKit.Game.Flow
{
    internal sealed class BattleViewTransformController
    {
        private readonly BattleViewHandleStore _handles;
        private readonly BattleViewInterpolationClock _clock = new BattleViewInterpolationClock();
        private readonly BattleViewPositionSampler _sampler;
        private readonly BattleViewPositionApplier _applier;

        public BattleViewTransformController(BattleViewHandleStore handles, BattleViewAttachedVfxController attachedVfx)
        {
            _handles = handles;
            _sampler = new BattleViewPositionSampler(handles);
            _applier = new BattleViewPositionApplier(handles, attachedVfx);
        }

        public bool InterpolationEnabled { get; set; } = true;

        public float BackTimeTicks { get; set; } = 1f;

        public float MaxLagTicks { get; set; } = 4f;

        public bool TryGetInterpolatedPos(EC.IEntityId id, out Vector3 pos)
        {
            pos = default;
            if (!_handles.TryGet(id, out var handle)) return false;
            if (handle.Destroyed) return false;

            if (!InterpolationEnabled)
            {
                if (handle.HasPendingPos)
                {
                    pos = handle.PendingPos;
                    return true;
                }

                return false;
            }

            if (handle.Pos.TryEvaluate(_clock.RenderTime, out pos)) return true;
            if (handle.HasPendingPos)
            {
                pos = handle.PendingPos;
                return true;
            }

            return false;
        }

        public void SampleEntity(in EC.IEntity entity, in Vector3 pos, BattleContext ctx)
        {
            _sampler.SampleEntity(entity, in pos, ctx);
        }

        public void Tick(BattleContext ctx, float deltaTime)
        {
            if (ctx == null) return;
            if (deltaTime <= 0f) return;
            if (ctx.EntityWorld == null) return;

            if (!InterpolationEnabled)
            {
                _applier.ApplyPendingPositions();
                return;
            }

            var frameAdvanced = _clock.Advance(ctx, deltaTime, BackTimeTicks, MaxLagTicks, out var sampleTime);
            if (frameAdvanced)
            {
                _sampler.SampleAliveEntityPositions(ctx, sampleTime);
            }

            _applier.ApplyInterpolatedPositions(_clock.RenderTime);
        }

        public void Reset()
        {
            _clock.Reset();
        }
    }
}
