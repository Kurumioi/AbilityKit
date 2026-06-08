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
        public BattleSessionHooks Hooks;
    }
}
