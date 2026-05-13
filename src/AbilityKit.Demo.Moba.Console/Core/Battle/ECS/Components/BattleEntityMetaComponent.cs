namespace AbilityKit.Demo.Moba.Console.Core.Battle.ECS.Components
{
    public enum BattleEntityKind
    {
        Unknown = 0,
        Character = 1,
        Projectile = 2,
        Vfx = 3
    }

    public sealed class BattleEntityMetaComponent
    {
        public BattleEntityKind Kind = BattleEntityKind.Unknown;
        public int EntityCode;
    }
}
