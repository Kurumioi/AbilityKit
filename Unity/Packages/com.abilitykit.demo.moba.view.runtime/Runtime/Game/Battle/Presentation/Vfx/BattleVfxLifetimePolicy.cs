using AbilityKit.Game.Battle.Component;
using AbilityKit.Game.Battle.Shared.Time;
using EC = AbilityKit.World.ECS;

namespace AbilityKit.Game.Battle.Vfx
{
    internal sealed class BattleVfxLifetimePolicy
    {
        private readonly IBattleViewTimeSource _time;

        public BattleVfxLifetimePolicy(IBattleViewTimeSource time = null)
        {
            _time = time ?? UnityBattleViewTimeSource.Shared;
        }

        public void AttachIfNeeded(EC.IEntity entity, int durationMs)
        {
            if (!entity.IsValid) return;
            if (durationMs <= 0) return;

            entity.WithRef(new BattleVfxLifetimeComponent { ExpireAtTime = _time.TimeSeconds + (durationMs / 1000f) });
        }

        public bool IsExpired(EC.IEntity entity)
        {
            if (!entity.TryGetRef(out BattleVfxLifetimeComponent life) || life == null) return false;
            if (life.ExpireAtTime <= 0f) return false;
            return _time.TimeSeconds >= life.ExpireAtTime;
        }
    }
}
