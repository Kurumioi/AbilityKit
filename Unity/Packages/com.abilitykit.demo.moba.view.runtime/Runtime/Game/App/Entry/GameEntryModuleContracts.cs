using AbilityKit.Game.Flow;
using AbilityKit.Game.View.Modules;
using AbilityKit.World.ECS;

namespace AbilityKit.Game
{
    public readonly struct GameEntryModuleContext
    {
        public readonly GameEntry Entry;
        public readonly IGameHost Host;
        public readonly IEntity Root;

        public GameEntryModuleContext(GameEntry entry, IEntity root)
        {
            Entry = entry;
            Host = entry;
            Root = root;
        }
    }

    public interface IGameEntryModule : IGameModule<GameEntryModuleContext>, IGameModuleId
    {
    }
}
