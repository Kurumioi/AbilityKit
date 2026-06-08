using UnityEngine;

namespace AbilityKit.Game.Flow.Battle.View
{
    internal sealed class BattleWorldFloatingText
    {
        public GameObject GameObject { get; set; }
        public TextMesh Text { get; set; }
        public float Age { get; set; }
        public float Lifetime { get; set; }
        public Vector3 Velocity { get; set; }
        public Color BaseColor { get; set; }

        public bool Tick(float deltaTime)
        {
            if (GameObject == null || Text == null) return false;

            Age += deltaTime;
            GameObject.transform.position += Velocity * deltaTime;

            var t = Lifetime > 0f ? Mathf.Clamp01(Age / Lifetime) : 1f;
            var color = BaseColor;
            color.a = 1f - t;
            Text.color = color;

            return Age < Lifetime;
        }

        public void Destroy()
        {
            if (GameObject != null)
            {
                Object.Destroy(GameObject);
                GameObject = null;
            }

            Text = null;
        }
    }
}
