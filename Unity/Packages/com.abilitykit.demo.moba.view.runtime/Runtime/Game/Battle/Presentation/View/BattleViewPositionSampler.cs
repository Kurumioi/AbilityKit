using AbilityKit.Game.Battle.Component;
using AbilityKit.Game.Battle.Entity;
using UnityEngine;
using EC = AbilityKit.World.ECS;

namespace AbilityKit.Game.Flow
{
    internal sealed class BattleViewPositionSampler
    {
        private readonly BattleViewHandleStore _handles;
        private readonly BattleViewSampleTimeResolver _sampleTimes;

        public BattleViewPositionSampler(
            BattleViewHandleStore handles,
            BattleViewSampleTimeResolver sampleTimes = null)
        {
            _handles = handles;
            _sampleTimes = sampleTimes ?? new BattleViewSampleTimeResolver();
        }

        public void SampleEntity(in EC.IEntity entity, in Vector3 pos, BattleContext ctx)
        {
            SampleEntity(entity, in pos, _sampleTimes.Resolve(ctx));
        }

        public void SampleAliveEntityPositions(BattleContext ctx, double sampleTime)
        {
            if (ctx?.EntityWorld == null) return;

            ctx.EntityWorld.ForEachAlive(entity =>
            {
                if (!entity.TryGetRef(out BattleNetIdComponent netIdComp) || netIdComp == null) return;
                if (!entity.TryGetRef(out BattleTransformComponent transform) || transform == null) return;

                SampleEntity(entity, in transform.Position, sampleTime);
            });
        }

        private void SampleEntity(in EC.IEntity entity, in Vector3 pos, double sampleTime)
        {
            var handle = _handles.GetOrCreate(entity.Id);
            if (handle.Destroyed) return;

            handle.PendingPos = pos;
            handle.HasPendingPos = true;
            handle.Pos.Add(sampleTime, in handle.PendingPos);

            if (entity.TryGetRef(out BattleNetIdComponent netIdComp) && netIdComp != null)
            {
                var actorId = netIdComp.NetId.Value;
                if (actorId > 0)
                {
                    _handles.SetActorId(handle, actorId, entity.Id);
                }
            }
        }

    }

    internal sealed class BattleViewSampleTimeResolver
    {
        public double Resolve(BattleContext ctx)
        {
            if (ctx == null) return 0d;

            var tickRate = ctx.Plan.TickRate;
            if (tickRate <= 0) tickRate = 30;
            return (double)ctx.LastFrame / tickRate;
        }
    }
}
