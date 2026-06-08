using AbilityKit.Ability.Host;
using AbilityKit.Ability.World.Abstractions;

namespace AbilityKit.Game.Flow
{
    internal static class BattleInputSessionIdentity
    {
        public static PlayerId ResolvePlayerId(in BattleStartPlan plan)
        {
            return new PlayerId(string.IsNullOrEmpty(plan.PlayerId) ? "p1" : plan.PlayerId);
        }

        public static WorldId ResolveWorldId(in BattleStartPlan plan)
        {
            return new WorldId(string.IsNullOrEmpty(plan.WorldId) ? "room_1" : plan.WorldId);
        }
    }
}
