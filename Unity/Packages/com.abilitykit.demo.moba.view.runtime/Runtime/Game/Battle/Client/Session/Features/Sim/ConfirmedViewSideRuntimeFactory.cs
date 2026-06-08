using AbilityKit.Ability.World.Abstractions;

namespace AbilityKit.Game.Flow
{
    internal readonly struct ConfirmedViewSideRuntime
    {
        public readonly BattleContext Context;
        public readonly ConfirmedViewSnapshotRuntime SnapshotRuntime;
        public readonly ConfirmedBattleViewFeature Feature;

        public ConfirmedViewSideRuntime(
            BattleContext context,
            ConfirmedViewSnapshotRuntime snapshotRuntime,
            ConfirmedBattleViewFeature feature)
        {
            Context = context;
            SnapshotRuntime = snapshotRuntime;
            Feature = feature;
        }
    }

    internal static class ConfirmedViewSideRuntimeFactory
    {
        public static ConfirmedViewSideRuntime Create(BattleContext sourceCtx, WorldId authWorldId)
        {
            var ctx = ConfirmedViewContextFactory.Create(sourceCtx, authWorldId);
            var snapshotRuntime = ConfirmedViewSnapshotRuntime.Create(ctx);
            var feature = new ConfirmedBattleViewFeature(ctx);

            return new ConfirmedViewSideRuntime(ctx, snapshotRuntime, feature);
        }
    }
}
