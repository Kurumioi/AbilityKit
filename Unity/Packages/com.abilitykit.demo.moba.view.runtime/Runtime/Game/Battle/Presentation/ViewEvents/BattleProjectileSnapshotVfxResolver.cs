using AbilityKit.Protocol.Moba.StateSync;
using UnityEngine;
using EC = AbilityKit.World.ECS;

namespace AbilityKit.Game.Flow.Battle.ViewEvents
{
    internal sealed class BattleProjectileSnapshotVfxResolver
    {
        private readonly BattleProjectileFollowTargetResolver _followTargets;
        private readonly BattleProjectileVfxResolver _vfx;
        private readonly BattleProjectileSnapshotPositionResolver _positions;
        private readonly BattleProjectileVfxSpawnSpecFactory _specs;

        public BattleProjectileSnapshotVfxResolver(
            BattleProjectileFollowTargetResolver followTargets,
            BattleViewResourceProvider resources = null,
            BattleProjectileVfxResolver vfx = null,
            BattleProjectileSnapshotPositionResolver positions = null,
            BattleProjectileVfxSpawnSpecFactory specs = null)
        {
            _followTargets = followTargets;
            _vfx = vfx ?? new BattleProjectileVfxResolver(resources);
            _positions = positions ?? new BattleProjectileSnapshotPositionResolver();
            _specs = specs ?? new BattleProjectileVfxSpawnSpecFactory();
        }

        public bool TryResolve(in MobaProjectileEventSnapshotEntry entry, out BattleProjectileVfxSpawnSpec spec)
        {
            spec = default;

            var vfxId = _vfx.ResolveSnapshotVfxId(entry.TemplateId, entry.Kind);
            if (vfxId <= 0) return false;

            var position = _positions.Resolve(in entry);
            var followId = _followTargets != null
                ? _followTargets.Resolve(entry.ProjectileActorId)
                : default;

            spec = _specs.Create(vfxId, in position, followId);
            return true;
        }
    }

    internal sealed class BattleProjectileSnapshotPositionResolver
    {
        public Vector3 Resolve(in MobaProjectileEventSnapshotEntry entry)
        {
            return new Vector3(entry.X, entry.Y, entry.Z);
        }
    }

    internal sealed class BattleProjectileVfxSpawnSpecFactory
    {
        public BattleProjectileVfxSpawnSpec Create(int vfxId, in Vector3 position, EC.IEntityId followTarget)
        {
            return new BattleProjectileVfxSpawnSpec(vfxId, in position, followTarget);
        }
    }
}
