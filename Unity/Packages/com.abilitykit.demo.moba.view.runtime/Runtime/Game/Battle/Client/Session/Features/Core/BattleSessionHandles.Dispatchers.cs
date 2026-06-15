using System;
using AbilityKit.Ability.Host;
using AbilityKit.Core.Logging;
using AbilityKit.Network.Abstractions;
using AbilityKit.Game.Flow.Battle.Replay;

namespace AbilityKit.Game.Flow
{
    internal sealed partial class BattleSessionHandles
    {
        internal sealed class DispatcherHandles
        {
            internal IDispatcher UnityDispatcher;
            internal DedicatedThreadDispatcher NetworkIoDispatcher;

            public void Reset()
            {
                UnityDispatcher = null;
                DisposeNetworkIoDispatcher();
            }

            public void DisposeNetworkIoDispatcher()
            {
                AbilityKit.Core.Utilities.DisposeUtils.TryDispose(ref NetworkIoDispatcher, ex => Log.Exception(ex));
            }
        }

        internal sealed class ReplayHandles
        {
            internal LockstepReplayDriver Driver;

            public void Reset()
            {
                Driver = null;
            }
        }
    }
}
