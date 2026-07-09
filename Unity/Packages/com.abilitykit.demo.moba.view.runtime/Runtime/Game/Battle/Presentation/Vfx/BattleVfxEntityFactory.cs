using System;
using AbilityKit.Game.Battle.Component;
using AbilityKit.Game.Flow;
using UnityEngine;
using EC = AbilityKit.World.ECS;

namespace AbilityKit.Game.Battle.Vfx
{
    internal sealed class BattleVfxEntityFactory
    {
        private readonly VfxDatabase _db;
        private readonly BattleVfxGameObjectFactory _gameObjects;
        private readonly BattleVfxLifetimePolicy _lifetime;
        private readonly BattleVfxEntityBuilder _entities;

        public BattleVfxEntityFactory(
            VfxDatabase db,
            BattleVfxPrefabCache prefabs,
            BattleVfxLifetimePolicy lifetime = null,
            BattleVfxGameObjectFactory gameObjects = null)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _lifetime = lifetime ?? new BattleVfxLifetimePolicy();
            _gameObjects = gameObjects ?? new BattleVfxGameObjectFactory(prefabs);
            _entities = new BattleVfxEntityBuilder(_lifetime);
        }

        public bool TryCreateEntity(EC.IECWorld world, EC.IEntity parent, int vfxId, EC.IEntityId followTarget, in Vector3 position, out EC.IEntity entity)
        {
            return TryCreateEntity(world, parent, vfxId, followTarget, in position, Quaternion.identity, out entity);
        }

        public bool TryCreateEntity(EC.IECWorld world, EC.IEntity parent, int vfxId, EC.IEntityId followTarget, in Vector3 position, in Quaternion rotation, out EC.IEntity entity)
        {
            return TryCreateEntity(world, parent, vfxId, followTarget, 0, in position, in rotation, out entity);
        }

        public bool TryCreateEntity(EC.IECWorld world, EC.IEntity parent, int vfxId, EC.IEntityId followTarget, int followTargetActorId, in Vector3 position, in Quaternion rotation, out EC.IEntity entity)
        {
            return TryCreateEntity(world, parent, vfxId, followTarget, followTargetActorId, in position, in rotation, 0, out entity);
        }

        public bool TryCreateEntity(EC.IECWorld world, EC.IEntity parent, int vfxId, EC.IEntityId followTarget, int followTargetActorId, in Vector3 position, in Quaternion rotation, int durationMsOverride, out EC.IEntity entity)
        {
            entity = default;
            if (world == null) return false;
            if (!parent.IsValid) return false;
            if (vfxId <= 0) return false;

            var durationMs = 650;
            GameObject go;
            if (_db.TryGet(vfxId, out var dto) && dto != null && !string.IsNullOrEmpty(dto.Resource))
            {
                go = _gameObjects.Create(vfxId, dto.Resource);
                durationMs = dto.DurationMs;
            }
            else
            {
                go = _gameObjects.CreatePlaceholder(vfxId);
            }

            if (durationMsOverride > 0)
            {
                durationMs = durationMsOverride;
            }

            go.transform.SetPositionAndRotation(position, rotation);

            entity = _entities.Create(world, parent, vfxId, followTarget, followTargetActorId, go, durationMs);
            return true;
        }
    }

    internal sealed class BattleVfxEntityBuilder
    {
        private readonly BattleVfxLifetimePolicy _lifetime;

        public BattleVfxEntityBuilder(BattleVfxLifetimePolicy lifetime)
        {
            _lifetime = lifetime ?? new BattleVfxLifetimePolicy();
        }

        public EC.IEntity Create(
            EC.IECWorld world,
            EC.IEntity parent,
            int vfxId,
            EC.IEntityId followTarget,
            int followTargetActorId,
            GameObject go,
            int durationMs)
        {
            var vfxEntity = world.CreateChild(parent);
            vfxEntity.SetName($"Vfx_{vfxId}");
            vfxEntity.WithRef(new BattleVfxComponent { VfxId = vfxId });
            vfxEntity.WithRef(new BattleViewGameObjectComponent { GameObject = go });
            vfxEntity.WithRef(new BattleViewFollowComponent { Target = followTarget, TargetActorId = followTargetActorId, Offset = Vector3.zero });

            _lifetime.AttachIfNeeded(vfxEntity, durationMs);
            return vfxEntity;
        }
    }
}
