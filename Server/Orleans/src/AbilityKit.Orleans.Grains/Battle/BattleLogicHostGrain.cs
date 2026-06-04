using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.Extensions.Logging;
using AbilityKit.Ability.FrameSync;
using AbilityKit.Ability.Host;
using AbilityKit.Ability.Host.Framework;
using AbilityKit.Ability.Host.Extensions.Server.BattleHost;
using AbilityKit.Demo.Moba.Services;
using AbilityKit.Orleans.Contracts.Battle;
using AbilityKit.Orleans.Grains.Battle.Protocol;
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
    private readonly IOrleansBattleProtocolMapper _protocolMapper;
    private readonly BattleHostState _battleHostState = new();
    private readonly IBattleInputBuffer<BattleInputItem> _inputBuffer = new BattleInputBuffer<BattleInputItem>();
    private readonly IBattleTickDriver<BattleInputItem> _tickDriver;
    private readonly BattleObserverRegistry<IStateSyncObserver> _observerRegistry = new();
    private readonly BattleSnapshotSyncPolicy _snapshotSyncPolicy = new();
    private readonly BattleSnapshotPublisher<IStateSyncObserver, StateSyncPush> _snapshotPublisher;
    private readonly BattleHostLifecycleRunner _lifecycleRunner;

    private IDisposable? _timer;
    private int _tickRate = 30;
    private ulong _worldId;
    private string _battleId = string.Empty;
    private bool _initialized;
    private IWorld? _battleWorld;
    private IWorldStateSnapshotProvider? _snapshotProvider;
    private IMobaBattleRuntimePort? _runtimePort;

    private TimeSpan _tickInterval;
    private const int MaxCatchUpFramesPerTimer = 5;

    public BattleLogicHostGrain(
        ILogger<BattleLogicHostGrain> logger,
        ServerMobaWorldManager worldManager)
    {
        _logger = logger;
        _worldManager = worldManager;
        _protocolMapper = DefaultOrleansBattleProtocolMapper.Instance;
        _tickDriver = new BattleTickDriver<BattleInputItem>(SubmitRuntimeInputs, TickBattleWorld);
        _snapshotPublisher = new BattleSnapshotPublisher<IStateSyncObserver, StateSyncPush>(
            BuildStateSyncPush,
            SendStateSyncPush,
            HandleSnapshotPublishError);
        _lifecycleRunner = new BattleHostLifecycleRunner(
            _battleHostState,
            CreateBattleWorld,
            ResolveRuntimePort,
            ValidateRuntimeStart,
            StartRuntime,
            ResolveSnapshotProvider,
            PublishInitialSnapshot,
            StartBattleTimer,
            CleanupBattleWorld);
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
        _lifecycleRunner.Stop();
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
        var context = new BattleHostStartContext(initParams.WorldId, roomId, initParams.TickRate);
        _logger.LogInformation(
            "[BattleLogicHost] Initializing battle - RoomId: {RoomId}, WorldId: {WorldId}, TickRate: {TickRate}, Players: {PlayerCount}",
            context.BattleId, context.WorldId, context.TickRate, initParams.Players?.Count ?? 0);

        _pendingInitParams = initParams;
        var result = _lifecycleRunner.Start(context);
        _pendingInitParams = null;
        if (!result.Succeeded)
        {
            _logger.LogError("[BattleLogicHost] Battle initialization failed. Result: {Result}", result.ToString());
            return Task.CompletedTask;
        }

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

        if (!_inputBuffer.Enqueue(frame, input))
        {
            _logger.LogWarning("[BattleLogicHost] Input rejected by host input buffer. Frame: {Frame}, PlayerId: {PlayerId}", frame, input.PlayerId);
            return Task.CompletedTask;
        }

        _logger.LogDebug(
            "[BattleLogicHost] Input received - Frame: {Frame}, PlayerId: {PlayerId}, OpCode: {OpCode}",
            frame, input.PlayerId, input.OpCode);

        return Task.CompletedTask;
    }

    public Task<int> GetCurrentFrameAsync()
    {
        return Task.FromResult(_battleHostState.Frame);
    }

    public Task<BattleSnapshot?> GetSnapshotAsync()
    {
        if (_battleWorld == null || _snapshotProvider == null)
        {
            return Task.FromResult<BattleSnapshot?>(null);
        }

        var frame = _battleHostState.Frame;
        var frameIndex = new FrameIndex(frame);
        if (_snapshotProvider.TryGetSnapshot(frameIndex, out var snapshot))
        {
            var battleSnapshot = _protocolMapper.CreateBattleSnapshot(frame, snapshot, _runtimePort?.GetAllEntityStates());
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

        if (_observerRegistry.Subscribe(observer))
        {
            _logger.LogInformation("[BattleLogicHost] Observer subscribed. Total observers: {Count}", _observerRegistry.Count);

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
        _lifecycleRunner.Stop();

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
                tickResult.Frame, tickResult.InputCount, tickResult.CommandCount, _observerRegistry.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[BattleLogicHost] Error in OnTickAsync");
        }
    }

    private int SubmitRuntimeInputs(int frame, IReadOnlyList<BattleInputItem> inputs)
    {
        if (inputs == null || inputs.Count == 0 || _runtimePort == null)
        {
            return 0;
        }

        var commands = _protocolMapper.CreatePlayerInputCommands(frame, inputs);
        if (commands.Count == 0)
        {
            return 0;
        }

        var result = _runtimePort.Submit(new FrameIndex(frame), commands);
        if (!result.Succeeded)
        {
            _logger.LogWarning("[BattleLogicHost] Runtime input rejected. Frame: {Frame}, Result: {Result}", frame, result.ToString());
            return 0;
        }

        return commands.Count;
    }

    private bool TickBattleWorld(int frame, int tickRate, float deltaTime)
    {
        if (_battleWorld == null)
        {
            return false;
        }

        _battleWorld.Tick(deltaTime);
        return true;
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
        var frameIndex = new FrameIndex(frame);
        WorldStateSnapshot snapshot = default;
        var hasSnapshot = _runtimePort?.TryGetSnapshot(frameIndex, out snapshot) == true;
        if (!hasSnapshot && _snapshotProvider != null)
        {
            hasSnapshot = _snapshotProvider.TryGetSnapshot(frameIndex, out snapshot);
        }

        return _protocolMapper.CreateStateSyncPush(
            _worldId,
            frame,
            hasSnapshot ? snapshot : null,
            _runtimePort?.GetAllEntityStates(),
            isFullSnapshot);
    }

    private static void SendStateSyncPush(IStateSyncObserver observer, StateSyncPush push)
    {
        observer.OnSnapshotPushed(push);
    }

    private void HandleSnapshotPublishError(IStateSyncObserver observer, Exception exception)
    {
        _logger.LogError(exception, "[BattleLogicHost] Error pushing snapshot to observer");
    }

    private BattleInitParams? _pendingInitParams;

    private BattleHostLifecycleResult CreateBattleWorld(BattleHostStartContext context)
    {
        _worldId = context.WorldId;
        _battleId = context.BattleId;
        _tickRate = context.TickRate;
        _tickInterval = context.TickInterval;
        _battleWorld = _worldManager.CreateBattleWorld(context.BattleId, context.TickRate);
        return _battleWorld != null
            ? BattleHostLifecycleResult.Success()
            : BattleHostLifecycleResult.Fail(BattleHostLifecycleErrorCode.CreateHostFailed, "Battle world creation returned null.");
    }

    private BattleHostLifecycleResult ResolveRuntimePort(BattleHostStartContext context)
    {
        if (_battleWorld == null || !_battleWorld.Services.TryResolve<IMobaBattleRuntimePort>(out _runtimePort) || _runtimePort == null)
        {
            return BattleHostLifecycleResult.Fail(BattleHostLifecycleErrorCode.RuntimeNotResolved, "IMobaBattleRuntimePort not resolved.");
        }

        return BattleHostLifecycleResult.Success();
    }

    private BattleHostLifecycleResult ValidateRuntimeStart(BattleHostStartContext context)
    {
        if (_runtimePort == null || !_runtimePort.Status.IsReadyForGameStart)
        {
            return BattleHostLifecycleResult.Fail(BattleHostLifecycleErrorCode.RuntimeNotReadyForStart, _runtimePort?.Status.ToString() ?? "Runtime port is null.");
        }

        return BattleHostLifecycleResult.Success();
    }

    private BattleHostLifecycleResult StartRuntime(BattleHostStartContext context)
    {
        if (_runtimePort == null || _pendingInitParams == null)
        {
            return BattleHostLifecycleResult.Fail(BattleHostLifecycleErrorCode.StartRuntimeRejected, "Runtime port or init params are missing.");
        }

        var startSpec = _protocolMapper.CreateGameStartSpec(context.BattleId, context.TickRate, _pendingInitParams);
        var startResult = _runtimePort.TryStartGame(in startSpec);
        return startResult.Succeeded
            ? BattleHostLifecycleResult.Success()
            : BattleHostLifecycleResult.Fail(BattleHostLifecycleErrorCode.StartRuntimeRejected, startResult.ToString());
    }

    private BattleHostLifecycleResult ResolveSnapshotProvider(BattleHostStartContext context)
    {
        if (_battleWorld == null)
        {
            return BattleHostLifecycleResult.Fail(BattleHostLifecycleErrorCode.SnapshotProviderNotResolved, "Battle world is null.");
        }

        _snapshotProvider = _battleWorld.Services.Resolve<IWorldStateSnapshotProvider>();
        if (_snapshotProvider == null)
        {
            return BattleHostLifecycleResult.Fail(BattleHostLifecycleErrorCode.SnapshotProviderNotResolved, "IWorldStateSnapshotProvider not resolved.");
        }

        if (_runtimePort != null && !_runtimePort.Status.IsReadyForBattleLoop)
        {
            _logger.LogWarning("[BattleLogicHost] Runtime battle-loop capabilities are incomplete. Status: {Status}", _runtimePort.Status.ToString());
        }

        return BattleHostLifecycleResult.Success();
    }

    private BattleHostLifecycleResult PublishInitialSnapshot(BattleHostStartContext context)
    {
        if (_observerRegistry.Count > 0)
        {
            PushSnapshot(isFullSnapshot: true);
        }

        return BattleHostLifecycleResult.Success();
    }

    private BattleHostLifecycleResult StartBattleTimer(BattleHostStartContext context, TimeSpan tickInterval)
    {
        _timer = RegisterTimer(_ => OnTickAsync(), state: null, dueTime: tickInterval, period: tickInterval);
        return _timer != null
            ? BattleHostLifecycleResult.Success()
            : BattleHostLifecycleResult.Fail(BattleHostLifecycleErrorCode.TimerStartFailed, "RegisterTimer returned null.");
    }

    private void CleanupBattleWorld(BattleHostLifecycleErrorCode reason)
    {
        _timer?.Dispose();
        _timer = null;
        if (_battleWorld != null)
        {
            _worldManager.DestroyBattleWorld(string.IsNullOrEmpty(_battleId) ? this.GetPrimaryKeyString() : _battleId);
        }

        _battleWorld = null;
        _battleId = string.Empty;
        _snapshotProvider = null;
        _runtimePort = null;
        _pendingInitParams = null;
        _initialized = false;
        _inputBuffer.Clear();
    }
}
