using AbilityKit.Game.Flow.Battle.Modules;
using System;
using System.Collections.Generic;

namespace AbilityKit.Game.Flow
{
    public sealed partial class BattleSessionFeature
    {
        private interface ISnapshotRoutingModuleHost
        {
            void BuildSnapshotRouting();
            void DisposeSnapshotRouting();
        }

        private sealed class SnapshotRoutingModule : IBattleSessionModule, IBattleSessionModuleId, IBattleSessionModuleDependencies
        {
            private readonly ISnapshotRoutingModuleHost _host;

            private Action _onSessionStarting;
            private Action _onSessionStopping;

            public SnapshotRoutingModule(ISnapshotRoutingModuleHost host)
            {
                _host = host;
            }

            public string Id => "snapshot_routing";

            public IEnumerable<string> Dependencies => null;

            public void OnAttach(in BattleSessionModuleContext ctx)
            {
                _onSessionStarting = () => _host?.BuildSnapshotRouting();
                _onSessionStopping = () => _host?.DisposeSnapshotRouting();

                ctx.Hooks?.SessionStarting.Add(_onSessionStarting);
                ctx.Hooks?.SessionStopping.Add(_onSessionStopping);
            }

            public void OnDetach(in BattleSessionModuleContext ctx)
            {
                if (_onSessionStarting != null)
                {
                    ctx.Hooks?.SessionStarting.Remove(_onSessionStarting);
                }
                if (_onSessionStopping != null)
                {
                    ctx.Hooks?.SessionStopping.Remove(_onSessionStopping);
                }

                _onSessionStarting = null;
                _onSessionStopping = null;
            }

            public void Tick(in BattleSessionModuleContext ctx, float deltaTime)
            {
            }
        }
    }
}
