using AbilityKit.Game.Flow.Battle.Modules;
using AbilityKit.Game.Flow.Modules;
using UnityEngine;

namespace AbilityKit.Game.Flow
{
    public sealed partial class ConfirmedBattleViewFeature
    {
        public void OnAttach(in GamePhaseContext ctx)
        {
            BindPresentationSession(ctx);
            EnsureSubFeaturesCreated();
            _subFeatureHost?.Attach(new FeatureModuleContext<ConfirmedBattleViewFeature>(ctx, this));
            OnAllSubFeaturesAttached(ctx);
        }

        private void OnAllSubFeaturesAttached(in GamePhaseContext ctx)
        {
            if (_confirmedCtx == null) return;
            var worldId = _confirmedCtx.RuntimeWorldId;
            _confirmedCtx?.Hooks?.ViewBinderReady.Invoke(new ViewBinderReadyEvent(isConfirmed: true, worldId: worldId));
        }

        public void OnDetach(in GamePhaseContext ctx)
        {
            _subFeatureHost?.Detach(new FeatureModuleContext<ConfirmedBattleViewFeature>(ctx, this));
            ClearPresentationSession(ctx);
        }

        public void Tick(in GamePhaseContext ctx, float deltaTime)
        {
            if (_confirmedCtx?.EntityWorld == null) return;
            _subFeatureHost?.Tick(new FeatureModuleContext<ConfirmedBattleViewFeature>(ctx, this), deltaTime);
        }

        public void RebindAll()
        {
            if (_confirmedCtx?.EntityWorld == null) return;
            _subFeatureHost?.RebindAll(new FeatureModuleContext<ConfirmedBattleViewFeature>(default, this));

            var frame = _confirmedCtx != null ? _confirmedCtx.LastFrame : 0;
            var worldId = _confirmedCtx != null ? _confirmedCtx.RuntimeWorldId : default;
            _confirmedCtx?.Hooks?.ViewsRebound.Invoke(new ViewsReboundEvent(isConfirmed: true, worldId: worldId, frame: frame));
        }

        private void EnsureSubFeaturesCreated()
        {
            if (_subFeatureHost != null && _subFeatures.Count > 0) return;

            _subFeatures.Clear();
            _subFeatureBuilder.AddConfirmedViewSubFeatures(_subFeatures);

            _subFeaturePipeline.AddStandardViewSubFeatures(_subFeatures);

            _subFeatureHost = _subFeaturePipeline.CreateHost(_subFeatures);
        }
    }
}
