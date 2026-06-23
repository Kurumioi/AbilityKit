using AbilityKit.Game.Flow;
using UnityEngine;

namespace AbilityKit.Game.Flow.Battle.ViewEvents
{
    internal sealed class BattleDamageFloatingTextFormatter
    {
        private static readonly Color DamageColor = new Color(1f, 0.2f, 0.2f, 1f);
        private static readonly Color HealColor = new Color(0.2f, 1f, 0.2f, 1f);

        private readonly BattleDamageTextFormatter _formatter;

        public BattleDamageFloatingTextFormatter(BattleDamageTextFormatter formatter = null)
        {
            _formatter = formatter ?? new BattleDamageTextFormatter();
        }

        public bool TryFormat(float value, bool isHeal, out BattleFloatingTextSpec spec)
        {
            spec = default;
            if (!_formatter.TryFormat(value, isHeal, out var textSpec)) return false;

            spec = new BattleFloatingTextSpec(textSpec.Text, textSpec.IsHeal ? HealColor : DamageColor);
            return true;
        }
    }
}
