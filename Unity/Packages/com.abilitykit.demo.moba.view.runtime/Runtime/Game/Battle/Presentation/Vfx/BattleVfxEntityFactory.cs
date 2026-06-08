using System;
using AbilityKit.Game.Battle.Component;
using UnityEngine;
using EC = AbilityKit.World.ECS;

namespace AbilityKit.Game.Battle.Vfx
{
    internal sealed class BattleVfxEntityFactory
    {
        private readonly VfxDatabase _db;
        private readonly BattleVfxPrefabCache _prefabs;

        public BattleVfxEntityFactory(VfxDatabase db, BattleVfxPrefabCache prefabs)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _prefabs = prefabs ?? throw new ArgumentNullException(nameof(prefabs));
        }

        public bool TryCreateEntity(EC.IECWorld world, EC.IEntity parent, int vfxId, EC.IEntityId followTarget, in Vector3 position, out EC.IEntity entity)
        {
            entity = default;
            if (world == null) return false;
            if (!parent.IsValid) return false;
            if (vfxId <= 0) return false;

            if (!_db.TryGet(vfxId, out var dto) || dto == null || string.IsNullOrEmpty(dto.Resource))
            {
                return false;
            }

            var go = CreateGameObject(vfxId, dto.Resource);
            go.transform.position = position;

            var vfxEntity = world.CreateChild(parent);
            vfxEntity.SetName($"Vfx_{vfxId}");
            vfxEntity.WithRef(new BattleVfxComponent { VfxId = vfxId });
            vfxEntity.WithRef(new BattleViewGameObjectComponent { GameObject = go });
            vfxEntity.WithRef(new BattleViewFollowComponent { Target = followTarget, Offset = Vector3.zero });

            if (dto.DurationMs > 0)
            {
                vfxEntity.WithRef(new BattleVfxLifetimeComponent { ExpireAtTime = Time.time + (dto.DurationMs / 1000f) });
            }

            entity = vfxEntity;
            return true;
        }

        private GameObject CreateGameObject(int vfxId, string resource)
        {
            GameObject go;
            if (_prefabs.TryGetPrefab(resource, out var prefab))
            {
                go = UnityEngine.Object.Instantiate(prefab);
            }
            else
            {
                go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                go.transform.localScale = Vector3.one * 0.5f;
            }

            go.name = $"Vfx_{vfxId}";
            return go;
        }
    }
}
