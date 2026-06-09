using System.Collections.Generic;
using AbilityKit.Game.Battle.Component;
using AbilityKit.Game.Flow;
using UnityEngine;
using EC = AbilityKit.World.ECS;

namespace AbilityKit.Game.Battle.Vfx
{
    internal sealed class BattleVfxFollowTargetPositionResolver
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private readonly HashSet<ulong> _interpFallbackWarned = new HashSet<ulong>();
#endif

        public bool TryResolve(EC.IECWorld world, EC.IEntity entity, BattleViewBinder binder, out Vector3 position)
        {
            position = default;
            if (world == null) return false;
            if (!entity.TryGetRef(out BattleViewFollowComponent follow) || follow == null) return false;
            if (follow.Target.Index == 0) return false;
            if (!world.IsAlive(follow.Target)) return false;

            if (binder != null && binder.TryGetInterpolatedPos(follow.Target, out var viewPos))
            {
                position = viewPos;
                return true;
            }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            WarnInterpolationFallback(entity.Id, follow.Target, binder);
#endif

            if (world.Wrap(follow.Target).TryGetRef(out BattleTransformComponent transform) && transform != null)
            {
                position = transform.Position;
                return true;
            }

            return false;
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private void WarnInterpolationFallback(EC.IEntityId vfxId, EC.IEntityId targetId, BattleViewBinder binder)
        {
            if (binder == null) return;

            var key = ((ulong)(uint)vfxId.Index << 32) | (uint)targetId.Index;
            if (_interpFallbackWarned.Add(key))
            {
                Debug.LogWarning($"[BattleVfxManager] VFX follow fallback to logic position: vfx={vfxId.Index} target={targetId.Index} frame={Time.frameCount}");
            }
        }
#endif
    }
}
