using AbilityKit.Ability.World.Abstractions;
using AbilityKit.Game.Flow.Battle.ViewEvents;

namespace AbilityKit.Game.Flow
{
    internal static class ConfirmedAuthorityDebugStatsPublisher
    {
        public static void Initialize(WorldId authWorldId)
        {
            BattleFlowDebugProvider.ConfirmedAuthorityWorldStats = new ConfirmedAuthorityWorldStatsSnapshot
            {
                WorldId = authWorldId.Value,
                ConfirmedFrame = 0,
                PredictedFrame = 0,
                AuthorityInputTargetFrame = 0,
                AuthorityDriveTargetFrame = 0,
                AuthorityLastTickedFrame = 0,
                ViewEventTotal = 0,
                RecentViewEvents = null,
            };
        }

        public static void Update(
            int confirmedFrame,
            int predictedFrame,
            int inputTargetFrame,
            int driveTargetFrame,
            int lastTickedFrame,
            DebugBattleViewEventSink viewEventSink)
        {
            var stats = BattleFlowDebugProvider.ConfirmedAuthorityWorldStats;
            if (stats == null) return;

            stats.ConfirmedFrame = confirmedFrame;
            stats.PredictedFrame = predictedFrame;
            stats.AuthorityInputTargetFrame = inputTargetFrame;
            stats.AuthorityDriveTargetFrame = driveTargetFrame;
            stats.AuthorityLastTickedFrame = lastTickedFrame;

            if (viewEventSink == null) return;

            stats.ViewEventTotal = viewEventSink.Total;
            stats.RecentViewEvents = viewEventSink.GetRecentLines();
        }

        public static void Clear(BattleContext ctx)
        {
            BattleFlowDebugProvider.ConfirmedAuthorityWorldStats = null;

            if (ctx != null)
            {
                ctx.PredictionStats = null;
            }
        }
    }
}
