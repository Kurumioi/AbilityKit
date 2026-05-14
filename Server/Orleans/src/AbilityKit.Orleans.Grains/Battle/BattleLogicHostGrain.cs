using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.Extensions.Logging;
using AbilityKit.Ability.Host;
using AbilityKit.Ability.Host.Framework;
using AbilityKit.Orleans.Contracts.Battle;
using IWorldStateSnapshotProvider = AbilityKit.Ability.Host.IWorldStateSnapshotProvider;
using IWorld = AbilityKit.Ability.World.Abstractions.IWorld;

namespace AbilityKit.Orleans.Grains.Battle;

/// <summary>
/// Battle Logic Host Grain 实现
/// 在服务器端运行战斗逻辑，使用 Moba.Core 战斗世界
/// </summary>
public sealed class BattleLogicHostGrain : Grain, IBattleLogicHostGrain
{
    private readonly ILogger<BattleLogicHostGrain> _logger;
    private readonly ServerMobaWorldManager _worldManager;

    private IDisposable? _timer;
    private int _frame;
    private int _tickRate = 30;
    private ulong _worldId;
    private bool _initialized;
    private IWorld? _battleWorld;
    private IWorldStateSnapshotProvider? _snapshotProvider;

    // 每帧的输入缓冲
    private readonly Dictionary<int, List<BattleInputItem>> _inputsByFrame = new();

    // 观察者列表
    private readonly List<IStateSyncObserver> _observers = new();

    private TimeSpan _tickInterval;
    private const int MaxCatchUpFramesPerTimer = 5;

    public BattleLogicHostGrain(
        ILogger<BattleLogicHostGrain> logger,
        ServerMobaWorldManager worldManager)
    {
        _logger = logger;
        _worldManager = worldManager;
    }

    public override Task OnActivateAsync(CancellationToken cancellationToken)
    {
        var key = this.GetPrimaryKeyString();
        _logger.LogInformation("[BattleLogicHost] Activated with key: {Key}", key);
        return Task.CompletedTask;
    }

    public override Task OnDeactivateAsync(DeactivationReason reason, CancellationToken cancellationToken)
    {
        _logger.LogInformation("[BattleLogicHost] Deactivated: {Reason}", reason);
        _timer?.Dispose();
        _timer = null;
        CleanupBattleWorld();
        return Task.CompletedTask;
    }

    public Task InitializeBattleAsync(BattleInitParams initParams)
    {
        if (_initialized)
        {
            _logger.LogWarning("[BattleLogicHost] Already initialized, ignoring duplicate init request");
            return Task.CompletedTask;
        }

        var roomId = this.GetPrimaryKeyString();
        _worldId = initParams.WorldId;
        _tickRate = initParams.TickRate > 0 ? initParams.TickRate : 30;
        _frame = 0;
        _tickInterval = TimeSpan.FromMilliseconds(1000.0 / _tickRate);

        _logger.LogInformation(
            "[BattleLogicHost] Initializing battle - RoomId: {RoomId}, WorldId: {WorldId}, TickRate: {TickRate}, Players: {PlayerCount}",
            roomId, _worldId, _tickRate, initParams.Players?.Count ?? 0);

        // 创建 Moba 战斗世界
        _battleWorld = _worldManager.CreateBattleWorld(roomId, _tickRate);
        _snapshotProvider = _battleWorld.Services.Resolve<IWorldStateSnapshotProvider>();

        // TODO: 使用 initParams 初始化战斗世界
        // InitializeMobaWorld(initParams);

        // 立即推送初始快照
        if (_observers.Count > 0)
        {
            PushSnapshot(isFullSnapshot: true);
        }

        // 启动帧循环
        _timer = RegisterTimer(_ => OnTickAsync(), state: null, dueTime: _tickInterval, period: _tickInterval);

        _initialized = true;
        _logger.LogInformation("[BattleLogicHost] Battle initialized successfully");
        return Task.CompletedTask;
    }

    public Task SubmitInputAsync(ulong worldId, int frame, BattleInputItem input)
    {
        if (!_initialized)
        {
            _logger.LogWarning("[BattleLogicHost] SubmitInput called but not initialized");
            return Task.CompletedTask;
        }

        if (input == null)
        {
            return Task.CompletedTask;
        }

        // 存储输入
        if (!_inputsByFrame.TryGetValue(frame, out var list))
        {
            list = new List<BattleInputItem>(8);
            _inputsByFrame[frame] = list;
        }

        list.Add(input);

        _logger.LogDebug(
            "[BattleLogicHost] Input received - Frame: {Frame}, PlayerId: {PlayerId}, OpCode: {OpCode}",
            frame, input.PlayerId, input.OpCode);

        return Task.CompletedTask;
    }

    public Task<int> GetCurrentFrameAsync()
    {
        return Task.FromResult(_frame);
    }

