using System;
using System.Collections.Generic;

namespace AbilityKit.Demo.Moba.Console.View
{
    /// <summary>
    /// 飘字信息
    /// </summary>
    public sealed class FloatingTextInfo
    {
        public int TargetActorId;
        public string Text;
        public bool IsHeal;
        public float Age;
        public float MaxAge;
        public float StartY;
        public float VelocityY;
    }

    /// <summary>
    /// Console 飘字系统
    /// </summary>
    public sealed class ConsoleFloatingTextSystem
    {
        private readonly List<FloatingTextInfo> _floatingTexts = new();
        private const float MaxAge = 1.5f;
        private const float VelocityY = 0.5f;

        public void Spawn(int targetActorId, string text, bool isHeal)
        {
            var info = new FloatingTextInfo
            {
                TargetActorId = targetActorId,
                Text = text,
                IsHeal = isHeal,
                Age = 0,
                MaxAge = MaxAge,
                StartY = 0,
                VelocityY = VelocityY
            };
            _floatingTexts.Add(info);
        }

        public void Tick()
        {
            for (int i = _floatingTexts.Count - 1; i >= 0; i--)
            {
                var ft = _floatingTexts[i];
                ft.Age += 1f / 60f;

                if (ft.Age >= ft.MaxAge)
                {
                    _floatingTexts.RemoveAt(i);
                }
            }
        }

        public IReadOnlyList<FloatingTextInfo> GetAll() => _floatingTexts;
        public void Clear() => _floatingTexts.Clear();
    }
}
