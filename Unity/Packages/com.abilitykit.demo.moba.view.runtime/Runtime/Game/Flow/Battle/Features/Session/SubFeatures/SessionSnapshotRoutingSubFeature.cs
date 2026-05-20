using System;
using AbilityKit.Game.Flow.Battle.Modules;
using AbilityKit.Game.Flow.Modules;

namespace AbilityKit.Game.Flow
{
    public sealed partial class BattleSessionFeature
    {
        private sealed class SessionSnapshotRoutingSubFeature :
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
                var f = ctx.Feature;
                if (f == null) return;

                _onSessionStarting = () => f.EnsureSnapshotRoutingBuilt();
                _onSessionStopping = () => f.DisposeSnapshotRoutingIfAny();

                f.Hooks?.SessionStarting.Add(_onSessionStarting);
                f.Hooks?.SessionStopping.Add(_onSessionStopping);
            }

            public void OnDetach(in FeatureModuleContext<BattleSessionFeature> ctx)
            {
                var f = ctx.Feature;
                if (_onSessionStarting != null && f != null)
                {
                    f.Hooks?.SessionStarting.Remove(_onSessionStarting);
                }
                if (_onSessionStopping != null && f != null)
                {
                    f.Hooks?.SessionStopping.Remove(_onSessionStopping);
                }

                _onSessionStarting = null;
                _onSessionStopping = null;
            }

            public void Tick(in FeatureModuleContext<BattleSessionFeature> ctx, float deltaTime) { }

            public void RebindAll(in FeatureModuleContext<BattleSessionFeature> ctx) { }
        }
    }
}
