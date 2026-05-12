using AbilityKit.Ability.World.DI;

namespace AbilityKit.Demo.Moba.Systems
{
    public sealed partial class MobaWorldBootstrapModule
    {
        private static void RegisterMovementAndCollision(WorldContainerBuilder builder)
        {
            // 注册碰撞服务
            builder.RegisterService<global::AbilityKit.Core.Math.ICollisionService, global::AbilityKit.Core.Math.CollisionService>();
        }
    }
}
