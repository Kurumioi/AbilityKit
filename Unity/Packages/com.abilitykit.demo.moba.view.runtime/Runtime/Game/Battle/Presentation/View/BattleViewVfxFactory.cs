using AbilityKit.Game.Battle.Vfx;
using UnityEngine;

namespace AbilityKit.Game.Flow
{
    internal interface IBattleViewVfxPrefabLoader
    {
        GameObject Load(string path);
    }

    internal sealed class ResourcesBattleViewVfxPrefabLoader : IBattleViewVfxPrefabLoader
    {
        public GameObject Load(string path)
        {
            return string.IsNullOrEmpty(path) ? null : Resources.Load<GameObject>(path);
        }
    }

    internal sealed class BattleViewVfxFactory
    {
        private readonly BattleViewPrimitiveFactory _primitives;
        private readonly IBattleViewVfxPrefabLoader _loader;

        public BattleViewVfxFactory(
            BattleViewPrimitiveFactory primitives = null,
            IBattleViewVfxPrefabLoader loader = null)
        {
            _primitives = primitives ?? new BattleViewPrimitiveFactory();
            _loader = loader ?? new ResourcesBattleViewVfxPrefabLoader();
        }

        public GameObject CreateAoeVfx(VfxDatabase db, int vfxId)
        {
            if (vfxId <= 0) return null;
            if (db == null) return null;

            if (!db.TryGet(vfxId, out var dto) || dto == null || string.IsNullOrEmpty(dto.Resource))
            {
                return null;
            }

            var prefab = _loader.Load(dto.Resource);
            GameObject go;
            if (prefab != null)
            {
                go = Object.Instantiate(prefab);
            }
            else
            {
                go = _primitives.CreateVfxFallback();
            }

            go.name = $"AoeVfx_{vfxId}";
            return go;
        }
    }
}
