using AbilityKit.Game.Battle.Vfx;
using EC = AbilityKit.World.ECS;

namespace AbilityKit.Game.Flow.Battle.ViewEvents
{
    internal sealed class BattleProjectileVfxSpawner
    {
        private readonly BattleContext _ctx;
        private readonly BattleVfxManager _vfx;
        private readonly EC.IEntity _vfxNode;

        public BattleProjectileVfxSpawner(BattleContext ctx, BattleVfxManager vfx, in EC.IEntity vfxNode)
        {
            _ctx = ctx;
            _vfx = vfx;
            _vfxNode = vfxNode;
        }

        public bool CanSpawn
        {
            get
            {
                if (_ctx?.EntityWorld == null) return false;
                if (_vfx == null) return false;
                if (!_vfxNode.IsValid) return false;
                return true;
            }
        }

        public bool TrySpawn(in BattleProjectileVfxSpawnSpec spec)
        {
            if (!CanSpawn) return false;
            if (!spec.IsValid) return false;

            var position = spec.Position;
            return _vfx.TryCreateVfxEntity(
                _ctx.EntityWorld,
                _vfxNode,
                spec.VfxId,
                spec.FollowTarget,
                in position,
                out _);
        }
    }
}
