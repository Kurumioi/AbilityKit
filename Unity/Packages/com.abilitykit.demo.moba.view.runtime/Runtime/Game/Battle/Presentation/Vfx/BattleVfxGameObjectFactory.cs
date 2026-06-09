using System;
using AbilityKit.Game.Flow;
using UnityEngine;

namespace AbilityKit.Game.Battle.Vfx
{
    internal sealed class BattleVfxGameObjectFactory
    {
        private readonly BattleVfxPrefabCache _prefabs;
        private readonly BattleViewPrimitiveFactory _primitives;
        private readonly BattleVfxGameObjectInstantiator _instantiator;

        public BattleVfxGameObjectFactory(
            BattleVfxPrefabCache prefabs,
            BattleViewPrimitiveFactory primitives = null,
            BattleVfxGameObjectInstantiator instantiator = null)
        {
            _prefabs = prefabs ?? throw new ArgumentNullException(nameof(prefabs));
            _primitives = primitives ?? new BattleViewPrimitiveFactory();
            _instantiator = instantiator ?? new BattleVfxGameObjectInstantiator();
        }

        public GameObject Create(int vfxId, string resource)
        {
            GameObject go;
            if (_prefabs.TryGetPrefab(resource, out var prefab))
            {
                go = _instantiator.Instantiate(prefab);
            }
            else
            {
                go = _primitives.CreateVfxFallback();
            }

            go.name = $"Vfx_{vfxId}";
            return go;
        }
    }

    internal sealed class BattleVfxGameObjectInstantiator
    {
        public GameObject Instantiate(GameObject prefab)
        {
            return prefab != null ? UnityEngine.Object.Instantiate(prefab) : null;
        }
    }
}
