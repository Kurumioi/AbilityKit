using AbilityKit.Game.Flow.Battle.Modules;
using AbilityKit.Game.Flow.Modules;
using AbilityKit.World.ECS;
using UnityEngine;

namespace AbilityKit.Game.Flow
{
    public sealed partial class BattleViewFeature
    {
        public void OnAttach(in GamePhaseContext ctx)
        {
            ctx.Root.TryGetRef(out _ctx);
            SetRuntimeQuery(_ctx?.EntityQuery);
            BindPresentationSession(ctx);

            EnsureSubFeaturesCreated();
            _subFeatureHost?.Attach(new FeatureModuleContext<BattleViewFeature>(ctx, this));
            OnAllSubFeaturesAttached(ctx);
        }

        private void OnAllSubFeaturesAttached(in GamePhaseContext ctx)
        {
            if (_ctx == null) return;
            var worldId = _ctx.RuntimeWorldId;
            _ctx?.Hooks?.ViewBinderReady.Invoke(new ViewBinderReadyEvent(isConfirmed: false, worldId: worldId));
        }

        public void OnDetach(in GamePhaseContext ctx)
        {
            _subFeatureHost?.Detach(new FeatureModuleContext<BattleViewFeature>(ctx, this));

            _ctx = null;
            ClearPresentationSession(ctx);
        }

        public void Tick(in GamePhaseContext ctx, float deltaTime)
        {
            if (_ctx?.EntityWorld == null) return;
            _subFeatureHost?.Tick(new FeatureModuleContext<BattleViewFeature>(ctx, this), deltaTime);
        }

        public void RebindAll()
        {
            if (_ctx?.EntityWorld == null) return;
            _subFeatureHost?.RebindAll(new FeatureModuleContext<BattleViewFeature>(default, this));

            var frame = _ctx != null ? _ctx.LastFrame : 0;
            var worldId = _ctx != null ? _ctx.RuntimeWorldId : default;
            _ctx?.Hooks?.ViewsRebound.Invoke(new ViewsReboundEvent(isConfirmed: false, worldId: worldId, frame: frame));
        }

        private void EnsureSubFeaturesCreated()
        {
            if (_subFeatureHost != null && _subFeatures.Count > 0) return;

            _subFeatures.Clear();
            _subFeatureBuilder.AddBattleViewSubFeatures(_subFeatures);

            _subFeaturePipeline.AddStandardViewSubFeatures(_subFeatures);

            _subFeatureHost = _subFeaturePipeline.CreateHost(_subFeatures);
        }
    }
}