    public Task<BattleSnapshot?> GetSnapshotAsync()
    {
        if (_battleWorld == null || _snapshotProvider == null)
        {
            return Task.FromResult<BattleSnapshot?>(null);
        }

        var frameIndex = new AbilityKit.Ability.FrameSync.FrameIndex(_frame);
        if (_snapshotProvider.TryGetSnapshot(frameIndex, out var snapshot))
        {
            // 解析快照
            var battleSnapshot = ParseWorldSnapshot(snapshot);
            return Task.FromResult<BattleSnapshot?>(battleSnapshot);
        }

        return Task.FromResult<BattleSnapshot?>(null);
    }

    public Task SubscribeAsync(IStateSyncObserver observer)
    {
        if (observer == null)
        {
            throw new ArgumentNullException(nameof(observer));
        }

        if (!_observers.Contains(observer))
        {
            _observers.Add(observer);
            _logger.LogInformation("[BattleLogicHost] Observer subscribed. Total observers: {Count}", _observers.Count);

            // 新订阅者加入时，发送当前完整状态
            if (_initialized && _battleWorld != null)
            {
                PushSnapshot(isFullSnapshot: true);
            }
        }

        return Task.CompletedTask;
    }

    public Task UnsubscribeAsync(IStateSyncObserver observer)
    {
        if (observer == null)
        {
            return Task.CompletedTask;
        }

        if (_observers.Remove(observer))
        {
            _logger.LogInformation("[BattleLogicHost] Observer unsubscribed. Total observers: {Count}", _observers.Count);
        }

        return Task.CompletedTask;
    }

    public Task DestroyAsync()
    {
        _logger.LogInformation("[BattleLogicHost] Destroying battle - WorldId: {WorldId}", _worldId);

        _timer?.Dispose();
        _timer = null;
        _inputsByFrame.Clear();
        _observers.Clear();
        _initialized = false;

        CleanupBattleWorld();

        DeactivateOnIdle();

        return Task.CompletedTask;
    }

    private async Task OnTickAsync()
    {
        try
        {
            // 获取当前帧的输入
            List<BattleInputItem>? inputs = null;
            if (_inputsByFrame.TryGetValue(_frame, out var list) && list != null && list.Count > 0)
            {
                inputs = list;
            }

            _inputsByFrame.Remove(_frame);

            // Tick 战斗世界
            if (_battleWorld != null)
            {
                var deltaTime = 1.0f / _tickRate;
                _battleWorld.Tick(deltaTime);
            }

            // 推送快照
            if (_observers.Count > 0 && _battleWorld != null)
            {
                PushSnapshot(isFullSnapshot: _frame % 30 == 0);
            }

            _logger.LogDebug(
                "[BattleLogicHost] Tick - Frame: {Frame}, Inputs: {InputCount}, Observers: {ObserverCount}",
                _frame, inputs?.Count ?? 0, _observers.Count);

            _frame++;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[BattleLogicHost] Error in OnTickAsync");
        }
    }

    private void PushSnapshot(bool isFullSnapshot)
    {
        if (_observers.Count == 0 || _snapshotProvider == null)
            return;

        var frameIndex = new AbilityKit.Ability.FrameSync.FrameIndex(_frame);
        if (!_snapshotProvider.TryGetSnapshot(frameIndex, out var snapshot))
            return;

        var push = ConvertToStateSyncPush(snapshot, isFullSnapshot);

        // 复制观察者列表，避免在推送过程中修改集合
        var observersCopy = _observers.ToArray();
        foreach (var observer in observersCopy)
        {
            try
            {
                observer.OnSnapshotPushed(push);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[BattleLogicHost] Error pushing snapshot to observer");
            }
        }
    }

    private StateSyncPush ConvertToStateSyncPush(WorldStateSnapshot snapshot, bool isFullSnapshot)
    {
        // 解析 snapshot.Payload 获取 Actor 状态
        // 这里需要根据 OpCode 解析不同的快照类型
        var actors = new List<ActorSnapshot>();

        // TODO: 根据 OpCode 解析正确的快照格式
        // if (snapshot.OpCode == MobaOpCode.ActorTransformSnapshot)
        // {
        //     var entries = MobaActorTransformSnapshotCodec.Deserialize(snapshot.Payload);
        //     foreach (var entry in entries)
        //     {
        //         actors.Add(new ActorSnapshot { ... });
        //     }
        // }

        return new StateSyncPush
        {
            WorldId = _worldId,
            Frame = _frame,
            Timestamp = DateTime.UtcNow.Ticks,
            Actors = actors,
            IsFullSnapshot = isFullSnapshot
        };
    }

    private BattleSnapshot? ParseWorldSnapshot(WorldStateSnapshot snapshot)
    {
        // TODO: 根据 OpCode 解析快照
        return null;
    }

    private void CleanupBattleWorld()
    {
        if (_battleWorld != null)
        {
            var roomId = this.GetPrimaryKeyString();
            _worldManager.DestroyBattleWorld(roomId);
            _battleWorld = null;
            _snapshotProvider = null;
        }
    }
}
