using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AbilityKit.Ability.World.Abstractions;
using AbilityKit.Game.Battle.Agent;
using AbilityKit.Network.Abstractions;
using AbilityKit.Network.Runtime;

namespace AbilityKit.Game.Flow
{
    public sealed partial class BattleSessionFeature
    {
        private BattleSessionNetAdapter _netAdapter
        {
            get => _handles.Net.Adapter;
            set => _handles.Net.Adapter = value;
        }

        private IBattleSessionNetAdapterContext _netAdapterCtx
        {
            get => _handles.Net.Ctx;
            set => _handles.Net.Ctx = value;
        }

        private IConnection _gatewayConn
        {
            get => _handles.GatewayRoom.Conn;
            set => _handles.GatewayRoom.Conn = value;
        }

        private GatewayRoomClient _gatewayClient
        {
            get => _handles.GatewayRoom.Client;
            set => _handles.GatewayRoom.Client = value;
        }

        private Task _gatewayTask
        {
            get => _handles.GatewayRoom.Task;
            set => _handles.GatewayRoom.Task = value;
        }

        private CancellationTokenSource _gatewayTimeSyncCts
        {
            get => _handles.GatewayRoom.TimeSyncCts;
            set => _handles.GatewayRoom.TimeSyncCts = value;
        }

        private Task _gatewayTimeSyncTask
        {
            get => _handles.GatewayRoom.TimeSyncTask;
            set => _handles.GatewayRoom.TimeSyncTask = value;
        }

        private Dictionary<WorldId, GatewayWorldStartAnchor> _gatewayWorldStartAnchors => _handles.GatewayRoom.WorldStartAnchors;

        private IDispatcher _unityDispatcher
        {
            get => _handles.Dispatchers.UnityDispatcher;
            set => _handles.Dispatchers.UnityDispatcher = value;
        }

        private DedicatedThreadDispatcher _networkIoDispatcher
        {
            get => _handles.Dispatchers.NetworkIoDispatcher;
            set => _handles.Dispatchers.NetworkIoDispatcher = value;
        }
    }
}
