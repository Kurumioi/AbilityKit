using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AbilityKit.Ability.World.Abstractions;
using AbilityKit.Core.Utilities;
using AbilityKit.Core.Logging;
using AbilityKit.Game.Battle.Agent;
using AbilityKit.Network.Abstractions;
using AbilityKit.Network.Runtime;

namespace AbilityKit.Game.Flow
{
    internal sealed partial class BattleSessionHandles
    {
        internal sealed class GatewayRoomHandles
        {
            internal IConnection Conn;
            internal IGatewayRoomClient Client;
            internal Task Task;

            internal readonly Dictionary<WorldId, GatewayWorldStartAnchor> WorldStartAnchors = new Dictionary<WorldId, GatewayWorldStartAnchor>();

            internal CancellationTokenSource TimeSyncCts;
            internal Task TimeSyncTask;

            public void Reset()
            {
                TimeSyncTask = null;

                if (TimeSyncCts != null)
                {
                    var cts = TimeSyncCts;
                    TimeSyncCts = null;

                    try
                    {
                        if (!cts.IsCancellationRequested)
                        {
                            cts.Cancel();
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Exception(ex);
                    }

                    DisposeUtils.TryDispose(ref cts, ex => Log.Exception(ex));
                }

                if (Conn != null)
                {
                    IDisposable conn = Conn;
                    Conn = null;
                    DisposeUtils.TryDispose(ref conn, ex => Log.Exception(ex));
                }

                Client = null;
                Task = null;

                WorldStartAnchors.Clear();
            }
        }
    }
}
