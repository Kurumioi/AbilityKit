using UnityEngine;

namespace AbilityKit.Game.Flow
{
    internal sealed class BattleHudDamageTextFormatter
    {
        public bool TryFormat(float value, bool isHeal, out string text)
        {
            text = null;

            var absValue = Mathf.Abs(value);
            if (absValue <= 0.0001f) return false;

            var sign = isHeal ? "+" : "-";
            text = sign + Mathf.RoundToInt(absValue).ToString();
            return true;
        }
    }
}
