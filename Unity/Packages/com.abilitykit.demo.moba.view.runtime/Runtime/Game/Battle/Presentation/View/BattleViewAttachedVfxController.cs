using AbilityKit.Game.Battle.Entity;
using AbilityKit.Game.Battle.Vfx;
using UnityEngine;
using EC = AbilityKit.World.ECS;

namespace AbilityKit.Game.Flow
{
    internal sealed class BattleViewAttachedVfxController
    {
        private readonly BattleVfxManager _vfx;
        private readonly EC.IEntity _vfxNode;

        public BattleViewAttachedVfxController(BattleVfxManager vfx, in EC.IEntity vfxNode)
        {
            _vfx = vfx;
            _vfxNode = vfxNode;
        }

        private bool IsAvailable => _vfx != null && _vfxNode.IsValid;

        public void SyncProjectileVfx(EC.IEntity entity, BattleViewHandle handle, BattleEntityMetaComponent meta)
        {
            if (!IsAvailable) return;
            if (handle == null) return;

            var desiredVfxId = BattleViewFactory.ResolveProjectileVfxId(meta);
            if (desiredVfxId <= 0)
            {
                Destroy(entity.World, handle);
                return;
            }

            if (handle.VfxEntityId.Index != 0 && handle.VfxId == desiredVfxId)
            {
                return;
            }

            Destroy(entity.World, handle);
            if (_vfx.TryCreateVfxEntity(entity.World, _vfxNode, desiredVfxId, entity.Id, in handle.PendingPos, out var vfxEntity))
            {
                handle.VfxId = desiredVfxId;
                handle.VfxEntityId = vfxEntity.Id;
            }
        }

        public void SyncPosition(BattleViewHandle handle, in Vector3 pos)
        {
            if (!IsAvailable) return;
            if (handle == null || handle.VfxEntityId.Index == 0) return;

            _vfx.SyncFollow(_vfxNode.World, handle.VfxEntityId, in pos);
        }

        public void Destroy(BattleViewHandle handle)
        {
            Destroy(_vfxNode.World, handle);
        }

        public void Destroy(EC.IECWorld world, BattleViewHandle handle)
        {
            if (_vfx == null) return;
            if (handle == null || handle.VfxEntityId.Index == 0) return;

            _vfx.DestroyVfxEntity(world, handle.VfxEntityId);
            handle.VfxId = 0;
            handle.VfxEntityId = default;
        }
    }
}
