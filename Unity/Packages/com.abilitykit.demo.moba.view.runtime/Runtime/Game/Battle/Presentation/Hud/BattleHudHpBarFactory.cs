using UnityEngine;

namespace AbilityKit.Game.Flow
{
    internal static class BattleHudHpBarFactory
    {
        public static BattleHudHpBarHandle Create(int actorId, BattleHudConfig cfg, RectTransform root)
        {
            var go = CreateGameObject(cfg, root);
            var rt = go.GetComponent<RectTransform>();
            if (rt == null) rt = go.AddComponent<RectTransform>();

            return new BattleHudHpBarHandle
            {
                ActorId = actorId,
                Root = rt,
                HpFill = go.GetComponentInChildren<UnityEngine.UI.Image>(),
                WorldOffset = cfg.HpBarWorldOffset,
            };
        }

        private static GameObject CreateGameObject(BattleHudConfig cfg, RectTransform root)
        {
            var prefab = !string.IsNullOrEmpty(cfg.HpBarPrefabPath)
                ? Resources.Load<GameObject>(cfg.HpBarPrefabPath)
                : null;

            if (prefab != null)
            {
                return Object.Instantiate(prefab, root);
            }

            var go = BattleHudFallbackUiFactory.CreateHpBar();
            go.transform.SetParent(root, worldPositionStays: false);
            return go;
        }
    }
}
