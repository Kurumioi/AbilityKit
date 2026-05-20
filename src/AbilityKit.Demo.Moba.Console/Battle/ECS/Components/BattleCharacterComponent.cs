namespace AbilityKit.Demo.Moba.Console.Battle.ECS.Components
{
    /// <summary>
    /// 战斗角色组件
    /// 存储角色的战斗属性
    /// </summary>
    public sealed class BattleCharacterComponent
    {
        public float Hp;
        public float HpMax;
        public float PhysicsAttack;
        public float MagicAttack;
        public float PhysicsDefense;
        public float MagicDefense;
        public float MoveSpeed;
        public int TeamId;
    }
}
