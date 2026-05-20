namespace AbilityKit.Demo.Moba.Console.Battle.ECS.Components
{
    /// <summary>
    /// 战斗实体元数据组件
    /// </summary>
    public sealed class BattleEntityMetaComponent
    {
        public BattleEntityKind Kind;
        public int EntityCode;
    }

    /// <summary>
    /// 战斗实体类型
    /// </summary>
    public enum BattleEntityKind
    {
        None = 0,
        Character = 1,
        Projectile = 2,
        Area = 3,
        Item = 4
    }
}
