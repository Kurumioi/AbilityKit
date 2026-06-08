using System.Collections.Generic;
using UnityEngine;
using EC = AbilityKit.World.ECS;

namespace AbilityKit.Game.Flow.Battle.View
{
    public sealed class BattleFloatingTextSystem
    {
        private readonly List<BattleWorldFloatingText> _floatingTexts = new List<BattleWorldFloatingText>(64);

        public void Spawn(in EC.IEntity vfxNode, string text, in Vector3 worldPos, Color color)
        {
            if (!vfxNode.IsValid) return;

            var floatingText = BattleWorldFloatingTextFactory.Create(text, in worldPos, color);
            _floatingTexts.Add(floatingText);
        }

        public void Tick(float deltaTime)
        {
            if (_floatingTexts.Count == 0) return;

            for (var i = _floatingTexts.Count - 1; i >= 0; i--)
            {
                var floatingText = _floatingTexts[i];
                if (floatingText == null)
                {
                    _floatingTexts.RemoveAt(i);
                    continue;
                }

                if (floatingText.Tick(deltaTime)) continue;

                floatingText.Destroy();
                _floatingTexts.RemoveAt(i);
            }
        }

        public void Clear()
        {
            for (var i = 0; i < _floatingTexts.Count; i++)
            {
                _floatingTexts[i]?.Destroy();
            }

            _floatingTexts.Clear();
        }
    }
}
