using AbilityKit.Game.View.Flow;
using AbilityKit.World.ECS;

namespace AbilityKit.Game.Flow
{
    public readonly struct GamePhaseContext
    {
        public readonly IGameHost Entry;
        public readonly IEntity Root;
        public readonly IGameFeatureStore Features;
        public readonly IBattleEntityRuntime BattleEntities;

        public GamePhaseContext(IGameHost entry, IEntity root, IGameFeatureStore features, IBattleEntityRuntime battleEntities)
        {
            Entry = entry;
            Root = root;
            Features = features;
            BattleEntities = battleEntities;
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
