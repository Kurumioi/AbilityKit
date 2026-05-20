namespace AbilityKit.Demo.Moba.Console.Battle.ECS.Components
{
    /// <summary>
    /// 战斗投射物组件
    /// </summary>
    public sealed class BattleProjectileComponent
    {
        public BattleNetId OwnerNetId;
        public float Speed;
        public float MaxDistance;
        public float Damage;
        public int[] HitTargetTeams;
    }
}
