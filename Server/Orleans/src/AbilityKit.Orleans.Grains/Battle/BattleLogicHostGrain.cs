using System;
using System.Collections.Generic;
using System.Threading;
using AbilityKit.Ability.Host.Extensions.Server.BattleHost;
using AbilityKit.Orleans.Contracts.Battle;
using AbilityKit.Orleans.Contracts.Rooms;
using AbilityKit.Orleans.Grains.Battle.Gameplay;
using AbilityKit.Orleans.Grains.Gameplay;
using Microsoft.Extensions.Logging;

namespace AbilityKit.Orleans.Grains.Battle;

/// <summary>
/// Battle Logic Host Grain 实现。
/// 负责通用战斗生命周期、输入缓冲、Tick 与状态同步，具体玩法运行时由 BattleRuntimeAdapter 提供。
/// </summary>
public sealed class BattleLogicHostGrain : Grain, IBattleLogicHostGrain
{
    private readonly ILogger<BattleLogicHostGrain> _logger;
    private readonly ServerGameplayModuleCatalog _gameplayModules;
    private readonly BattleRuntimeRegistry _runtimeRegistry;
    private readonly BattleHostState _battleHostState = new();
    private readonly IBattleInputBuffer<BattleInputItem> _inputBuffer = new BattleInputBuffer<BattleInputItem>();
    private readonly IBattleTickDriver<BattleInputItem> _tickDriver;
    private readonly BattleObserverRegistry<IStateSyncObserverGrain> _observerRegistry = new();
    private readonly BattleSnapshotSyncPolicy _snapshotSyncPolicy = new();
    private readonly BattleSnapshotPublisher<IStateSyncObserverGrain, StateSyncPush> _snapshotPublisher;

    private IDisposable? _timer;
    private IBattleRuntimeSession? _runtimeSession;
    private int _tickRate = 30;
    private ulong _worldId;
    private string _battleId = string.Empty;
    private bool _initialized;
    private TimeSpan _tickInterval;
    private WorldStartAnchor? _worldStartAnchor;
    private int _inputDelayFrames;
    private ServerBattleSyncProfile? _syncProfile;
    private string _syncTemplateId = string.Empty;

