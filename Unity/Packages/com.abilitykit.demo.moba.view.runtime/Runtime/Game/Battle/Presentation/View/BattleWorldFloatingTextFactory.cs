using UnityEngine;

namespace AbilityKit.Game.Flow.Battle.View
{
    internal static class BattleWorldFloatingTextFactory
    {
        public static BattleWorldFloatingText Create(string text, in Vector3 worldPos, Color color)
        {
            var go = new GameObject("DamageText");
            go.transform.position = worldPos;

            var textMesh = go.AddComponent<TextMesh>();
            textMesh.text = text;
            textMesh.color = color;
            textMesh.fontSize = 42;
            textMesh.characterSize = 0.06f;
            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.alignment = TextAlignment.Center;

            return new BattleWorldFloatingText
            {
                GameObject = go,
                Text = textMesh,
                Age = 0f,
                Lifetime = 0.9f,
                Velocity = new Vector3(0f, 1.5f, 0f),
                BaseColor = color,
            };
        }
    }
}
