using System;
using System.Collections.Generic;
using System.Threading;
using AbilityKit.Ability.Host.Extensions.Server.BattleHost;
using AbilityKit.Orleans.Contracts.Battle;
using AbilityKit.Orleans.Grains.Battle.Gameplay;
using AbilityKit.Orleans.Grains.Battle.Protocol;
using AbilityKit.Orleans.Grains.Rooms.Gameplay;
using Microsoft.Extensions.Logging;

namespace AbilityKit.Orleans.Grains.Battle;

/// <summary>
/// Battle Logic Host Grain 实现。
/// 负责通用战斗生命周期、输入缓冲、Tick 和状态同步，具体玩法运行时由 BattleRuntimeAdapter 提供。
/// </summary>
public sealed class BattleLogicHostGrain : Grain, IBattleLogicHostGrain
{
    private readonly ILogger<BattleLogicHostGrain> _logger;
    private readonly BattleRuntimeRegistry _runtimeRegistry;
    private readonly BattleHostState _battleHostState = new();
    private readonly IBattleInputBuffer<BattleInputItem> _inputBuffer = new BattleInputBuffer<BattleInputItem>();
    private readonly IBattleTickDriver<BattleInputItem> _tickDriver;
    private readonly BattleObserverRegistry<IStateSyncObserver> _observerRegistry = new();
    private readonly BattleSnapshotSyncPolicy _snapshotSyncPolicy = new();
    private readonly BattleSnapshotPublisher<IStateSyncObserver, StateSyncPush> _snapshotPublisher;

    private IDisposable? _timer;
    private IBattleRuntimeSession? _runtimeSession;
    private int _tickRate = 30;
    private ulong _worldId;
    private string _battleId = string.Empty;
    private bool _initialized;
    private TimeSpan _tickInterval;

    public BattleLogicHostGrain(
        ILogger<BattleLogicHostGrain> logger,
        ServerMobaWorldManager worldManager)
    {
        _logger = logger;
        _runtimeRegistry = new BattleRuntimeRegistry(
            new IBattleRuntimeAdapter[]
            {
                new MobaBattleRuntimeAdapter(worldManager, DefaultOrleansBattleProtocolMapper.Instance),
                new ShooterBattleRuntimeAdapter()
            },
            MobaRoomGameplayAdapter.DefaultRoomType);
        _tickDriver = new BattleTickDriver<BattleInputItem>(SubmitRuntimeInputs, TickBattleWorld);
        _snapshotPublisher = new BattleSnapshotPublisher<IStateSyncObserver, StateSyncPush>(
            BuildStateSyncPush,
            SendStateSyncPush,
            HandleSnapshotPublishError);
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
        StopBattleRuntime();
        return Task.CompletedTask;
    }

    public Task InitializeBattleAsync(BattleInitParams initParams)
    {
        if (initParams is null)
        {
            throw new ArgumentNullException(nameof(initParams));
        }

        if (_initialized)
        {
            _logger.LogWarning("[BattleLogicHost] Already initialized, ignoring duplicate init request");
            return Task.CompletedTask;
        }

        _battleId = this.GetPrimaryKeyString();
        _worldId = initParams.WorldId;
        _tickRate = initParams.TickRate > 0 ? initParams.TickRate : 30;
        _tickInterval = TimeSpan.FromSeconds(1.0 / _tickRate);
        _battleHostState.Initialize(_worldId, _battleId, _tickRate);

        _logger.LogInformation(
            "[BattleLogicHost] Initializing battle - BattleId: {BattleId}, RoomType: {RoomType}, WorldId: {WorldId}, TickRate: {TickRate}, Players: {PlayerCount}",
            _battleId,
            initParams.RoomType ?? MobaRoomGameplayAdapter.DefaultRoomType,
            _worldId,
            _tickRate,
            initParams.Players?.Count ?? 0);

        var adapter = _runtimeRegistry.Resolve(initParams.RoomType);
        _runtimeSession = adapter.CreateSession(_battleId);
        var startResult = _runtimeSession.Start(initParams);
        if (!startResult.Succeeded)
        {
            _logger.LogError("[BattleLogicHost] Battle initialization failed. Error: {Error}", startResult.Error);
            StopBattleRuntime();
            return Task.CompletedTask;
        }

        _initialized = true;
        PublishInitialSnapshot();
        StartBattleTimer();
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

        if (worldId != 0 && worldId != _worldId)
        {
            _logger.LogWarning("[BattleLogicHost] Input world mismatch. Expected: {ExpectedWorldId}, Actual: {ActualWorldId}", _worldId, worldId);
            return Task.CompletedTask;
        }

        if (input == null)
        {
            return Task.CompletedTask;
        }

        if (!_inputBuffer.Enqueue(frame, input))
        {
            _logger.LogWarning("[BattleLogicHost] Input rejected by host input buffer. Frame: {Frame}, PlayerId: {PlayerId}", frame, input.PlayerId);
            return Task.CompletedTask;
        }

        _logger.LogDebug(
            "[BattleLogicHost] Input received - Frame: {Frame}, PlayerId: {PlayerId}, OpCode: {OpCode}",
            frame,
            input.PlayerId,
            input.OpCode);

        return Task.CompletedTask;
    }

