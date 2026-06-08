using System;
using AbilityKit.Game.Flow.Battle.Modules;
using AbilityKit.Game.Flow.Modules;

namespace AbilityKit.Game.Flow
{
    internal sealed class SessionSnapshotRoutingSubFeature :
        ISessionSubFeature<BattleSessionFeature>,
        IGameModuleId,
        IGameModuleDependencies
    {
        private Action _onSessionStarting;
        private Action _onSessionStopping;

        public string Id => "snapshot_routing";

        public System.Collections.Generic.IEnumerable<string> Dependencies => new[] { "session_events" };

        public void OnAttach(in FeatureModuleContext<BattleSessionFeature> ctx)
        {
            if (!BattleSessionFeatureRuntimeAccess.TryGet<ISessionSnapshotRoutingRuntime>(ctx, out var runtime)) return;

            _onSessionStarting = () => runtime.EnsureSnapshotRoutingBuilt();
            _onSessionStopping = () => runtime.DisposeSnapshotRoutingIfAny();

            runtime.Hooks?.SessionStarting.Add(_onSessionStarting);
            runtime.Hooks?.SessionStopping.Add(_onSessionStopping);
        }

        public void OnDetach(in FeatureModuleContext<BattleSessionFeature> ctx)
        {
            BattleSessionFeatureRuntimeAccess.TryGet<ISessionSnapshotRoutingRuntime>(ctx, out var runtime);
            if (_onSessionStarting != null && runtime != null)
            {
                runtime.Hooks?.SessionStarting.Remove(_onSessionStarting);
            }
            if (_onSessionStopping != null && runtime != null)
            {
                runtime.Hooks?.SessionStopping.Remove(_onSessionStopping);
            }

            _onSessionStarting = null;
            _onSessionStopping = null;
        }

        public void Tick(in FeatureModuleContext<BattleSessionFeature> ctx, float deltaTime) { }

        public void RebindAll(in FeatureModuleContext<BattleSessionFeature> ctx) { }
    }
}
