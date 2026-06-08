using System;
using AbilityKit.Game.Battle.Entity;
using AbilityKit.Game.Battle.Vfx;
using UnityEngine;
using EC = AbilityKit.World.ECS;

namespace AbilityKit.Game.Flow
{
    public sealed class BattleViewBinder : IMonoViewHandleRegistry
    {
        private readonly BattleViewHandleStore _handles = new BattleViewHandleStore();
        private readonly BattleViewShellController _shells;
        private readonly BattleViewAttachedVfxController _attachedVfx;
        private readonly BattleViewTransformController _transforms;
        private readonly BattleViewEntitySyncController _sync;
        private readonly BattleViewLifecycleController _lifecycle;

        public BattleViewBinder(BattleVfxManager vfx, in EC.IEntity vfxNode, IBattleViewShellLoader shellLoader = null)
        {
            _shells = new BattleViewShellController(shellLoader ?? new ResourceBattleViewShellLoader(), this);
            _attachedVfx = new BattleViewAttachedVfxController(vfx, vfxNode);
            _transforms = new BattleViewTransformController(_handles, _attachedVfx);
            _sync = new BattleViewEntitySyncController(_handles, _shells, _attachedVfx, _transforms);
            _lifecycle = new BattleViewLifecycleController(_handles, _shells, _attachedVfx, _transforms);
        }

        public bool InterpolationEnabled
        {
            get => _transforms.InterpolationEnabled;
            set => _transforms.InterpolationEnabled = value;
        }

        public float BackTimeTicks
        {
            get => _transforms.BackTimeTicks;
            set => _transforms.BackTimeTicks = value;
        }

        public float MaxLagTicks
        {
            get => _transforms.MaxLagTicks;
            set => _transforms.MaxLagTicks = value;
        }

        public bool TryGetShellGameObject(EC.IEntityId id, out GameObject go)
        {
            go = null;
            if (!_handles.TryGet(id, out var handle)) return false;
            if (handle.Destroyed) return false;
            if (handle.GameObject == null) return false;

            go = handle.GameObject;
            return true;
        }

        public bool TryGetInterpolatedPos(EC.IEntityId id, out Vector3 pos)
        {
            return _transforms.TryGetInterpolatedPos(id, out pos);
        }

        public void ForEachShellGameObject(Action<int, EC.IEntityId, GameObject> visitor)
        {
            if (visitor == null) return;

            _handles.ForEach((id, handle) =>
            {
                if (handle == null || handle.Destroyed || handle.GameObject == null) return;
                visitor(handle.ActorId, id, handle.GameObject);
            });
        }

        public bool TryGetAttachRoot(BattleNetId netId, out Transform transform)
        {
            transform = null;
            if (netId.Value <= 0) return false;

            if (!_handles.TryGetByActorId(netId.Value, out _, out var handle)) return false;
            if (handle.Destroyed || handle.GameObject == null) return false;

            var child = handle.GameObject.transform.Find("AttachRoot");
            if (child == null) return false;

            transform = child;
            return true;
        }

        public void Sync(EC.IEntity entity)
        {
            _sync.Sync(entity);
        }

        public void Sync(EC.IEntity entity, BattleContext ctx)
        {
            _sync.Sync(entity, ctx);
        }

        public void TickInterpolation(BattleContext ctx, float deltaTime)
        {
            _transforms.Tick(ctx, deltaTime);
        }

        public void OnDestroyed(EC.IEntityId id)
        {
            _lifecycle.OnDestroyed(id);
        }

        public void Clear()
        {
            _lifecycle.Clear();
        }

        public void RebindAll(EC.IECWorld world)
        {
            if (world == null) return;
            world.ForEachAlive(entity => Sync(entity));
        }

        public void RebindAll(EC.IECWorld world, BattleContext ctx)
        {
            if (world == null) return;
            world.ForEachAlive(entity => Sync(entity, ctx));
        }

        void IMonoViewHandleRegistry.OnMonoViewHandleDestroyed(MonoViewHandle handle)
        {
            if (handle == null) return;
            if (handle.ActorId <= 0) return;
            if (!_handles.TryGetByActorId(handle.ActorId, out _, out var viewHandle)) return;

            if (!ReferenceEquals(viewHandle.ViewHandle, handle)) return;

            viewHandle.GameObject = null;
            viewHandle.ViewHandle = null;
        }
    }
}
