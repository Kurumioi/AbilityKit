using AbilityKit.Game.Battle.Component;
using AbilityKit.Game.Battle.Entity;
using AbilityKit.Game.Battle.Vfx;
using EC = AbilityKit.World.ECS;

namespace AbilityKit.Game.Flow
{
    internal sealed class BattleViewEntitySyncController
    {
        private readonly BattleViewHandleStore _handles;
        private readonly BattleViewShellController _shells;
        private readonly BattleViewAttachedVfxController _attachedVfx;
        private readonly BattleViewTransformController _transforms;

        public BattleViewEntitySyncController(
            BattleViewHandleStore handles,
            BattleViewShellController shells,
            BattleViewAttachedVfxController attachedVfx,
            BattleViewTransformController transforms)
        {
            _handles = handles;
            _shells = shells;
            _attachedVfx = attachedVfx;
            _transforms = transforms;
        }

        public void Sync(EC.IEntity entity)
        {
            Sync(entity, ctx: null);
        }

        public void Sync(EC.IEntity entity, BattleContext ctx)
        {
            if (!entity.TryGetRef(out BattleNetIdComponent netIdComp) || netIdComp == null) return;
            if (!entity.TryGetRef(out BattleTransformComponent t) || t == null) return;
            var meta = entity.TryGetRef(out BattleEntityMetaComponent metaComp) ? metaComp : null;

            var actorId = netIdComp.NetId.Value;
            if (actorId <= 0) return;

            var desiredModelId = BattleViewFactory.ResolveModelId(meta);
            var handle = _handles.GetOrCreate(entity.Id);
            if (handle.Destroyed) return;

            _handles.SetActorId(handle, actorId, entity.Id);
            _transforms.SampleEntity(entity, in t.Position, ctx);

            if (desiredModelId > 0 && (handle.GameObject == null || handle.ModelId != desiredModelId))
            {
                handle.Version++;
                _shells.Recreate(handle, actorId, desiredModelId);
            }

            _attachedVfx.SyncProjectileVfx(entity, handle, meta);
        }
    }
}
