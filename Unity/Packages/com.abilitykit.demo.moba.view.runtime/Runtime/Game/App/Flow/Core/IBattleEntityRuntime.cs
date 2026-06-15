using AbilityKit.World.ECS;

namespace AbilityKit.Game.Flow
{
    public interface IBattleEntityRuntime
    {
        IECWorld World { get; }
        IEntity CreateNode(string debugName);
        void DestroyTree(IEntity root);
    }
}
