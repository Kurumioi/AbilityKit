using AbilityKit.Ability.World.Services;

namespace AbilityKit.Core.Mathematics
{
    public interface ICollisionService : IService
    {
        ICollisionWorld World { get; }
    }
}