    public Task<int> GetCurrentFrameAsync()
    {
        return Task.FromResult(_battleHostState.Frame);
    }

    public Task<BattleSnapshot?> GetSnapshotAsync()
    {
        return Task.FromResult(_runtimeSession?.GetSnapshot(_battleHostState.Frame));
    }

    public Task SubscribeAsync(IStateSyncObserver observer)
    {
        if (observer == null)
        {
            throw new ArgumentNullException(nameof(observer));
        }

        if (_observerRegistry.Subscribe(observer))
        {
            _logger.LogInformation("[BattleLogicHost] Observer subscribed. Total observers: {Count}", _observerRegistry.Count);

            if (_initialized)
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

        if (_observerRegistry.Unsubscribe(observer))
        {
            _logger.LogInformation("[BattleLogicHost] Observer unsubscribed. Total observers: {Count}", _observerRegistry.Count);
        }

        return Task.CompletedTask;
    }

    public Task DestroyAsync()
    {
        _logger.LogInformation("[BattleLogicHost] Destroying battle - WorldId: {WorldId}", _worldId);
        _observerRegistry.Clear();
        StopBattleRuntime();
        DeactivateOnIdle();
        return Task.CompletedTask;
    }

    private async Task OnTickAsync()
    {
        try
        {
            var tickResult = _tickDriver.Tick(_battleHostState, _inputBuffer);
            if (!tickResult.InputSubmitted)
            {
                _logger.LogWarning("[BattleLogicHost] Runtime input rejected. Frame: {Frame}", tickResult.Frame);
            }

            if (_snapshotSyncPolicy.ShouldPublish(_observerRegistry.Count, tickResult.WorldTicked))
            {
                PushSnapshot(tickResult.Frame, _snapshotSyncPolicy.ShouldCreateFullSnapshot(tickResult.Frame));
            }

            _logger.LogDebug(
                "[BattleLogicHost] Tick - Frame: {Frame}, Inputs: {InputCount}, Commands: {CommandCount}, Observers: {ObserverCount}",
                tickResult.Frame,
                tickResult.InputCount,
                tickResult.CommandCount,
                _observerRegistry.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[BattleLogicHost] Error in OnTickAsync");
        }

        await Task.CompletedTask;
    }

    private int SubmitRuntimeInputs(int frame, IReadOnlyList<BattleInputItem> inputs)
    {
        if (inputs == null || inputs.Count == 0 || _runtimeSession == null)
        {
            return 0;
        }

        return _runtimeSession.SubmitInputs(frame, inputs);
    }

    private bool TickBattleWorld(int frame, int tickRate, float deltaTime)
    {
        return _runtimeSession?.Tick(frame, tickRate, deltaTime) == true;
    }

    private void PublishInitialSnapshot()
    {
        if (_observerRegistry.Count > 0)
        {
            PushSnapshot(isFullSnapshot: true);
        }
    }

    private void StartBattleTimer()
    {
        _timer = RegisterTimer(_ => OnTickAsync(), state: null, dueTime: _tickInterval, period: _tickInterval);
    }

    private void PushSnapshot(bool isFullSnapshot)
    {
        PushSnapshot(_battleHostState.Frame, isFullSnapshot);
    }

    private void PushSnapshot(int frame, bool isFullSnapshot)
    {
        _snapshotPublisher.Publish(_observerRegistry.Snapshot(), frame, isFullSnapshot);
    }

    private StateSyncPush BuildStateSyncPush(int frame, bool isFullSnapshot)
    {
        return _runtimeSession?.CreateStateSyncPush(_worldId, frame, isFullSnapshot)
            ?? new StateSyncPush
            {
                WorldId = _worldId,
                Frame = frame,
                Timestamp = DateTime.UtcNow.Ticks,
                IsFullSnapshot = isFullSnapshot
            };
    }

    private static void SendStateSyncPush(IStateSyncObserver observer, StateSyncPush push)
    {
        observer.OnSnapshotPushed(push);
    }

    private void HandleSnapshotPublishError(IStateSyncObserver observer, Exception exception)
    {
        _logger.LogError(exception, "[BattleLogicHost] Error pushing snapshot to observer");
    }

    private void StopBattleRuntime()
    {
        _timer?.Dispose();
        _timer = null;
        _runtimeSession?.Dispose();
        _runtimeSession = null;
        _battleId = string.Empty;
        _worldId = 0;
        _initialized = false;
        _inputBuffer.Clear();
        _battleHostState.Reset();
    }
}
