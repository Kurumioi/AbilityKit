using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AbilityKit.Demo.Moba.Console.Core.Battle.Context;
using AbilityKit.Game.Battle.Transport.Moba;
using EC = AbilityKit.World.ECS;

namespace AbilityKit.Demo.Moba.Console.Battle.Sync;

/// <summary>
/// 状态同步适配器
/// 连接 Orleans Server，接收状态快照并应用
/// </summary>
public sealed class StateSyncAdapter : IBattleSyncAdapter
{
    private ConsoleBattleContext _context;
    private BattleStartConfig _config;
    private OrleansGatewayClient? _gatewayClient;
    private bool _initialized;
    private bool _connected;
    private int _currentFrame;
    private double _logicTimeSeconds;
    private double _renderTimeSeconds;
    private int _localActorId;

    private string _roomId = string.Empty;
    private string _playerId = string.Empty;
    private string _host = "localhost";
    private int _port = 4000;

    private readonly List<ActorStateSnapshot> _actorStates = new();
    private readonly Dictionary<int, ActorStateSnapshot> _latestActorStates = new();
    private readonly object _statesLock = new();
    private CancellationTokenSource? _syncCts;

    public SyncMode Mode => SyncMode.StateSync;
    public bool IsConnected => _connected && (_gatewayClient?.IsConnected ?? false);
    public int CurrentFrame => _currentFrame;
    public double LogicTimeSeconds => _logicTimeSeconds;
    public double RenderTimeSeconds => _renderTimeSeconds;
    public int LocalActorId => _localActorId;

    public event Action<bool> OnConnectionChanged;
    public event Action<int, double> OnFrameSync;
    public event Action<ActorStateSnapshot[]> OnActorStateSnapshot;

    public void Initialize(ConsoleBattleContext context, BattleStartConfig config)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _initialized = true;
        _connected = false;
        _currentFrame = 0;
        _logicTimeSeconds = 0;
        _renderTimeSeconds = 0;
        _localActorId = _config.Players?.Count > 0
            ? HashPlayerId(_config.Players[0].PlayerId)
            : 1;

