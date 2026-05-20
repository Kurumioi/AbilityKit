using System;
using System.Collections.Generic;
using AbilityKit.Core.Common.Log;
using AbilityKit.Game.Flow.Battle.Modules;

namespace AbilityKit.Game.Flow
{
    public sealed partial class BattleSessionFeature
    {
        private interface IGatewayRoomModuleHost
        {
            bool ShouldPrepareGatewayRoom();
            void StartGatewayRoomPreparation();
            void StopGatewayRoomPreparation();

            bool HasGatewayRoomConnection { get; }
            void TickGatewayRoomConnection(float deltaTime);
            System.Threading.Tasks.Task GatewayRoomTask { get; }
        }

        private sealed class GatewayRoomModule : IBattleSessionModule, IBattleSessionModuleId, IBattleSessionModuleDependencies
        {
            private readonly IGatewayRoomModuleHost _host;
            private readonly SessionEventsController _sessionEventsController;

            private Func<BattleStartPlan, bool> _planBuiltHandler;
            private bool _sessionRequested;

            public GatewayRoomModule(IGatewayRoomModuleHost host, SessionEventsController sessionEventsController)
            {
                _host = host;
                _sessionEventsController = sessionEventsController;
            }

            public string Id => "gateway_room";

            public IEnumerable<string> Dependencies => null;

            public void OnAttach(in BattleSessionModuleContext ctx)
            {
                _planBuiltHandler = plan =>
                {
                    if (_host == null || !_host.ShouldPrepareGatewayRoom()) return false;
                    _host.StartGatewayRoomPreparation();
                    return false;
                };

                ctx.Hooks?.PlanBuilt.Add(_planBuiltHandler);
            }

            public void OnDetach(in BattleSessionModuleContext ctx)
            {
                if (_planBuiltHandler != null)
                {
                    ctx.Hooks?.PlanBuilt.Remove(_planBuiltHandler);
                }
                _planBuiltHandler = null;

                _host?.StopGatewayRoomPreparation();
            }

            public void Tick(in BattleSessionModuleContext ctx, float deltaTime)
            {
            }

            public void PreTick(in BattleSessionModuleContext ctx, float deltaTime)
            {
                if (_host == null || !_host.HasGatewayRoomConnection) return;

                _host.TickGatewayRoomConnection(deltaTime);

                var task = _host.GatewayRoomTask;
                if (task == null || !task.IsCompleted) return;

                if (task.IsFaulted)
                {
                    var ex = task.Exception != null ? task.Exception.GetBaseException() : null;
                    var wrapped = new InvalidOperationException("Gateway room preparation failed.", ex);
                    Log.Exception(wrapped, "[BattleSessionFeature] Gateway room preparation failed");
                    _host.StopGatewayRoomPreparation();
                    _sessionEventsController?.NotifySessionFailed(ctx.Host as ISessionEventsHost, wrapped);
                    return;
                }

                _host.StopGatewayRoomPreparation();

                if (!_sessionRequested)
                {
                    _sessionRequested = true;
                    _sessionEventsController?.RequestStartSession(ctx.Host as ISessionEventsHost);
                }
            }
        }
    }
}
