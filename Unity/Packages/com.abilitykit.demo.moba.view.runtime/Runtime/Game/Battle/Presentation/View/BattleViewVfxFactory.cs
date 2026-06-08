using AbilityKit.Game.Battle.Vfx;
using UnityEngine;

namespace AbilityKit.Game.Flow
{
    internal static class BattleViewVfxFactory
    {
        public static GameObject CreateAoeVfx(VfxDatabase db, int vfxId)
        {
            if (vfxId <= 0) return null;
            if (db == null) return null;

            if (!db.TryGet(vfxId, out var dto) || dto == null || string.IsNullOrEmpty(dto.Resource))
            {
                return null;
            }

            var prefab = Resources.Load<GameObject>(dto.Resource);
            GameObject go;
            if (prefab != null)
            {
                go = Object.Instantiate(prefab);
            }
            else
            {
                go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                go.transform.localScale = Vector3.one * 0.5f;
            }

            go.name = $"AoeVfx_{vfxId}";
            return go;
        }
    }
}
