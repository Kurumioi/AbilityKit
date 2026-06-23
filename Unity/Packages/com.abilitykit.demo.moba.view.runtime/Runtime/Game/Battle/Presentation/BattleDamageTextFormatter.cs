using System;
using System.Globalization;

namespace AbilityKit.Game.Flow
{
    internal readonly struct BattleDamageTextSpec
    {
        public BattleDamageTextSpec(string text, bool isHeal)
        {
            Text = text;
            IsHeal = isHeal;
        }

        public string Text { get; }
        public bool IsHeal { get; }
    }

    internal sealed class BattleDamageTextFormatter
    {
        public bool TryFormat(float value, bool isHeal, out BattleDamageTextSpec spec)
        {
            spec = default;

            var absValue = Math.Abs(value);
            if (absValue <= 0.0001f) return false;

            var text = FormatAmount(absValue);
            text = (isHeal ? "+" : "-") + text;

            spec = new BattleDamageTextSpec(text, isHeal);
            return true;
        }

        private static string FormatAmount(float amount)
        {
            if (amount >= 1f)
            {
                return ((int)Math.Round(amount, MidpointRounding.AwayFromZero)).ToString(CultureInfo.InvariantCulture);
            }

            return amount.ToString("0.0", CultureInfo.InvariantCulture);
        }
    }
}
