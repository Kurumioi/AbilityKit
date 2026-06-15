using AbilityKit.Game.View.Flow;
using AbilityKit.World.ECS;

namespace AbilityKit.Game.Flow
{
    public readonly struct GamePhaseContext
    {
        public readonly IGameHost Entry;
        public readonly IEntity Root;

        public GamePhaseContext(IGameHost entry, IEntity root)
        {
            Entry = entry;
            Root = root;
        }
    }

    public interface IGamePhase : IPhase<GamePhaseContext>
    {
    }

    public interface IGamePhaseFeature : IPhaseFeature<GamePhaseContext>
    {
    }

    public interface IOnGUIFeature : IPhaseGuiFeature<GamePhaseContext>
    {
    }
}
