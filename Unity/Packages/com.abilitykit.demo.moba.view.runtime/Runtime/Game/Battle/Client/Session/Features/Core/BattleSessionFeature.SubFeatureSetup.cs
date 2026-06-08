using System;
using System.Collections.Generic;
using AbilityKit.Core.Common.Log;
using AbilityKit.Game.Battle;
using AbilityKit.Game.Flow.Battle.Modules;
using AbilityKit.Game.Flow.Modules;

namespace AbilityKit.Game.Flow
{
    public sealed partial class BattleSessionFeature
    {
        private void EnsureSubFeaturesCreated()
        {
            _subFeatures ??= new List<ISessionSubFeature<BattleSessionFeature>>(capacity: 8);
            if (_subFeatureHost != null && _subFeatures.Count > 0) return;

            void Fail(string message)
            {
                Log.Error($"[BattleSessionFeature] SubFeature dependency validation failed: {message}");

                var ex = new InvalidOperationException(message);
                if (_eventsCtrl != null)
                {
                    _eventsCtrl.NotifySessionFailed(this, ex);
                }
                else
                {
                    _pendingSubFeatureValidationFailure = ex;
                }
            }

            _subFeatureHost = SessionSubFeaturePipeline.CreateHost(_subFeatures, Fail);

            if (_subFeatures.Count == 0)
            {
                SessionSubFeaturePipeline.AddStandardSessionSubFeatures(_subFeatures);
                SessionSubFeaturePipeline.AddLateSessionSubFeatures(_subFeatures);

                if (!_subFeatureHost.TrySortByDependencies())
                {
                    return;
                }
            }
        }

    }
}
