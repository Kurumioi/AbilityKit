namespace AbilityKit.Game.Flow
{
    internal sealed class BattleHudDamageTextFormatter
    {
        private readonly BattleDamageTextFormatter _formatter;

        public BattleHudDamageTextFormatter(BattleDamageTextFormatter formatter = null)
        {
            _formatter = formatter ?? new BattleDamageTextFormatter();
        }

        public bool TryFormat(float value, bool isHeal, out string text)
        {
            text = null;
            if (!_formatter.TryFormat(value, isHeal, out var spec)) return false;

            text = spec.Text;
            return true;
        }
    }
}
