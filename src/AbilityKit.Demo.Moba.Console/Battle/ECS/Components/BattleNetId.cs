using System;

namespace AbilityKit.Demo.Moba.Console.Battle.ECS
{
    /// <summary>
    /// 战斗网络 ID
    /// 用于跨网络同步实体
    /// </summary>
    public readonly struct BattleNetId : IEquatable<BattleNetId>
    {
        public int Value { get; }

        public BattleNetId(int value)
        {
            Value = value;
        }

        public static readonly BattleNetId Invalid = new(0);

        public bool IsValid => Value != 0;

        public bool Equals(BattleNetId other) => Value == other.Value;
        public override bool Equals(object? obj) => obj is BattleNetId other && Equals(other);
        public override int GetHashCode() => Value.GetHashCode();
        public override string ToString() => $"NetId:{Value}";

        public static bool operator ==(BattleNetId left, BattleNetId right) => left.Equals(right);
        public static bool operator !=(BattleNetId left, BattleNetId right) => !left.Equals(right);
    }
}
