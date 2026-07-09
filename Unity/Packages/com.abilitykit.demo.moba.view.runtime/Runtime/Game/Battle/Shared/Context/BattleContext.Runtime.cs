using AbilityKit.Game.Battle;
using AbilityKit.Game.Flow.Battle.Modules;

namespace AbilityKit.Game.Flow
{
    public sealed partial class BattleContext
    {
        public BattleLogicSession Session;
        public BattleStartPlan Plan;
        public int LastFrame;
        public double LogicTimeSeconds;
        public int LocalActorId;
        public string LocalControlPlayerId;
        public BattleSessionHooks Hooks;

        public string ResolveLocalControlPlayerId()
        {
            if (!string.IsNullOrEmpty(LocalControlPlayerId)) return LocalControlPlayerId;
            if (!string.IsNullOrEmpty(Plan.World.PlayerId)) return Plan.World.PlayerId;
            return Plan.LaunchSpec.LocalPlayerId.Value;
        }
    }
}
