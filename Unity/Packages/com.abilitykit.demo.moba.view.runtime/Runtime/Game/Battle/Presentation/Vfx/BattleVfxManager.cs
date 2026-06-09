using System;
using AbilityKit.Game.Battle.Component;
using AbilityKit.Game.Flow;
using AbilityKit.World.ECS;
using UnityEngine;
using EC = AbilityKit.World.ECS;

namespace AbilityKit.Game.Battle.Vfx
{
    public sealed class BattleVfxManager
    {
        private readonly BattleVfxEntityFactory _factory;
        private readonly BattleVfxFollowController _followController;

        public BattleVfxManager(VfxDatabase db)
            : this(db, null)
        {
        }

        internal BattleVfxManager(VfxDatabase db, BattleVfxManagerComponentFactory components)
        {
            if (db == null) throw new ArgumentNullException(nameof(db));
            components ??= new BattleVfxManagerComponentFactory();

            var prefabs = components.CreatePrefabs();
            var lifetime = components.CreateLifetimePolicy();
            _factory = components.CreateEntityFactory(db, prefabs, lifetime);
            _followController = components.CreateFollowController(lifetime);
        }

        public bool TryCreateVfxEntity(EC.IECWorld world, EC.IEntity parent, int vfxId, EC.IEntityId followTarget, in Vector3 position, out EC.IEntity entity)
        {
            return _factory.TryCreateEntity(world, parent, vfxId, followTarget, in position, out entity);
        }

        public void Tick(in EC.IEntity vfxRoot)
        {
            Tick(vfxRoot, binder: null);
        }

        public void Tick(in EC.IEntity vfxRoot, BattleViewBinder binder)
        {
            _followController.Tick(vfxRoot, binder, DestroyVfxEntity);
        }

        public void DestroyVfxEntity(EC.IECWorld world, EC.IEntityId id)
        {
            if (world == null) return;
            if (!world.IsAlive(id)) return;

            var e = world.Wrap(id);
            if (e.TryGetRef(out BattleViewGameObjectComponent goComp) && goComp != null && goComp.GameObject != null)
            {
                UnityEngine.Object.Destroy(goComp.GameObject);
                goComp.GameObject = null;
            }

            if (e.IsValid) e.Destroy();
        }

        public void SyncFollow(EC.IECWorld world, EC.IEntityId vfxEntityId, in Vector3 targetPos)
        {
            _followController.SyncFollow(world, vfxEntityId, in targetPos);
        }
    }

    internal sealed class BattleVfxManagerComponentFactory
    {
        public BattleVfxPrefabCache CreatePrefabs()
        {
            return new BattleVfxPrefabCache();
        }

        public BattleVfxLifetimePolicy CreateLifetimePolicy()
        {
            return new BattleVfxLifetimePolicy();
        }

        public BattleVfxEntityFactory CreateEntityFactory(
            VfxDatabase db,
            BattleVfxPrefabCache prefabs,
            BattleVfxLifetimePolicy lifetime)
        {
            return new BattleVfxEntityFactory(db, prefabs, lifetime);
        }

        public BattleVfxFollowController CreateFollowController(BattleVfxLifetimePolicy lifetime)
        {
            return new BattleVfxFollowController(lifetime);
        }
    }
}
