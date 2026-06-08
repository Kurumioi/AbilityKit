using AbilityKit.Game.Flow.Battle.Modules;
using AbilityKit.Game.Flow.Modules;

namespace AbilityKit.Game.Flow
{
    internal sealed class SessionLifecycleSubFeature :
        ISessionSubFeature<BattleSessionFeature>,
        ISessionLifecycleNotifySubFeature<BattleSessionFeature>,
        IGameModuleId,
        IGameModuleDependencies
    {
        public string Id => "session_lifecycle";

        public System.Collections.Generic.IEnumerable<string> Dependencies => new[] { "session_events" };

        public void NotifySessionStarting(in FeatureModuleContext<BattleSessionFeature> ctx)
        {
            if (!BattleSessionFeatureRuntimeAccess.TryGet<ISessionLifecycleRuntime>(ctx, out var runtime)) return;

            runtime.Hooks?.SessionStarting.Invoke();
        }

        public void NotifySessionStopping(in FeatureModuleContext<BattleSessionFeature> ctx)
        {
            if (!BattleSessionFeatureRuntimeAccess.TryGet<ISessionLifecycleRuntime>(ctx, out var runtime)) return;

            runtime.Hooks?.SessionStopping.Invoke();
        }

        public void OnAttach(in FeatureModuleContext<BattleSessionFeature> ctx) { }

        public void OnDetach(in FeatureModuleContext<BattleSessionFeature> ctx) { }

        public void Tick(in FeatureModuleContext<BattleSessionFeature> ctx, float deltaTime) { }

        public void RebindAll(in FeatureModuleContext<BattleSessionFeature> ctx) { }
    }
}
