using AbilityKit.Demo.Moba.Services;
using AbilityKit.Game.Flow.Battle.Modules;
using AbilityKit.Game.Flow.Modules;

namespace AbilityKit.Game.Flow
{
    public sealed partial class ConfirmedBattleViewFeature : ViewFeatureRuntimeHostBase, IGamePhaseFeature
    {
        private readonly BattleContext _confirmedCtx;

        private readonly System.Collections.Generic.List<IViewSubFeature<ConfirmedBattleViewFeature>> _subFeatures = new System.Collections.Generic.List<IViewSubFeature<ConfirmedBattleViewFeature>>(8);
        private readonly ViewFeatureSubFeatureBuilder _subFeatureBuilder = new ViewFeatureSubFeatureBuilder();
        private readonly ViewSubFeaturePipeline _subFeaturePipeline = new ViewSubFeaturePipeline();
        private ModuleHost<FeatureModuleContext<ConfirmedBattleViewFeature>, IViewSubFeature<ConfirmedBattleViewFeature>> _subFeatureHost;

        public ConfirmedBattleViewFeature(BattleContext confirmedCtx)
        {
            _confirmedCtx = confirmedCtx;
            SetRuntimeQuery(confirmedCtx?.EntityQuery);
        }
    }
}
