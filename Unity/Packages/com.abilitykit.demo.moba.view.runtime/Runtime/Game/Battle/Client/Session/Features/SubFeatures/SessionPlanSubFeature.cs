using System;
using AbilityKit.Core.Logging;
using AbilityKit.Game.Flow.Battle.Modules;
using AbilityKit.Game.Flow.Modules;

namespace AbilityKit.Game.Flow
{
    internal sealed class SessionPlanSubFeature :
        ISessionSubFeature<BattleSessionFeature>,
        IGameModuleId,
        IGameModuleDependencies
    {
        public string Id => "session_plan";

        public System.Collections.Generic.IEnumerable<string> Dependencies => new[] { "session_events" };

        public void OnAttach(in FeatureModuleContext<BattleSessionFeature> ctx)
        {
            if (!BattleSessionFeatureRuntimeAccess.TryGet<ISessionPlanRuntime>(ctx, out var runtime)) return;

            runtime.PlanController.OnAttach(
                host: (ISessionPlanHost)ctx.Feature,
                bootstrapper: runtime.Bootstrapper,
                state: runtime.State,
                handles: runtime.Handles,
                hooks: runtime.Hooks,
                ctx: runtime.Context);
        }

        public void OnDetach(in FeatureModuleContext<BattleSessionFeature> ctx)
        {
        }

        public void Tick(in FeatureModuleContext<BattleSessionFeature> ctx, float deltaTime) { }

        public void RebindAll(in FeatureModuleContext<BattleSessionFeature> ctx) { }
    }
}
