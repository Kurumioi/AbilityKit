using System.Collections.Generic;
using AbilityKit.Game.Flow.Modules;

namespace AbilityKit.Game.Flow
{
    internal sealed partial class BattleSessionHandles
    {
        internal sealed class PhaseHandles
        {
            internal GamePhaseContext PhaseCtx;
            internal BattleContext Ctx;
            internal AbilityKit.World.ECS.IEntity Root;

            internal List<ISessionSubFeature<BattleSessionFeature>> SubFeatures;
            internal ModuleHost<FeatureModuleContext<BattleSessionFeature>, ISessionSubFeature<BattleSessionFeature>> SubFeatureHost;

            internal GameFlowDomain Flow;

            public void Reset()
            {
                PhaseCtx = default;
                Ctx = null;
                Root = default;
                SubFeatures = null;
                SubFeatureHost = null;
                Flow = null;
            }
        }
    }
}
