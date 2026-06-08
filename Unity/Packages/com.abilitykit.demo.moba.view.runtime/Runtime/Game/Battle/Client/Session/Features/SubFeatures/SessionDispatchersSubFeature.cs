using AbilityKit.Game.Flow.Modules;

namespace AbilityKit.Game.Flow
{
    internal sealed class SessionDispatchersSubFeature :
        ISessionSubFeature<BattleSessionFeature>,
        IGameModuleId,
        IGameModuleDependencies
    {
        public string Id => "session_dispatchers";

        public System.Collections.Generic.IEnumerable<string> Dependencies => new[] { "session_events" };

        public void OnAttach(in FeatureModuleContext<BattleSessionFeature> ctx)
        {
            if (!BattleSessionFeatureRuntimeAccess.TryGet<ISessionDispatchersRuntime>(ctx, out var runtime)) return;

            runtime.Dispatchers.OnAttach(runtime.Handles);
        }

        public void OnDetach(in FeatureModuleContext<BattleSessionFeature> ctx)
        {
            if (!BattleSessionFeatureRuntimeAccess.TryGet<ISessionDispatchersRuntime>(ctx, out var runtime)) return;

            runtime.Dispatchers.OnDetach(runtime.Handles);
        }

        public void Tick(in FeatureModuleContext<BattleSessionFeature> ctx, float deltaTime) { }

        public void RebindAll(in FeatureModuleContext<BattleSessionFeature> ctx) { }
    }
}
