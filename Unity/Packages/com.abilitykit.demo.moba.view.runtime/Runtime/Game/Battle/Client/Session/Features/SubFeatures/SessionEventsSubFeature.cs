using System;
using AbilityKit.Game.Flow.Battle.Modules;
using AbilityKit.Game.Flow.Modules;

namespace AbilityKit.Game.Flow
{
    internal sealed class SessionEventsSubFeature :
        ISessionSubFeature<BattleSessionFeature>,
        IGameModuleId,
        IGameModuleDependencies
    {
        public string Id => "session_events";

        public System.Collections.Generic.IEnumerable<string> Dependencies => null;

        public void OnAttach(in FeatureModuleContext<BattleSessionFeature> ctx)
        {
            if (!BattleSessionFeatureRuntimeAccess.TryGet<ISessionEventsRuntime>(ctx, out var runtime)) return;

            runtime.Events.OnAttach((ISessionEventsHost)ctx.Feature);
        }

        public void OnDetach(in FeatureModuleContext<BattleSessionFeature> ctx)
        {
            if (!BattleSessionFeatureRuntimeAccess.TryGet<ISessionEventsRuntime>(ctx, out var runtime)) return;

            runtime.Events.OnDetach((ISessionEventsHost)ctx.Feature);
        }

        public void Tick(in FeatureModuleContext<BattleSessionFeature> ctx, float deltaTime) { }

        public void RebindAll(in FeatureModuleContext<BattleSessionFeature> ctx) { }
    }
}
