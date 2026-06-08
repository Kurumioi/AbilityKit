using AbilityKit.Core.Common.Log;
using AbilityKit.Demo.Moba.Config.Core;
using UnityEngine;

namespace AbilityKit.Game.Flow
{
    internal static class BattleViewModelFactory
    {
        public static GameObject CreateActorShell(MobaConfigDatabase configs, int actorId, int modelId)
        {
            var prefab = TryLoadModelPrefab(configs, modelId, out var scale);

            GameObject go;
            if (prefab != null)
            {
                go = Object.Instantiate(prefab);
                var s = scale <= 0f ? 1f : scale;
                go.transform.localScale = new Vector3(s, s, s);
            }
            else
            {
                go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.transform.localScale = new Vector3(1f, 2f, 1f);
            }

            go.name = $"Actor_{actorId}";
            EnsureAttachRoot(go);
            return go;
        }

        public static GameObject CreateAoeModel(MobaConfigDatabase configs, int modelId)
        {
            if (modelId <= 0) return null;

            var prefab = TryLoadModelPrefab(configs, modelId, out _);

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

            go.name = $"AoeModel_{modelId}";
            return go;
        }

        private static GameObject TryLoadModelPrefab(MobaConfigDatabase configs, int modelId, out float scale)
        {
            scale = 1f;
            if (configs == null || modelId <= 0) return null;

            try
            {
                var model = configs.GetModel(modelId);
                if (model == null) return null;

                scale = model.Scale;
                return string.IsNullOrEmpty(model.PrefabPath)
                    ? null
                    : Resources.Load<GameObject>(model.PrefabPath);
            }
            catch (System.Exception ex)
            {
                Log.Exception(ex);
                return null;
            }
        }

        private static void EnsureAttachRoot(GameObject go)
        {
            if (go == null) return;
            if (go.transform.Find("AttachRoot") != null) return;

            var attachRoot = new GameObject("AttachRoot");
            attachRoot.transform.SetParent(go.transform, worldPositionStays: false);
            attachRoot.transform.localPosition = Vector3.zero;
        }
    }
}
