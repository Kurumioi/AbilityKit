using UnityEngine;

namespace AbilityKit.Game.Flow.Battle.ViewEvents
{
    internal sealed class BattleDamageFloatingTextFormatter
    {
        private static readonly Color DamageColor = new Color(1f, 0.2f, 0.2f, 1f);
        private static readonly Color HealColor = new Color(0.2f, 1f, 0.2f, 1f);

        public bool TryFormat(float value, bool isHeal, out BattleFloatingTextSpec spec)
        {
            spec = default;
            if (value == 0f) return false;

            var amount = Mathf.Abs(value);
            var text = amount >= 1f ? Mathf.RoundToInt(amount).ToString() : amount.ToString("0.0");
            if (isHeal) text = $"+{text}";

            spec = new BattleFloatingTextSpec(text, isHeal ? HealColor : DamageColor);
            return true;
        }
    }
}
