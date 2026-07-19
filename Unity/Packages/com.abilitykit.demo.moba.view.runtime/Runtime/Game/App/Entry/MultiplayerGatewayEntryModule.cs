using System;
using System.Threading;
using System.Threading.Tasks;
using AbilityKit.Core.Logging;
using AbilityKit.Game.Battle.Agent;
using AbilityKit.World.ECS;
using AbilityKit.Game.Flow;
using AbilityKit.Game.View.Modules;
using AbilityKit.Network.Abstractions;
using AbilityKit.Network.Protocol;
using AbilityKit.Network.Runtime;

namespace AbilityKit.Game
{
    public interface IMultiplayerGatewayRuntime
    {
        bool IsRemoteActive { get; }
        ConnectionState ConnectionState { get; }
    }

    public sealed class MultiplayerGatewayEntryModule :
        IGameEntryModule,
        IGameModuleTick<GameEntryModuleContext>,
        IMultiplayerGatewayRuntime
    {
        private readonly BattleGatewayConfigSO _config;
        private IConnection _connection;
        private DedicatedThreadDispatcher _ioDispatcher;
        private CancellationTokenSource _lifetime;
        private ClientRoomStore _store;
        private GatewayRoomClient _client;
        private GatewayMultiplayerRoomSession _session;
        private ClientRoomSnapshotProvider _snapshotProvider;
        private MultiplayerRoomFlowController _controller;
        private ClientRoomPushSynchronizer _pushSynchronizer;
        private LobbyBattleEntrySelection _selection;

        public bool IsRemoteActive => _selection?.IsRemoteSelected == true;
        public ConnectionState ConnectionState =>
            _connection != null ? _connection.State : ConnectionState.Disconnected;

        public MultiplayerGatewayEntryModule(BattleGatewayConfigSO config)
        {
            _config = config;
        }

        public string Id => "game.entry.multiplayer-gateway";

        public void OnAttach(in GameEntryModuleContext ctx)
        {
            if (_config == null)
            {
                return;
            }

            ValidateConfig(_config);

            _lifetime = new CancellationTokenSource();
            _ioDispatcher = new DedicatedThreadDispatcher("LobbyGatewayNetworkThread");
            var callbackDispatcher = UnityMainThreadDispatcher.CaptureCurrent();
            var options = new ConnectionOptions
            {
                FrameCodec = LengthPrefixedFrameCodec.Instance,
                KickPushOpCode = 9000
            };

            _connection = new ConnectionManager(
                () => new TcpTransport(),
                options,
                callbackDispatcher,
                _ioDispatcher);
            _store = new ClientRoomStore();
            _client = new GatewayRoomClient(
                _connection,
                new GatewayRoomOpCodes(_config.CreateRoomOpCode, _config.JoinRoomOpCode));
            _session = new GatewayMultiplayerRoomSession(_client, _store);
            _snapshotProvider = new ClientRoomSnapshotProvider(_store);
            _controller = new MultiplayerRoomFlowController(_session, _snapshotProvider);
            _pushSynchronizer = new ClientRoomPushSynchronizer(
                _client,
                _store,
                RefreshCurrentRoomAsync);

            _connection.ServerPushReceived += HandleServerPush;
            ctx.Root.TryGetRef(out _selection);
            if (_selection != null)
            {
                _selection.Changed += HandleEntrySelectionChanged;
            }

            ctx.Root.WithRef(_config);
            ctx.Root.WithRef(_store);
            ctx.Root.WithRef<IGatewayRoomClient>(_client);
            ctx.Root.WithRef<IMultiplayerRoomSession>(_session);
            ctx.Root.WithRef(_session);
            ctx.Root.WithRef<IRoomSnapshotProvider>(_snapshotProvider);
            ctx.Root.WithRef(_controller);
            ctx.Root.WithRef<IMultiplayerGatewayRuntime>(this);
            ApplyEntrySelection();
        }

        public void Tick(in GameEntryModuleContext ctx, float deltaTime)
        {
            _connection?.Tick(deltaTime);
        }

        public void OnDetach(in GameEntryModuleContext ctx)
        {
            _lifetime?.Cancel();
            if (_selection != null)
            {
                _selection.Changed -= HandleEntrySelectionChanged;
            }

            if (_connection != null)
            {
                _connection.ServerPushReceived -= HandleServerPush;
            }

            if (ctx.Root.IsValid)
            {
                ctx.Root.RemoveComponent(typeof(IMultiplayerGatewayRuntime));
                ctx.Root.RemoveComponent(typeof(MultiplayerRoomFlowController));
                ctx.Root.RemoveComponent(typeof(IRoomSnapshotProvider));
                ctx.Root.RemoveComponent(typeof(GatewayMultiplayerRoomSession));
                ctx.Root.RemoveComponent(typeof(IMultiplayerRoomSession));
                ctx.Root.RemoveComponent(typeof(IGatewayRoomClient));
                ctx.Root.RemoveComponent(typeof(ClientRoomStore));
                ctx.Root.RemoveComponent(typeof(BattleGatewayConfigSO));
            }

            _controller?.Dispose();
            _snapshotProvider?.Dispose();
            _connection?.Dispose();
            _ioDispatcher?.Dispose();
            _lifetime?.Dispose();

            _pushSynchronizer = null;
            _selection = null;
            _controller = null;
            _snapshotProvider = null;
            _session = null;
            _client = null;
            _store = null;
            _connection = null;
            _ioDispatcher = null;
            _lifetime = null;
        }

        private void HandleEntrySelectionChanged()
        {
            ApplyEntrySelection();
        }

        private void ApplyEntrySelection()
        {
            if (_connection == null)
            {
                return;
            }

            if (IsRemoteActive)
            {
                if (_connection.State == ConnectionState.Disconnected)
                {
                    _connection.Open(_config.Host, _config.Port);
                }

                return;
            }

            _controller?.Cancel();
            _store?.Reset();
            if (_connection.State != ConnectionState.Disconnected)
            {
                _connection.Close();
            }
        }

        private async void HandleServerPush(uint opCode, ArraySegment<byte> payload)
        {
            var synchronizer = _pushSynchronizer;
            var lifetime = _lifetime;
            if (synchronizer == null || lifetime == null)
            {
                return;
            }

            try
            {
                await synchronizer.HandleServerPushAsync(
                    opCode,
                    payload,
                    lifetime.Token);
            }
            catch (OperationCanceledException) when (lifetime.IsCancellationRequested)
            {
            }
            catch (Exception ex)
            {
                Log.Exception(ex, "[MultiplayerGatewayEntryModule] Failed to process Room push.");
            }
        }

        private Task RefreshCurrentRoomAsync(CancellationToken cancellationToken)
        {
            var roomId = _store?.Current?.RoomId;
            if (string.IsNullOrWhiteSpace(roomId))
            {
                throw new InvalidOperationException("Cannot refresh Room snapshot before joining a room.");
            }

            return _session.RefreshSnapshotAsync(roomId, cancellationToken);
        }

        private static void ValidateConfig(BattleGatewayConfigSO config)
        {
            if (string.IsNullOrWhiteSpace(config.Host))
            {
                throw new InvalidOperationException("Lobby Gateway Host is required.");
            }

            if (config.Port <= 0 || config.Port > 65535)
            {
                throw new InvalidOperationException("Lobby Gateway Port must be between 1 and 65535.");
            }
        }
    }
}