    public BattleLogicHostGrain(
        ILogger<BattleLogicHostGrain> logger,
        ServerBattleWorldManager worldManager)
    {
        _logger = logger;
        _gameplayModules = ServerGameplayModuleCatalog.Default;
        _runtimeRegistry = new BattleRuntimeRegistry(
            _gameplayModules.CreateBattleRuntimeAdapters(worldManager),
            _gameplayModules.GameplayCatalog);
        _tickDriver = new BattleTickDriver<BattleInputItem>(SubmitRuntimeInputs, TickBattleWorld);
        _snapshotPublisher = new BattleSnapshotPublisher<IStateSyncObserverGrain, StateSyncPush>(
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
        var module = _gameplayModules.ResolveModule(initParams.RoomType);
        _syncProfile = module.SyncProfile;
        var requestedSyncTemplateId = initParams.SyncOptions?.SyncTemplateId;
        if (!_syncProfile.TryResolveTemplate(requestedSyncTemplateId, out var syncTemplate))
        {
            _logger.LogError(
                "[BattleLogicHost] Unsupported sync template. BattleId: {BattleId}, RoomType: {RoomType}, RequestedTemplate: {RequestedTemplate}, DefaultTemplate: {DefaultTemplate}",
                _battleId,
                module.RoomType,
                requestedSyncTemplateId,
                _syncProfile.DefaultTemplateId);
            StopBattleRuntime();
            return Task.CompletedTask;
        }

        var syncOptions = initParams.SyncOptions;
        _syncTemplateId = syncTemplate.TemplateId;
        initParams.SyncOptions = new BattleSyncStartOptions(
            _syncTemplateId,
            syncOptions?.SyncModel ?? 0,
            syncOptions?.NetworkEnvironmentId,
            syncOptions?.CarrierName,
            syncOptions?.EnableAuthoritativeWorld ?? false,
            syncOptions?.InterpolationEnabled ?? false,
            syncOptions?.InputDelayFrames ?? 0);
        _worldId = initParams.WorldId;
        _tickRate = initParams.TickRate > 0 ? initParams.TickRate : 30;
        _tickInterval = TimeSpan.FromSeconds(1.0 / _tickRate);
        _inputDelayFrames = initParams.InputDelayFrames > 0 ? initParams.InputDelayFrames : 0;
        _worldStartAnchor = new WorldStartAnchor(
            DateTime.UtcNow.Ticks,
            TimeSpan.TicksPerSecond,
            0,
            1.0 / _tickRate);
        initParams.WorldStartAnchor = _worldStartAnchor;
        _battleHostState.Initialize(_worldId, _battleId, _tickRate);

        _logger.LogInformation(
            "[BattleLogicHost] Initializing battle - BattleId: {BattleId}, RoomType: {RoomType}, WorldId: {WorldId}, TickRate: {TickRate}, Players: {PlayerCount}, SyncMode: {SyncMode}, SyncTemplate: {SyncTemplate}",
            _battleId,
            module.RoomType,
            _worldId,
            _tickRate,
            initParams.Players?.Count ?? 0,
            syncTemplate.Mode,
            _syncTemplateId);

        var adapter = _runtimeRegistry.Resolve(module.RoomType);
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

    public Task<BattleInputSubmitResult> SubmitInputAsync(ulong worldId, int frame, BattleInputItem input)
    {
        if (!_initialized)
        {
            _logger.LogWarning("[BattleLogicHost] SubmitInput called but not initialized");
            return Task.FromResult(CreateInputSubmitResult(false, frame, frame, BattleResultStatusCodes.RejectedNotInitialized, "Battle is not initialized."));
        }

        var currentFrame = _battleHostState.Frame;
        if (worldId != 0 && worldId != _worldId)
        {
            _logger.LogWarning("[BattleLogicHost] Input world mismatch. Expected: {ExpectedWorldId}, Actual: {ActualWorldId}", _worldId, worldId);
            return Task.FromResult(CreateInputSubmitResult(false, frame, frame, BattleResultStatusCodes.RejectedWorldMismatch, "Input world does not match battle world."));
        }

        if (input == null)
        {
            return Task.FromResult(CreateInputSubmitResult(false, frame, frame, BattleResultStatusCodes.RejectedNullInput, "Input is required."));
        }

        var schedule = BattleInputFrameScheduler.Schedule(
            frame,
            currentFrame,
            _inputDelayFrames,
            BattleInputFrameSchedulerOptions.Default);
        if (!schedule.Accepted)
        {
            _logger.LogWarning(
                "[BattleLogicHost] Input rejected by frame scheduler. Frame: {Frame}, CurrentFrame: {CurrentFrame}, Status: {Status}, PlayerId: {PlayerId}",
                frame,
                currentFrame,
                schedule.Status,
                input.PlayerId);
            return Task.FromResult(CreateInputSubmitResult(false, frame, schedule.AcceptedFrame, schedule.Status.ToString(), BuildInputSubmitMessage(schedule)));
        }

        if (!_inputBuffer.Enqueue(schedule.AcceptedFrame, input))
        {
            _logger.LogWarning("[BattleLogicHost] Input rejected by host input buffer. Frame: {Frame}, PlayerId: {PlayerId}", schedule.AcceptedFrame, input.PlayerId);
            return Task.FromResult(CreateInputSubmitResult(false, frame, schedule.AcceptedFrame, BattleResultStatusCodes.RejectedByInputBuffer, "Input buffer rejected the scheduled frame."));
        }

        _logger.LogDebug(
            "[BattleLogicHost] Input received - RequestedFrame: {RequestedFrame}, AcceptedFrame: {AcceptedFrame}, CurrentFrame: {CurrentFrame}, PlayerId: {PlayerId}, OpCode: {OpCode}, Status: {Status}",
            frame,
            schedule.AcceptedFrame,
            currentFrame,
            input.PlayerId,
            input.OpCode,
            schedule.Status);

        return Task.FromResult(CreateInputSubmitResult(true, frame, schedule.AcceptedFrame, schedule.Status.ToString(), BuildInputSubmitMessage(schedule)));
    }

    private BattleInputSubmitResult CreateInputSubmitResult(bool accepted, int requestedFrame, int acceptedFrame, string status, string message)
    {
        return new BattleInputSubmitResult(
            accepted,
            requestedFrame,
            acceptedFrame,
            _battleHostState.Frame,
            status,
            message);
    }

    private static string BuildInputSubmitMessage(BattleInputFrameScheduleResult schedule)
    {
        return schedule.Status switch
        {
            BattleInputAcceptStatus.Accepted => string.Empty,
            BattleInputAcceptStatus.RemappedLate => $"Input frame {schedule.RequestedFrame} is late and was remapped to frame {schedule.AcceptedFrame}.",
            BattleInputAcceptStatus.RemappedTooEarly => $"Input frame {schedule.RequestedFrame} is earlier than the configured input delay and was remapped to frame {schedule.AcceptedFrame}.",
            BattleInputAcceptStatus.RejectedInvalidFrame => "Input frame is invalid.",
            BattleInputAcceptStatus.RejectedTooFarFuture => $"Input frame {schedule.RequestedFrame} is too far ahead of current frame {schedule.CurrentFrame}.",
            _ => schedule.Status.ToString()
        };
    }

    public Task<BattlePlayerJoinResult> JoinPlayerAsync(BattlePlayerJoinRequest request)
    {
        if (!_initialized || _runtimeSession == null)
        {
            return Task.FromResult(new BattlePlayerJoinResult(
                false,
                request?.Player?.PlayerId ?? 0u,
                _battleHostState.Frame,
                BattleResultStatusCodes.RejectedNotInitialized,
                "Battle is not initialized."));
        }

        if (request is null)
        {
            return Task.FromResult(new BattlePlayerJoinResult(false, 0u, _battleHostState.Frame, BattleResultStatusCodes.RejectedNullRequest, "Join request is required."));
        }

        if (request.Player is null)
        {
            return Task.FromResult(new BattlePlayerJoinResult(false, 0u, _battleHostState.Frame, BattleResultStatusCodes.RejectedNullPlayer, "Player init info is required."));
        }

        if (request.WorldId != 0 && request.WorldId != _worldId)
        {
            _logger.LogWarning("[BattleLogicHost] JoinPlayer world mismatch. Expected: {ExpectedWorldId}, Actual: {ActualWorldId}", _worldId, request.WorldId);
            return Task.FromResult(new BattlePlayerJoinResult(
                false,
                request.Player.PlayerId,
                _battleHostState.Frame,
                BattleResultStatusCodes.RejectedWorldMismatch,
                "Join world does not match battle world."));
        }

        var result = _runtimeSession.JoinPlayer(request, _battleHostState.Frame);
        if (result.Accepted)
        {
            PushSnapshot(isFullSnapshot: true);
            _logger.LogInformation(
                "[BattleLogicHost] Player joined running battle. BattleId: {BattleId}, WorldId: {WorldId}, PlayerId: {PlayerId}, IsBot: {IsBot}",
                _battleId,
                _worldId,
                result.PlayerId,
                request.IsBot);
        }
        else
        {
            _logger.LogWarning(
                "[BattleLogicHost] Player join rejected. BattleId: {BattleId}, PlayerId: {PlayerId}, Status: {Status}, Message: {Message}",
                _battleId,
                request.Player.PlayerId,
                result.Status,
                result.Message);
        }

        return Task.FromResult(result);
    }

    public Task<BattleBotAiMountResult> MountBotAiAsync(BattleBotAiMountRequest request)
    {
        if (!_initialized || _runtimeSession == null)
        {
            return Task.FromResult(new BattleBotAiMountResult(
                false,
                request?.PlayerId ?? 0u,
                _battleHostState.Frame,
                BattleResultStatusCodes.RejectedNotInitialized,
                "Battle is not initialized."));
        }

        if (request is null)
        {
            return Task.FromResult(new BattleBotAiMountResult(false, 0u, _battleHostState.Frame, BattleResultStatusCodes.RejectedNullRequest, "Bot AI mount request is required."));
        }

        if (request.WorldId != 0 && request.WorldId != _worldId)
        {
            _logger.LogWarning("[BattleLogicHost] MountBotAi world mismatch. Expected: {ExpectedWorldId}, Actual: {ActualWorldId}", _worldId, request.WorldId);
            return Task.FromResult(new BattleBotAiMountResult(
                false,
                request.PlayerId,
                _battleHostState.Frame,
                BattleResultStatusCodes.RejectedWorldMismatch,
                "Bot AI mount world does not match battle world."));
        }

        var result = _runtimeSession.MountBotAi(request, _battleHostState.Frame);
        if (result.Accepted)
        {
            _logger.LogInformation(
                "[BattleLogicHost] Bot AI mounted. BattleId: {BattleId}, WorldId: {WorldId}, PlayerId: {PlayerId}, ProfileId: {ProfileId}",
                _battleId,
                _worldId,
                result.PlayerId,
                request.ProfileId ?? string.Empty);
        }
        else
        {
            _logger.LogWarning(
                "[BattleLogicHost] Bot AI mount rejected. BattleId: {BattleId}, PlayerId: {PlayerId}, Status: {Status}, Message: {Message}",
                _battleId,
                request.PlayerId,
                result.Status,
                result.Message);
        }

        return Task.FromResult(result);
    }

    public Task<int> GetCurrentFrameAsync()
    {
        return Task.FromResult(_battleHostState.Frame);
    }

    public Task<BattleSnapshot?> GetSnapshotAsync()
    {
        return Task.FromResult(_runtimeSession?.GetSnapshot(_battleHostState.Frame));
    }

    public Task<BattleWorldDiagnostics?> GetWorldDiagnosticsAsync()
    {
        return Task.FromResult(_runtimeSession?.GetWorldDiagnostics(_worldId, _battleHostState.Frame));
    }

    public Task<WorldStartAnchor?> GetWorldStartAnchorAsync()
    {
        return Task.FromResult(_worldStartAnchor);
    }

    public Task SubscribeAsync(IStateSyncObserverGrain observer)
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

    public async Task RequestFullSnapshotAsync(IStateSyncObserverGrain observer)
    {
        if (observer == null)
        {
            throw new ArgumentNullException(nameof(observer));
        }

        if (!_initialized)
        {
            return;
        }

        var push = BuildStateSyncPush(_battleHostState.Frame, isFullSnapshot: true);
        await observer.OnSnapshotPushedAsync(push);
    }

    public Task UnsubscribeAsync(IStateSyncObserverGrain observer)
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
        var syncTemplate = _syncProfile?.ResolveTemplate(_syncTemplateId);
        if (syncTemplate?.SupportsStateSyncPush == false)
        {
            _logger.LogTrace(
                "[BattleLogicHost] Skipping state-sync publish for frame-sync template. BattleId: {BattleId}, SyncTemplate: {SyncTemplate}",
                _battleId,
                _syncTemplateId);
            return;
        }

        _snapshotPublisher.Publish(_observerRegistry.Snapshot(), frame, isFullSnapshot);
    }

    private StateSyncPush BuildStateSyncPush(int frame, bool isFullSnapshot)
    {
        var push = _runtimeSession?.CreateStateSyncPush(_worldId, frame, isFullSnapshot)
            ?? new StateSyncPush
            {
                WorldId = _worldId,
                Frame = frame,
                IsFullSnapshot = isFullSnapshot
            };

        var serverTicks = DateTime.UtcNow.Ticks;
        push.ServerTicks = serverTicks;
        if (push.Timestamp <= 0d)
        {
            push.Timestamp = serverTicks;
        }

        return push;
    }

    private static void SendStateSyncPush(IStateSyncObserverGrain observer, StateSyncPush push)
    {
        _ = observer.OnSnapshotPushedAsync(push);
    }

    private void HandleSnapshotPublishError(IStateSyncObserverGrain observer, Exception exception)
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
        _worldStartAnchor = null;
        _syncProfile = null;
        _syncTemplateId = string.Empty;
        _inputBuffer.Clear();
        _battleHostState.Reset();
    }
}

