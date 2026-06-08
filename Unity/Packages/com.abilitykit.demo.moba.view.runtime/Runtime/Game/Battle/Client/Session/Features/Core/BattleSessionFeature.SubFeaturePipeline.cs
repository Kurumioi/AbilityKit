using AbilityKit.Game.Flow.Modules;

namespace AbilityKit.Game.Flow
{
    public sealed partial class BattleSessionFeature
    {
        private void InvokeSubFeaturesPreTick(in GamePhaseContext ctx, float deltaTime)
        {
            if (_subFeatureHost == null) return;
            var fctx = new FeatureModuleContext<BattleSessionFeature>(ctx, this);
            _subFeatureHost.ForEach<ISessionPreTickSubFeature<BattleSessionFeature>>(m => m.PreTick(fctx, deltaTime));
        }

        private bool InvokeSubFeaturesPlanBuilt()
        {
            if (_subFeatureHost == null) return false;
            var fctx = new FeatureModuleContext<BattleSessionFeature>(_phaseCtx, this);
            var handled = false;
            _subFeatureHost.ForEach<ISessionPlanSubFeature<BattleSessionFeature>>(m =>
            {
                if (!handled && m.OnPlanBuilt(fctx)) handled = true;
            });
            return handled;
        }

        private void InvokeSessionStartingPipeline()
        {
            if (_subFeatureHost == null) return;
            var fctx = new FeatureModuleContext<BattleSessionFeature>(_phaseCtx, this);
            _subFeatureHost.ForEach<ISessionLifecycleNotifySubFeature<BattleSessionFeature>>(m => m.NotifySessionStarting(fctx));
            _subFeatureHost.ForEach<ISessionLifecycleSubFeature<BattleSessionFeature>>(m => m.OnSessionStarting(fctx));
        }

        private void InvokeSessionStoppingPipeline()
        {
            if (_subFeatureHost == null) return;
            var fctx = new FeatureModuleContext<BattleSessionFeature>(_phaseCtx, this);
            _subFeatureHost.ForEach<ISessionLifecycleNotifySubFeature<BattleSessionFeature>>(m => m.NotifySessionStopping(fctx));
            _subFeatureHost.ForEachReverse<ISessionLifecycleSubFeature<BattleSessionFeature>>(m => m.OnSessionStopping(fctx));
        }

        private void InvokeReplaySetupPipeline()
        {
            if (_subFeatureHost == null) return;
            var fctx = new FeatureModuleContext<BattleSessionFeature>(_phaseCtx, this);
            _subFeatureHost.ForEach<ISessionReplaySetupSubFeature<BattleSessionFeature>>(m => m.SetupReplayOrRecord(fctx));
        }
    }
}
