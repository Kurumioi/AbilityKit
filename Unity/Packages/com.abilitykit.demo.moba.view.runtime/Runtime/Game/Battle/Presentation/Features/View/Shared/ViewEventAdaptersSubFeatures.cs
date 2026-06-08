using AbilityKit.Demo.Moba.Services;
using AbilityKit.Game.Flow.Battle.ViewEvents.Snapshot;
using AbilityKit.Game.Flow.Battle.ViewEvents.Triggering;
using AbilityKit.Game.Flow.Modules;

namespace AbilityKit.Game.Flow
{
    internal sealed class ViewEventAdaptersSubFeature<TFeature> : IViewSubFeature<TFeature>
        where TFeature : class, IViewFeatureRuntime
    {
        public void OnAttach(in FeatureModuleContext<TFeature> ctx)
        {
            var runtime = ctx.Feature;
            if (runtime == null) return;

            runtime.SnapshotAdapter?.Dispose();
            runtime.SnapshotAdapter = null;

            runtime.TriggerAdapter?.Dispose();
            runtime.TriggerAdapter = null;

            var mode = runtime.Context != null ? runtime.Context.Plan.ViewEventSourceMode : BattleViewEventSourceMode.SnapshotOnly;

            if ((mode == BattleViewEventSourceMode.TriggerOnly || mode == BattleViewEventSourceMode.Hybrid) && runtime.Context?.Session != null)
            {
                runtime.TriggerAdapter = new BattleTriggerEventViewAdapter(runtime.Context.Session, runtime.EventSink);
            }

            if ((mode == BattleViewEventSourceMode.SnapshotOnly || mode == BattleViewEventSourceMode.Hybrid)
                && runtime.Context != null
                && runtime.Context.TryGetFrameSnapshots(out var snapshots))
            {
                runtime.SnapshotAdapter = new BattleSnapshotViewAdapter(snapshots, runtime.EventSink);
            }
        }

        public void OnDetach(in FeatureModuleContext<TFeature> ctx)
        {
            var runtime = ctx.Feature;
            if (runtime == null) return;

            runtime.SnapshotAdapter?.Dispose();
            runtime.SnapshotAdapter = null;

            runtime.TriggerAdapter?.Dispose();
            runtime.TriggerAdapter = null;
        }

        public void Tick(in FeatureModuleContext<TFeature> ctx, float deltaTime) { }

        public void RebindAll(in FeatureModuleContext<TFeature> ctx) { }
    }
}
