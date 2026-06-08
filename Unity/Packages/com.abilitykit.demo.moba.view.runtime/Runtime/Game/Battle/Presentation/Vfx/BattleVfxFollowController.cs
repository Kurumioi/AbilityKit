using System;
using System.Collections.Generic;
using AbilityKit.Game.Battle.Component;
using AbilityKit.Game.Flow;
using UnityEngine;
using EC = AbilityKit.World.ECS;

namespace AbilityKit.Game.Battle.Vfx
{
    internal sealed class BattleVfxFollowController
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private readonly HashSet<ulong> _interpFallbackWarned = new HashSet<ulong>();
#endif

        public void Tick(in EC.IEntity vfxRoot, BattleViewBinder binder, Action<EC.IECWorld, EC.IEntityId> destroyAction)
        {
            if (!vfxRoot.IsValid) return;
            var world = vfxRoot.World;
            if (world == null) return;
            if (destroyAction == null) return;

            var ids = new List<EC.IEntityId>(32);
            BattleVfxEntityCollector.Collect(vfxRoot, ids);
            if (ids.Count == 0) return;

            for (int i = 0; i < ids.Count; i++)
            {
                var id = ids[i];
                if (!world.IsAlive(id)) continue;

                var entity = world.Wrap(id);
                if (IsExpired(entity))
                {
                    destroyAction(world, id);
                    continue;
                }

                TrySyncFollow(world, entity, binder);
            }
        }

        public void SyncFollow(EC.IECWorld world, EC.IEntityId vfxEntityId, in Vector3 targetPos)
        {
            if (world == null) return;
            if (!world.IsAlive(vfxEntityId)) return;

            var entity = world.Wrap(vfxEntityId);
            if (!entity.TryGetRef(out BattleViewGameObjectComponent goComp) || goComp == null || goComp.GameObject == null) return;

            var pos = targetPos;
            if (entity.TryGetRef(out BattleViewFollowComponent follow) && follow != null)
            {
                pos += follow.Offset;
            }

            goComp.GameObject.transform.position = pos;
        }

        private bool IsExpired(EC.IEntity entity)
        {
            if (!entity.TryGetRef(out BattleVfxLifetimeComponent life) || life == null) return false;
            if (life.ExpireAtTime <= 0f) return false;
            return Time.time >= life.ExpireAtTime;
        }

        private void TrySyncFollow(EC.IECWorld world, EC.IEntity entity, BattleViewBinder binder)
        {
            if (!entity.TryGetRef(out BattleViewFollowComponent follow) || follow == null) return;
            if (follow.Target.Index == 0) return;
            if (!world.IsAlive(follow.Target)) return;

            if (binder != null && binder.TryGetInterpolatedPos(follow.Target, out var viewPos))
            {
                SyncFollow(world, entity.Id, viewPos);
                return;
            }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (binder != null)
            {
                var key = ((ulong)(uint)entity.Id.Index << 32) | (uint)follow.Target.Index;
                if (_interpFallbackWarned.Add(key))
                {
                    Debug.LogWarning($"[BattleVfxManager] VFX follow fallback to logic position: vfx={entity.Id.Index} target={follow.Target.Index} frame={Time.frameCount}");
                }
            }
#endif

            if (world.Wrap(follow.Target).TryGetRef(out BattleTransformComponent transform) && transform != null)
            {
                SyncFollow(world, entity.Id, transform.Position);
            }
        }
    }
}
