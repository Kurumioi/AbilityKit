using System;
using System.Collections.Generic;
using UnityEngine;

namespace AbilityKit.Game.Battle.Vfx
{
    internal sealed class BattleVfxPrefabCache
    {
        private readonly Dictionary<string, GameObject> _prefabs = new Dictionary<string, GameObject>(StringComparer.Ordinal);

        public bool TryGetPrefab(string resource, out GameObject prefab)
        {
            prefab = null;
            if (string.IsNullOrEmpty(resource))
            {
                return false;
            }

            if (!_prefabs.TryGetValue(resource, out prefab) || prefab == null)
            {
                prefab = Resources.Load<GameObject>(resource);
                _prefabs[resource] = prefab;
            }

            return prefab != null;
        }
    }
}
