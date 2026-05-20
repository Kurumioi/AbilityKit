using System;
using System.Collections.Generic;
using AbilityKit.Demo.Moba.Console.Core.Battle.Context;
using AbilityKit.Demo.Moba.Share;
using AbilityKit.Game.Battle.Transport.Moba;
using AbilityKit.Protocol.Moba.FrameSync;
using EC = AbilityKit.World.ECS;

namespace AbilityKit.Demo.Moba.Console.Battle.Sync;

/// <summary>
/// 状态同步适配器
/// 连接网络服务器，接收状态快照并应用
/// </summary>
public sealed class StateSyncAdapter : IBattleSyncAdapter
{
    private ConsoleBattleContext _context;
    private BattleStartConfig _config;
    private TcpNetworkClient? _gatewayClient;
    private bool _initialized;
    private bool _connected;
    private int _currentFrame;
    private double _logicTimeSeconds;
    private double _renderTimeSeconds;
    private int _localActorId;

    private string _roomId = string.Empty;
    private ulong _numericRoomId;
    private string _playerId = string.Empty;

    private readonly List<ActorStateSnapshot> _actorStates = new();
    private readonly Dictionary<int, ActorStateSnapshot> _latestActorStates = new();
    private readonly object _statesLock = new();

    private NetworkConfig _networkConfig = new();

    public SyncMode Mode => SyncMode.SnapshotAuthority;
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
        _networkConfig = config.Network ?? new NetworkConfig();
        _localActorId = config.Players?.Count > 0
            ? DeterministicHash.StringToActorId(_config.Players[0].PlayerId)
            : 1;

        Platform.Log.Sync($"[StateSync] Initialized - Mode: {Mode}, LocalActorId: {_localActorId}");
    }

    public void Connect(string host, int port, string roomId, string playerId)
    {
        if (!_initialized)
            throw new InvalidOperationException("StateSyncAdapter not initialized. Call Initialize first.");

        _roomId = roomId;
        _playerId = playerId;

        _gatewayClient = new TcpNetworkClient();
        _gatewayClient.OnConnected += OnGatewayConnected;
        _gatewayClient.OnDisconnected += OnGatewayDisconnected;
        _gatewayClient.OnServerPush += OnServerPush;
        _gatewayClient.OnError += OnGatewayError;

        Platform.Log.Sync($"[StateSync] Connecting to {host}:{port}...");
        _gatewayClient.Connect(host, port);
    }

    /// <summary>
    /// 使用配置连接
    /// </summary>
    public void Connect()
    {
        Connect(_networkConfig.Host, _networkConfig.Port, _roomId, _playerId);
    }

    private async void OnGatewayConnected()
    {
        Platform.Log.Sync("[StateSync] Connected to server, logging in...");

        try
        {
            // 1. Guest Login
            var loginPayload = NetworkProtocol.EncodeGuestLoginReq(_playerId);
            var loginResp = await _gatewayClient!.SendRequestAsync(
                NetworkOpCodes.GuestLogin, loginPayload);

            var loginResult = NetworkProtocol.DecodeGuestLoginResp(loginResp);
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
                var joinPayload = NetworkProtocol.EncodeJoinRoomReq(_playerId, roomId: _roomId);
                var joinResp = await _gatewayClient.SendRequestAsync(
                    NetworkOpCodes.JoinRoom, joinPayload);

                var joinResult = NetworkProtocol.DecodeJoinRoomResp(joinResp);
                if (joinResult.Success)
                {
                    _numericRoomId = joinResult.NumericRoomId;
                    Platform.Log.Sync($"[StateSync] Joined room: {_roomId}, NumericId: {_numericRoomId}");
                    roomJoined = true;
                }
            }

            if (!roomJoined)
            {
                var createPayload = NetworkProtocol.EncodeCreateRoomReq(
                    _playerId,
                    title: _roomId);
                var createResp = await _gatewayClient.SendRequestAsync(
                    NetworkOpCodes.CreateRoom, createPayload);

                var createResult = NetworkProtocol.DecodeCreateRoomResp(createResp);
                if (createResult.Success)
                {
                    _roomId = createResult.RoomId;
                    Platform.Log.Sync($"[StateSync] Created room: {_roomId}");

                    // Join the created room to get numeric ID
                    var rejoinPayload = NetworkProtocol.EncodeJoinRoomReq(_playerId, roomId: _roomId);
                    var rejoinResp = await _gatewayClient.SendRequestAsync(
                        NetworkOpCodes.JoinRoom, rejoinPayload);
                    var rejoinResult = NetworkProtocol.DecodeJoinRoomResp(rejoinResp);
                    if (rejoinResult.Success)
                    {
                        _numericRoomId = rejoinResult.NumericRoomId;
                        Platform.Log.Sync($"[StateSync] Rejoined room, NumericId: {_numericRoomId}");
                    }

                    roomJoined = true;
                }
            }

            if (roomJoined)
            {
                _connected = true;
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
        Platform.Log.Sync($"[StateSync] Disconnected from server: {reason}");
    }

    private void OnGatewayError(Exception ex)
    {
        Platform.Log.Sync($"[StateSync] Server error: {ex.Message}");
    }

    private void OnServerPush(uint opCode, byte[] payload)
    {
        switch (opCode)
        {
            case NetworkOpCodes.FramePushed:
                HandleFramePushed(payload);
                break;

            case NetworkOpCodes.SnapshotPushed:
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
            var frameData = NetworkProtocol.DecodeFramePushed(payload);

            lock (_statesLock)
            {
                _currentFrame = frameData.Frame;

                foreach (var input in frameData.Inputs)
                {
                    if (input.OpCode == InputOpCodes.Move && input.Payload != null && input.Payload.Length > 0)
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

    public void Disconnect()
    {
        _gatewayClient?.Dispose();
        _gatewayClient = null;

        _connected = false;
        OnConnectionChanged?.Invoke(false);
        Platform.Log.Sync("[StateSync] Disconnected");
    }

    public void SubmitInput(PlayerInput input)
    {
        if (!_connected || _gatewayClient == null) return;

        var payload = NetworkProtocol.EncodeSubmitFrameInput(
            _numericRoomId,
            worldId: 0,
            frame: _currentFrame,
            playerId: (uint)LocalActorId,
            inputOpCode: (int)input.OpCode,
            inputPayload: input.Payload);

        _ = _gatewayClient.SendServerPushAsync(NetworkOpCodes.SubmitFrameInput, payload);
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
