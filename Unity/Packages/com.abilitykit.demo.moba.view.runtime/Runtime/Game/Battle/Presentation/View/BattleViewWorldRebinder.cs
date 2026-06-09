using System;
using EC = AbilityKit.World.ECS;

namespace AbilityKit.Game.Flow
{
    internal sealed class BattleViewWorldRebinder
    {
        private readonly BattleViewEntitySyncController _sync;

        public BattleViewWorldRebinder(BattleViewEntitySyncController sync)
        {
            _sync = sync ?? throw new ArgumentNullException(nameof(sync));
        }

        public void RebindAll(EC.IECWorld world)
        {
            if (world == null) return;
            world.ForEachAlive(entity => _sync.Sync(entity));
        }

        public void RebindAll(EC.IECWorld world, BattleContext ctx)
        {
            if (world == null) return;
            world.ForEachAlive(entity => _sync.Sync(entity, ctx));
        }
    }
}
