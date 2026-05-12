using AbilityKit.Ability.World.Services;

namespace AbilityKit.Core.Math
{
    /// <summary>
    /// 碰撞服务默认实现。
    /// 使用 NaiveCollisionWorld 作为碰撞世界。
    /// </summary>
    public sealed class CollisionService : ICollisionService, IService
    {
        private readonly ICollisionWorld _world = new NaiveCollisionWorld();

        public ICollisionWorld World => _world;

        public void Dispose()
        {
        }
    }
}
