using System;
using AbilityKit.Core.Common.Log;
using AbilityKit.Game.Flow.Battle.Modules;
using AbilityKit.Game.Flow.Modules;

namespace AbilityKit.Game.Flow
{
    public sealed partial class BattleSessionFeature
    {
        private sealed class SessionGatewayRoomSubFeature :
            ISessionSubFeature<BattleSessionFeature>,
            ISessionPreTickSubFeature<BattleSessionFeature>,
            IGameModuleId,
            IGameModuleDependencies
        {
            private Func<BattleStartPlan, bool> _planBuiltHandler;
            private bool _sessionRequested;

            public string Id => "gateway_room";

            public System.Collections.Generic.IEnumerable<string> Dependencies => new[] { "session_events" };

            public void OnAttach(in FeatureModuleContext<BattleSessionFeature> ctx)
            {
                var f = ctx.Feature;
                if (f == null) return;

                _planBuiltHandler = plan =>
                {
                    if (!f.ShouldPrepareGatewayRoom()) return false;
                    f.StartGatewayRoomPreparation();
                    return false;
                };

                f.Hooks?.PlanBuilt.Add(_planBuiltHandler);
            }

            public void OnDetach(in FeatureModuleContext<BattleSessionFeature> ctx)
            {
                var f = ctx.Feature;
                if (_planBuiltHandler != null && f != null)
                {
                    f.Hooks?.PlanBuilt.Remove(_planBuiltHandler);
                }
                _planBuiltHandler = null;

                f?.StopGatewayRoomPreparation();
            }

            public void PreTick(in FeatureModuleContext<BattleSessionFeature> ctx, float deltaTime)
            {
                var f = ctx.Feature;
                if (f == null) return;
                if (!f.HasGatewayRoomConnection) return;

                f.TickGatewayRoomConnection(deltaTime);

                var task = f.GatewayRoomPreparationTask;
                if (task == null || !task.IsCompleted) return;

                if (task.IsFaulted)
                {
                    var ex = task.Exception != null ? task.Exception.GetBaseException() : null;
                    var wrapped = new InvalidOperationException("Gateway room preparation failed.", ex);
                    Log.Exception(wrapped, "[BattleSessionFeature] Gateway room preparation failed");
                    f.StopGatewayRoomPreparation();
                    f.Hooks?.SessionFailed.Invoke(wrapped);
                    return;
                }

                f.StopGatewayRoomPreparation();

                if (!_sessionRequested)
                {
                    _sessionRequested = true;
                    f.Hooks?.SessionStarting.Invoke();
                    f.OnStartSessionRequested();
                }
            }

            public void Tick(in FeatureModuleContext<BattleSessionFeature> ctx, float deltaTime) { }

            public void RebindAll(in FeatureModuleContext<BattleSessionFeature> ctx) { }
        }
    }
}
