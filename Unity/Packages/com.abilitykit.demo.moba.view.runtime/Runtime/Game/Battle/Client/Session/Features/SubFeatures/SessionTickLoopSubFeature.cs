using AbilityKit.Game.Flow.Modules;

namespace AbilityKit.Game.Flow
{
    internal sealed class SessionTickLoopSubFeature :
        ISessionSubFeature<BattleSessionFeature>,
        ISessionMainTickSubFeature<BattleSessionFeature>,
        IGameModuleId,
        IGameModuleDependencies
    {
        public string Id => "session_tick_loop";

        public System.Collections.Generic.IEnumerable<string> Dependencies => new[] { "session_events" };

        public void OnAttach(in FeatureModuleContext<BattleSessionFeature> ctx) { }

        public void OnDetach(in FeatureModuleContext<BattleSessionFeature> ctx) { }

        public void MainTick(in FeatureModuleContext<BattleSessionFeature> ctx, float deltaTime)
        {
            if (!BattleSessionFeatureRuntimeAccess.TryGet<ISessionTickLoopRuntime>(ctx, out var runtime)) return;

            runtime.TickLoop.MainTick(deltaTime);
        }

        public void Tick(in FeatureModuleContext<BattleSessionFeature> ctx, float deltaTime) { }

        public void RebindAll(in FeatureModuleContext<BattleSessionFeature> ctx) { }
    }
}
