using AbilityKit.Game;
using AbilityKit.World.ECS;

namespace AbilityKit.Game.Flow
{
    public readonly struct GamePhaseContext
    {
        public readonly GameEntry Entry;
        public readonly IEntity Root;

        public GamePhaseContext(GameEntry entry, IEntity root)
        {
            Entry = entry;
            Root = root;
        }
    }

    public interface IGamePhase
    {
        void Enter(in GamePhaseContext ctx);
        void Exit(in GamePhaseContext ctx);
        void Tick(in GamePhaseContext ctx, float deltaTime);
    }

    public interface IGamePhaseFeature
    {
        void OnAttach(in GamePhaseContext ctx);
        void OnDetach(in GamePhaseContext ctx);
        void Tick(in GamePhaseContext ctx, float deltaTime);
    }

    public interface IOnGUIFeature
    {
        void OnGUI(in GamePhaseContext ctx);
    }
}