        Platform.Log.Sync($"[StateSync] Initialized - Mode: {Mode}, LocalActorId: {_localActorId}");
    }

    public void Connect(string host, int port, string roomId, string playerId)
    {
        if (!_initialized)
            throw new InvalidOperationException("StateSyncAdapter not initialized. Call Initialize first.");

        _host = host;
        _port = port;
        _roomId = roomId;
        _playerId = playerId;

        _gatewayClient = new OrleansGatewayClient();
        _gatewayClient.OnConnected += OnGatewayConnected;
        _gatewayClient.OnDisconnected += OnGatewayDisconnected;
        _gatewayClient.OnServerPush += OnServerPush;
        _gatewayClient.OnError += OnGatewayError;

        Platform.Log.Sync($"[StateSync] Connecting to {host}:{port}...");
        _gatewayClient.Connect(host, port);
    }

    private async void OnGatewayConnected()
    {
        Platform.Log.Sync("[StateSync] Connected to Gateway, logging in...");

        try
        {
            // 1. Guest Login
            var loginPayload = GatewayProtocol.EncodeGuestLoginReq(_playerId);
            var loginResp = await _gatewayClient!.SendRequestAsync(
                GatewayOpCodes.GuestLogin, loginPayload);

            var loginResult = GatewayProtocol.DecodeGuestLoginResp(loginResp);
            if (!loginResult.Success)
            {
                Platform.Log.Sync($"[StateSync] Login failed: {loginResult.Message}");
                return;
            }
            _playerId = loginResult.SessionToken;
            Platform.Log.Sync($"[StateSync] Logged in: {_playerId}");

            // 2. Create or Join Room
            bool roomJoined = false;
            if (!string.IsNullOrEmpty(_roomId))
            {
                // Try to join existing room
                var joinPayload = GatewayProtocol.EncodeJoinRoomReq(_roomId, _roomId);
                var joinResp = await _gatewayClient.SendRequestAsync(
                    GatewayOpCodes.JoinRoom, joinPayload);

                var joinResult = GatewayProtocol.DecodeJoinRoomResp(joinResp);
                if (joinResult.Success)
                {
                    Platform.Log.Sync($"[StateSync] Joined room: {_roomId}");
                    roomJoined = true;
                }
            }

            if (!roomJoined)
            {
                // Create new room
                var createPayload = GatewayProtocol.EncodeCreateRoomReq(_roomId);
                var createResp = await _gatewayClient.SendRequestAsync(
                    GatewayOpCodes.CreateRoom, createPayload);

                var createResult = GatewayProtocol.DecodeCreateRoomResp(createResp);
                if (createResult.Success)
                {
                    _roomId = createResult.RoomId;
                    Platform.Log.Sync($"[StateSync] Created room: {_roomId}");
                    roomJoined = true;
                }
            }

            if (roomJoined)
            {
                _connected = true;
                _syncCts = new CancellationTokenSource();

                // Start sync loop
                _ = SyncLoop(_syncCts.Token);

                OnConnectionChanged?.Invoke(true);
            }
        }
        catch (Exception ex)
        {
            Platform.Log.Sync($"[StateSync] Connection error: {ex.Message}");
            OnConnectionChanged?.Invoke(false);
        }
    }

    private void OnGatewayDisconnected(string reason)
    {
        _connected = false;
        OnConnectionChanged?.Invoke(false);
        Platform.Log.Sync($"[StateSync] Disconnected from Gateway: {reason}");
    }

    private void OnGatewayError(Exception ex)
    {
        Platform.Log.Sync($"[StateSync] Gateway error: {ex.Message}");
    }

    private void OnServerPush(uint opCode, byte[] payload)
    {
        switch (opCode)
        {
            case GatewayOpCodes.FramePushed:
                HandleFramePushed(payload);
                break;

            case GatewayOpCodes.SnapshotPushed:
                HandleSnapshotPushed(payload);
                break;

            default:
                Platform.Log.Sync($"[StateSync] Unknown server push OpCode: {opCode}");
                break;
        }
    }

    private void HandleSnapshotPushed(byte[] payload)
    {
        try
        {
            var notification = ClientStateSyncPayloadCodec.Deserialize(payload);

            lock (_statesLock)
            {
                _currentFrame = notification.Frame;

                foreach (var actor in notification.Actors)
                {
                    var snapshot = new ActorStateSnapshot
                    {
                        ActorId = actor.ActorId,
                        X = actor.X,
                        Y = actor.Y,
                        Z = actor.Z,
                        Rotation = actor.Rotation,
                        VelocityX = actor.VelocityX,
                        VelocityZ = actor.VelocityZ,
                        Hp = actor.Hp,
                        HpMax = actor.HpMax,
                        TeamId = actor.TeamId
                    };

                    _latestActorStates[snapshot.ActorId] = snapshot;
                }

                _actorStates.Clear();
                foreach (var kvp in _latestActorStates)
                {
                    _actorStates.Add(kvp.Value);
                }

                OnActorStateSnapshot?.Invoke(_actorStates.ToArray());
            }

            OnFrameSync?.Invoke(_currentFrame, _logicTimeSeconds);

            Platform.Log.Sync($"[StateSync] Snapshot received - Frame:{notification.Frame}, Actors:{notification.Actors.Count}");
        }
        catch (Exception ex)
        {
            Platform.Log.Sync($"[StateSync] Error handling snapshot pushed: {ex.Message}");
        }
    }

    private void HandleFramePushed(byte[] payload)
    {
        try
        {
            var frameData = GatewayProtocol.DecodeFramePushed(payload);

            lock (_statesLock)
            {
                _currentFrame = frameData.Frame;

                foreach (var input in frameData.Inputs)
                {
                    if (input.OpCode == MobaOpCode.Move && input.Payload != null && input.Payload.Length > 0)
                    {
                        Platform.Log.Sync($"[StateSync] Remote input - Frame:{frameData.Frame} OpCode:{input.OpCode}");
                    }
                }
            }

            OnFrameSync?.Invoke(_currentFrame, _logicTimeSeconds);
        }
        catch (Exception ex)
        {
            Platform.Log.Sync($"[StateSync] Error handling frame pushed: {ex.Message}");
        }
    }

    private async Task SyncLoop(CancellationToken cancellationToken)
    {
        Platform.Log.Sync("[StateSync] Sync loop started");

        try
        {
            while (!cancellationToken.IsCancellationRequested && _connected)
            {
                await Task.Delay(33, cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            Platform.Log.Sync($"[StateSync] Sync loop error: {ex.Message}");
        }

        Platform.Log.Sync("[StateSync] Sync loop ended");
    }

    public void Disconnect()
    {
        _syncCts?.Cancel();
        _syncCts?.Dispose();
        _syncCts = null;

        _gatewayClient?.Dispose();
        _gatewayClient = null;

        _connected = false;
        OnConnectionChanged?.Invoke(false);
        Platform.Log.Sync("[StateSync] Disconnected");
    }

    public void SubmitInput(PlayerInput input)
    {
        if (!_connected || _gatewayClient == null) return;

        var payload = GatewayProtocol.EncodeSubmitFrameInput(
            _roomId,
            worldId: 0,
            frame: _currentFrame,
            playerId: (uint)LocalActorId,
            inputOpCode: (uint)input.OpCode,
            inputPayload: input.Payload);

        _ = _gatewayClient.SendServerPushAsync(GatewayOpCodes.SubmitFrameInput, payload);
    }

    public void Tick(float deltaTime)
    {
        if (!_initialized) return;

        _renderTimeSeconds = _logicTimeSeconds - (1.0 / _config.TickRate);
        _logicTimeSeconds += deltaTime;
    }

    public ActorStateSnapshot[] GetAllActorStates()
    {
        lock (_statesLock)
        {
            _actorStates.Clear();
            foreach (var kvp in _latestActorStates)
            {
                _actorStates.Add(kvp.Value);
            }
            return _actorStates.ToArray();
        }
    }

    public void UpdateActorState(ActorStateSnapshot snapshot)
    {
        lock (_statesLock)
        {
            _latestActorStates[snapshot.ActorId] = snapshot;
        }
    }

    private static int HashPlayerId(string playerId)
    {
        return playerId.GetHashCode() & 0xFFFF;
    }

    public void Dispose()
    {
        Disconnect();
        _initialized = false;
        OnConnectionChanged = null;
        OnFrameSync = null;
        OnActorStateSnapshot = null;
        Platform.Log.Sync("[StateSync] Disposed");
    }
}
