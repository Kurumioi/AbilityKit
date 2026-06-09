using UnityEngine;
using EC = AbilityKit.World.ECS;

namespace AbilityKit.Game.Flow.Battle.ViewEvents
{
    internal readonly struct BattleProjectileVfxSpawnSpec
    {
        public BattleProjectileVfxSpawnSpec(int vfxId, in Vector3 position, EC.IEntityId followTarget)
        {
            VfxId = vfxId;
            Position = position;
            FollowTarget = followTarget;
        }

        public int VfxId { get; }

        public Vector3 Position { get; }

        public EC.IEntityId FollowTarget { get; }

        public bool IsValid => VfxId > 0;
    }
}
