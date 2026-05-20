namespace AbilityKit.Demo.Moba.Console.Battle.ECS.Components
{
    /// <summary>
    /// 战斗变换组件
    /// 存储实体的位置和朝向
    /// </summary>
    public sealed class BattleTransformComponent
    {
        public float X;
        public float Y;
        public float Z;
        public float ForwardX;
        public float ForwardZ;

        public BattleTransformComponent()
        {
        }

        public BattleTransformComponent(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }
    }
}
