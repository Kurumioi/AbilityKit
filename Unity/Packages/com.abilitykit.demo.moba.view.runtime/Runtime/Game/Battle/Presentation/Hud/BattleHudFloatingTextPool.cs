using System.Collections.Generic;
using UnityEngine;

namespace AbilityKit.Game.Flow
{
    internal sealed class BattleHudFloatingTextPool
    {
        private readonly RectTransform _root;
        private readonly Stack<BattleHudFloatingTextHandle> _pool = new Stack<BattleHudFloatingTextHandle>(64);

        public BattleHudFloatingTextPool(RectTransform root)
        {
            _root = root;
        }

        public BattleHudFloatingTextHandle Rent()
        {
            var floating = _pool.Count > 0 ? _pool.Pop() : null;
            if (floating == null)
            {
                var go = BattleHudFallbackUiFactory.CreateFloatingText();
                go.transform.SetParent(_root, worldPositionStays: false);

                return new BattleHudFloatingTextHandle
                {
                    Root = go.GetComponent<RectTransform>(),
                    Text = go.GetComponentInChildren<UnityEngine.UI.Text>(),
                };
            }

            if (floating.Root != null && floating.Root.parent != _root)
            {
                floating.Root.SetParent(_root, worldPositionStays: false);
            }

            if (floating.Root != null) floating.Root.gameObject.SetActive(true);
            return floating;
        }

        public void Recycle(BattleHudFloatingTextHandle floating)
        {
            if (floating == null) return;

            if (floating.Root != null)
            {
                floating.Root.gameObject.SetActive(false);
            }

            _pool.Push(floating);
        }

        public void Clear()
        {
            while (_pool.Count > 0)
            {
                DestroyHandle(_pool.Pop());
            }
        }

        public static void DestroyHandle(BattleHudFloatingTextHandle floating)
        {
            if (floating?.Root != null)
            {
                Object.Destroy(floating.Root.gameObject);
            }
        }
    }
}
