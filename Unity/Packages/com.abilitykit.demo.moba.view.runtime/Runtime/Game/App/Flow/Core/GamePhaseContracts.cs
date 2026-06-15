using AbilityKit.Game.View.Flow;

namespace AbilityKit.Game.Flow
{
    public readonly struct GamePhaseContext
    {
        public readonly IGameHost Entry;
        public readonly IGameFeatureStore Features;
        public readonly IBattleEntityRuntime BattleEntities;

        public GamePhaseContext(IGameHost entry, IGameFeatureStore features, IBattleEntityRuntime battleEntities)
        {
            Entry = entry;
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
