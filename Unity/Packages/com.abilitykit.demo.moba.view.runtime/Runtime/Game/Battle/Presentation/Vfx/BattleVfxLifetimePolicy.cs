using AbilityKit.Game.Battle.Component;
using UnityEngine;
using EC = AbilityKit.World.ECS;

namespace AbilityKit.Game.Battle.Vfx
{
    internal sealed class BattleVfxLifetimePolicy
    {
        public void AttachIfNeeded(EC.IEntity entity, int durationMs)
        {
            if (!entity.IsValid) return;
            if (durationMs <= 0) return;

            entity.WithRef(new BattleVfxLifetimeComponent { ExpireAtTime = Time.time + (durationMs / 1000f) });
        }

        public bool IsExpired(EC.IEntity entity)
        {
            if (!entity.TryGetRef(out BattleVfxLifetimeComponent life) || life == null) return false;
            if (life.ExpireAtTime <= 0f) return false;
            return Time.time >= life.ExpireAtTime;
        }
    }
}
