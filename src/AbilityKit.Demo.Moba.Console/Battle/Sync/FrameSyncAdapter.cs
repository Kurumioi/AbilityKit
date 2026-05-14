using System;
using System.Collections.Generic;
using AbilityKit.Demo.Moba.Console.Core.Battle.Context;
using AbilityKit.Demo.Moba.Console.Events;
using EC = AbilityKit.World.ECS;

namespace AbilityKit.Demo.Moba.Console.Battle.Sync;

/// <summary>
/// 帧同步适配器
/// 封装本地帧循环，用于 Lockstep 模式
/// 复用现有的 ConsoleSyncFeature 和 BattleFlow
/// </summary>
public sealed class FrameSyncAdapter : IBattleSyncAdapter
{
    private ConsoleBattleContext _context;
    private BattleStartConfig _config;
    private bool _initialized;
    private bool _connected;
    private int _currentFrame;
    private double _logicTimeSeconds;
    private double _renderTimeSeconds;
    private int _localActorId;

    private readonly List<ActorStateSnapshot> _actorStates = new();
    private int _syncTickCounter;

    public SyncMode Mode => SyncMode.Lockstep;
    public bool IsConnected => _connected;
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
        _connected = true; // 本地模式始终连接
        _currentFrame = 0;
        _logicTimeSeconds = 0;
        _localActorId = _config.Players?.Count > 0
            ? HashPlayerId(_config.Players[0].PlayerId)
            : 1;

        OnConnectionChanged?.Invoke(_connected);
        Platform.Log.Sync($"[FrameSync] Initialized - Mode: {Mode}, LocalActorId: {_localActorId}");
    }

    public void Connect(string host, int port, string roomId, string playerId)
    {
        if (!_initialized)
            throw new InvalidOperationException("FrameSyncAdapter not initialized. Call Initialize first.");

        // 本地模式不支持远程连接，抛出异常
        throw new NotSupportedException("FrameSyncAdapter is a local adapter. Use StateSyncAdapter for remote connections.");
    }

    public void Disconnect()
    {
        _connected = false;
        OnConnectionChanged?.Invoke(_connected);
        Platform.Log.Sync("[FrameSync] Disconnected");
    }

    public void SubmitInput(PlayerInput input)
    {
        // 在帧同步模式下，输入被直接应用到本地世界
        // 无需额外处理，ConsoleInputFeature 已经处理了输入
    }

    public void Tick(float deltaTime)
    {
        if (!_initialized || !_connected) return;

        _currentFrame = _context.LastFrame;
        _logicTimeSeconds = _context.LogicTimeSeconds;

        // 渲染时间比逻辑时间滞后一帧（用于插值）
        _renderTimeSeconds = _logicTimeSeconds - (1.0 / _config.TickRate);

        _syncTickCounter++;

        // 每 300 帧（约 10 秒 @30FPS）输出一次状态
        if (_syncTickCounter % 300 == 0)
        {
            Platform.Log.Sync($"[FrameSync] Frame: {_currentFrame}, State: {_context.State}, Actors: {_context.EcsWorld?.AliveCount ?? 0}");
        }

        // 发布帧同步事件
        OnFrameSync?.Invoke(_currentFrame, _logicTimeSeconds);

        // 发布帧事件（供其他模块使用）
        BattleEventBus.Publish(new FrameSyncEvent
        {
            Frame = _currentFrame,
            ActorCount = _context.EcsWorld?.AliveCount ?? 0,
            LogicTimeSeconds = _logicTimeSeconds
        });
    }

    public ActorStateSnapshot[] GetAllActorStates()
    {
        // 简化实现：返回空数组
        // 完整实现需要使用 EntityWorld 的查询 API
        return Array.Empty<ActorStateSnapshot>();
    }

    private static int HashPlayerId(string playerId)
    {
        return playerId.GetHashCode() & 0xFFFF;
    }

    public void Dispose()
    {
        _initialized = false;
        _connected = false;
        OnConnectionChanged = null;
        OnFrameSync = null;
        OnActorStateSnapshot = null;
        Platform.Log.Sync("[FrameSync] Disposed");
    }
}
