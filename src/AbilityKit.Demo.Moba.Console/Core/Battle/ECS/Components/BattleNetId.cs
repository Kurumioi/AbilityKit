namespace AbilityKit.Demo.Moba.Console.Core.Battle.ECS.Components
{
    public readonly struct BattleNetId
    {
        public readonly int Value;

        public BattleNetId(int value)
        {
            Value = value;
        }

        public static BattleNetId Invalid => default;
        public override string ToString() => Value.ToString();
    }
}
