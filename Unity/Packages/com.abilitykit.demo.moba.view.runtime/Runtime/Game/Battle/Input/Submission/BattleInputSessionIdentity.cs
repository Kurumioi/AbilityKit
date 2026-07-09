using AbilityKit.Ability.Host;
using AbilityKit.Ability.World.Abstractions;

namespace AbilityKit.Game.Flow
{
    internal static class BattleInputSessionIdentity
    {
        public static PlayerId ResolvePlayerId(in BattleStartPlan plan)
        {
            var playerId = plan.World.PlayerId;
            return new PlayerId(string.IsNullOrEmpty(playerId) ? "p1" : playerId);
        }

        public static PlayerId ResolvePlayerId(BattleContext ctx)
        {
            if (ctx == null) return new PlayerId("p1");
            var playerId = ctx.ResolveLocalControlPlayerId();
            return new PlayerId(string.IsNullOrEmpty(playerId) ? "p1" : playerId);
        }

        public static WorldId ResolveWorldId(in BattleStartPlan plan)
        {
            var worldId = plan.World.WorldId;
            return new WorldId(string.IsNullOrEmpty(worldId) ? "room_1" : worldId);
        }
    }
}
