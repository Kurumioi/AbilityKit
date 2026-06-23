using System.Collections.Generic;
using UnityEngine;

namespace AbilityKit.Game.Flow.Battle.View
{
    internal sealed class BattleWorldFloatingTextFactory
    {
        private readonly Stack<BattleWorldFloatingText> _pool = new Stack<BattleWorldFloatingText>(64);

        public BattleWorldFloatingText Create(string text, in Vector3 worldPos, Color color)
        {
            var floatingText = _pool.Count > 0 ? _pool.Pop() : CreateNew();
            floatingText.Reset(text, in worldPos, color);
            return floatingText;
        }

        public void Release(BattleWorldFloatingText floatingText)
        {
            if (floatingText == null) return;
            if (floatingText.GameObject == null)
            {
                floatingText.Destroy();
                return;
            }

            floatingText.Deactivate();
            _pool.Push(floatingText);
        }

        public void ClearPool()
        {
            while (_pool.Count > 0)
            {
                _pool.Pop()?.Destroy();
            }
        }

        private static BattleWorldFloatingText CreateNew()
        {
            var go = new GameObject("DamageText");
            var textMesh = go.AddComponent<TextMesh>();
            textMesh.fontSize = 42;
            textMesh.characterSize = 0.06f;
            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.alignment = TextAlignment.Center;

            return new BattleWorldFloatingText
            {
                GameObject = go,
                Text = textMesh,
                Lifetime = 0.9f,
                Velocity = new Vector3(0f, 1.5f, 0f),
            };
        }
    }
}
