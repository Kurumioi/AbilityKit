#nullable enable

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AbilityKit.Ability.Host.Extensions.Client.StateSync;
using AbilityKit.Demo.Shooter.Runtime;
using AbilityKit.Demo.Shooter.View.Hosting;
using AbilityKit.Network.Abstractions;
using AbilityKit.Network.Runtime.Sync;
using AbilityKit.Protocol.Shooter;
using UnityEngine;
using UnityEngine.LowLevel;
using UnityEngine.PlayerLoop;

namespace AbilityKit.Demo.Shooter.View.PlayMode
{
    /// <summary>
    /// 远程 Shooter 状态同步演示的 Unity Play-mode host。
    /// <para>
    /// 框架包提供世界、会话与状态同步原语；该类型把示例特定的 Unity PlayerLoop、重连流程、输入泵和
    /// GameObject 渲染组合保留在 Shooter 示例层。
    /// </para>
    /// </summary>
    public static class ShooterRemoteStateSyncPlayModeHost
    {
        private static readonly UnityShooterPlayInputSource InputSource = new();
        private static readonly UnityShooterGameObjectViewSink ViewSink = new();
        private static ShooterRemoteStateSyncRuntimeState? _state;
        private static RemoteClientInputSubmitQueue<ShooterClientInputSubmitResult, ShooterClientGatewayInputSubmitResult>? _gatewayInputQueue;
        private static ShooterRemoteStateSyncLaunchOptions _options;
        private static ShooterRemoteStateSyncLaunchOptions _pausedResumeOptions;
        private static ShooterRemoteStateSyncConnectionResult? _lastConnectionResult;
        private static ShooterHostFrameInput _lastInput;
        private static ShooterClientInputSubmitResult _lastSubmitResult;
        private static ShooterClientFrameTickResult _lastTickResult;
        private static bool _playerLoopInstalled;
        private static bool _isStarting;
        private static bool _isPaused;
        private static bool _isAutoReconnecting;
        private static Exception? _lastError;
        private static long _stepCount;
        private static long _renderCount;
        private static ShooterTimeAnchorCoordinator? _timeAnchors;
        private static SyncTimeAnchor _lastRemoteTimeAnchor;
        private static ShooterRemoteLatencyCompensationDiagnostics _lastRemoteLatencyCompensationDiagnostics;

        public static event Action? StateChanged;

        public static bool IsInstalled => _playerLoopInstalled;
        public static bool IsRunning => _state != null;
        public static bool IsStarting => _isStarting;
        public static bool IsPaused => _isPaused;
        public static bool IsAutoReconnecting => _isAutoReconnecting;
        public static Exception? LastError => _lastError;
        public static ShooterClientNetworkLaunchResult? Launch => _state?.Launch;
        public static ShooterRemoteStateSyncConnectionResult? LastConnectionResult => _lastConnectionResult;
        public static ShooterClientSession? Session => _state?.Launch.Session;
        public static ShooterClientBattleHandle? Battle => _state?.Launch.Battle;
        public static ShooterRoomGatewayFlowResult? Flow => _state?.Launch.Flow;
        public static ShooterPlayModeSessionOptions Options => IsRunning || IsStarting || IsPaused ? _options.SessionOptions : ShooterPlayModeSessionOptions.Default;
        public static ShooterHostFrameInput LastInput => _lastInput;
        public static ShooterClientInputSubmitResult LastSubmitResult => _lastSubmitResult;
        public static ShooterClientFrameTickResult LastTickResult => _lastTickResult;
        public static ShooterClientGatewayInputSubmitResult LastGatewaySubmitResult => _gatewayInputQueue != null ? _gatewayInputQueue.LastResult : default;
        public static Exception? LastGatewayInputError => _gatewayInputQueue?.LastError;
        public static bool HasPendingGatewayInput => _gatewayInputQueue?.HasPending == true;
        public static bool HasQueuedGatewayInput => _gatewayInputQueue?.HasQueued == true;
        public static long GatewayInputSubmittedCount => _gatewayInputQueue?.SubmittedCount ?? 0L;
        public static long GatewayInputQueuedCount => _gatewayInputQueue?.QueuedCount ?? 0L;
        public static long GatewayInputReplacedCount => _gatewayInputQueue?.ReplacedCount ?? 0L;
        public static long GatewayInputCompletedCount => _gatewayInputQueue?.CompletedCount ?? 0L;
        public static long GatewayInputFailedCount => _gatewayInputQueue?.FailedCount ?? 0L;
        public static long GatewayInputResyncRequestedCount => _gatewayInputQueue?.ResyncRequestedCount ?? 0L;
        public static long StepCount => _stepCount;
        public static long RenderCount => _renderCount;
        public static SyncTimeAnchor LastLocalTimeAnchor => _timeAnchors?.LastLocalAnchor ?? default;
        public static SyncTimeAnchor LastRemoteTimeAnchor => _lastRemoteTimeAnchor;
        public static ShooterRemoteLatencyCompensationDiagnostics LastRemoteLatencyCompensationDiagnostics => _lastRemoteLatencyCompensationDiagnostics;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Uninstall();
            StateChanged = null;
        }

        public static Task<ShooterClientNetworkLaunchResult> StartOrReconnectAsync(
            ShooterPlayModeSessionOptions options,
            ShooterClientNetworkEndpoint endpoint,
            string sessionToken = ShooterRemoteStateSyncDefaults.DefaultSessionToken,
            string region = ShooterRemoteStateSyncDefaults.DefaultRegion,
            string serverId = ShooterRemoteStateSyncDefaults.DefaultServerId)
        {
            return StartAsync(ShooterRemoteStateSyncLaunchOptions.RestoreFirst(options, endpoint, sessionToken, region, serverId));
        }

        public static async Task<ShooterClientNetworkLaunchResult> StartAsync(ShooterRemoteStateSyncLaunchOptions launchOptions)
        {
            Install();
            StopRunningSession();
            _lastError = null;
            _isStarting = true;
            _options = launchOptions;
            _pausedResumeOptions = default;
            _isPaused = false;
            _isAutoReconnecting = false;
            NotifyStateChanged();

            try
            {
                var state = await StartSessionAsync(launchOptions).ConfigureAwait(false);
                _state = state;
                _gatewayInputQueue = new RemoteClientInputSubmitQueue<ShooterClientInputSubmitResult, ShooterClientGatewayInputSubmitResult>(
                    (local, timeout) => state.CoordinatorInputBridge.SubmitAcceptedInputAsync(local, timeout),
                    launchOptions.Timeout,
                    result => result.Remote.ShouldResync);
                _lastInput = default;
                _lastSubmitResult = default;
                _lastTickResult = default;
                _stepCount = 0;
                _renderCount = 0;
                _timeAnchors = ShooterTimeAnchorCoordinator.CreateLocal(_options.SessionOptions.TickRate);
                _lastRemoteTimeAnchor = state.Launch.Flow.RemoteTimeAnchorProjection.TimeAnchor;
                _lastRemoteLatencyCompensationDiagnostics = default;
                _lastError = null;
                return state.Launch;
            }
            catch (Exception ex)
            {
                StopRunningSession();
                _lastConnectionResult = null;
                _lastError = ex;
                throw;
            }
            finally
            {
                _isStarting = false;
                NotifyStateChanged();
            }
        }

        public static void PauseForReconnectValidation()
        {
            var state = _state;
            if (state == null || _isPaused)
            {
                return;
            }

            _pausedResumeOptions = BuildReconnectLaunchOptions(_options, state.Launch.Flow.RoomId);
            _gatewayInputQueue?.Reset();
            _gatewayInputQueue = null;
            state.Launcher.Close();
            _isPaused = true;
            NotifyStateChanged();
        }

        public static Task<ShooterClientNetworkLaunchResult> ResumeFromPauseAsync()
        {
            if (!_isPaused)
            {
                var runningLaunch = _state?.Launch;
                if (runningLaunch != null)
                {
                    return Task.FromResult(runningLaunch);
                }

                throw new InvalidOperationException("Shooter remote state-sync host is not paused.");
            }

            var resumeOptions = _pausedResumeOptions;
            if (string.IsNullOrWhiteSpace(resumeOptions.SessionToken))
            {
                resumeOptions = BuildReconnectLaunchOptions(_options, _state?.Launch.Flow.RoomId ?? _options.RoomId);
            }

            return StartAsync(resumeOptions);
        }

        public static void Stop()
        {
            StopRunningSession();
            _lastConnectionResult = null;
            _lastError = null;
            ViewSink.Clear();
            NotifyStateChanged();
        }

        public static void Uninstall()
        {
            StopRunningSession();
            UninstallPlayerLoop();
            Application.quitting -= OnApplicationQuitting;
            _lastConnectionResult = null;
            _lastError = null;
            ViewSink.Clear();
            NotifyStateChanged();
        }

        public static void Tick(float deltaSeconds)
        {
            try
            {
                TickRunningSession(deltaSeconds);
            }
            catch (Exception ex)
            {
                _lastError = ex;
                Debug.LogException(ex);
                Stop();
            }
        }

        public static void RebuildViews()
        {
            ViewSink.RebuildAll();
        }

        private static void Install()
        {
            InstallPlayerLoop();
            Application.quitting -= OnApplicationQuitting;
            Application.quitting += OnApplicationQuitting;
        }

        private static async Task<ShooterRemoteStateSyncRuntimeState> StartSessionAsync(ShooterRemoteStateSyncLaunchOptions launchOptions)
        {
            var runtimeWorld = ShooterBattleWorldSession.Create(
                $"remote-{launchOptions.SessionToken}-client",
                ShooterGameplayScenarioWorldHostFactory.Create(launchOptions.SessionOptions.GameplayScenario));
            var launcher = ShooterClientNetworkLauncher.Create(ShooterClientConnectionFactory.Tcp());

            try
            {
                var start = BuildStartPayload(launchOptions.SessionOptions);
                var launchSpec = launchOptions.RoomLaunchSpec
                    ?? ShooterRoomLaunchSpec.CreateDefault($"unity-{launchOptions.SessionOptions.ControlledPlayerId}");
                var connectionResult = await new ShooterRemoteStateSyncConnectionFlow().ConnectAsync(
                    launcher,
                    runtimeWorld.Runtime,
                    ShooterPresentationSessionContext.CreateDefault(),
                    launchOptions,
                    start,
                    launchSpec,
                    (uint)launchOptions.SessionOptions.ControlledPlayerId).ConfigureAwait(false);

                await RequestInitialFullStateSyncIfNeededAsync(
                    connectionResult,
                    launcher,
                    launchOptions.Timeout,
                    launchOptions.SessionOptions.TickRate).ConfigureAwait(false);

                _lastConnectionResult = connectionResult;
                connectionResult.Launch.Session.Presentation.ControlledPlayerId = launchOptions.SessionOptions.ControlledPlayerId;

                var coordinatorInputBridge = ShooterCoordinatorInputBridge.Create(
                    runtimeWorld.World,
                    connectionResult.Launch,
                    launchOptions.Endpoint,
                    launchOptions.SessionOptions.TickRate);

                return new ShooterRemoteStateSyncRuntimeState(runtimeWorld, launcher, connectionResult.Launch, coordinatorInputBridge);
            }
            catch
            {
                launcher.Dispose();
                runtimeWorld.Dispose();
                _lastConnectionResult = null;
                throw;
            }
        }

        private static void TickRunningSession(float deltaSeconds)
        {
            var state = _state;
            if (state == null || _isPaused || _isAutoReconnecting)
            {
                return;
            }

            state.Launcher.Tick(deltaSeconds);
            if (TryBeginAutoReconnectAfterSocketLoss(state))
            {
                return;
            }

            _gatewayInputQueue?.CompleteIfFinished();

            var localTimeAnchor = (_timeAnchors ??= ShooterTimeAnchorCoordinator.CreateLocal(_options.SessionOptions.TickRate)).AdvanceLocal();
            _lastRemoteTimeAnchor = state.Launch.Flow.RemoteTimeAnchorProjection.TimeAnchor;
            var input = InputSource.ReadInput(_options.SessionOptions.ControlledPlayerId);
            _lastInput = input;
            _stepCount++;

            var command = ShooterClientInputBuilder.CreateCommand(
                _options.SessionOptions.ControlledPlayerId,
                input.MoveX,
                input.MoveY,
                input.AimX,
                input.AimY,
                input.Fire,
                input.AttackSlot);

            _lastSubmitResult = state.Launch.Session.SubmitLocalInput(in command);
            _gatewayInputQueue?.SubmitOrQueue(_lastSubmitResult);

            _lastTickResult = state.Launch.Session.Tick(deltaSeconds);
            state.CoordinatorInputBridge.Tick(deltaSeconds);
            _gatewayInputQueue?.CompleteIfFinished();
            _lastRemoteLatencyCompensationDiagnostics = CreateRemoteLatencyCompensationDiagnostics();

            var frame = new ShooterHostPresentationFrame(
                state.Launch.Session.Presentation.ViewModel.Current,
                ShooterSnapshotViewBatch.Empty,
                false,
                _options.SessionOptions.ControlledPlayerId,
                _options.SessionOptions.WorldScale,
                null,
                state.Launch.GatewayConnection.LastPushResult,
                _lastRemoteTimeAnchor,
                localTimeAnchor,
                null,
                null,
                _lastRemoteLatencyCompensationDiagnostics,
                ShooterCrossLayerDiagnostics.From(
                    state.Launch.Session.FrameworkSnapshotPipelineDiagnostics,
                    state.Launch.GatewayConnection.LastPushResult,
                    _lastRemoteLatencyCompensationDiagnostics,
                    state.Launch.Session.Presentation.NeedsPureStateFullBaselineResync,
                    state.Launch.Session.Presentation.LastPureStateAppliedFrame,
                    state.Launch.Session.Presentation.LastPureStateResyncFrame),
                state.Launch.Session.Presentation.LastPureStateSyncDiagnostics,
                state.Launch.Session.Presentation.NeedsPureStateFullBaselineResync,
                state.Launch.Session.Presentation.LastPureStateResyncReason,
                state.Launch.Session.Presentation.LastPureStateAppliedFrame,
                state.Launch.Session.Presentation.LastPureStateAppliedStateHash,
                state.Launch.Session.Presentation.LastPureStateResyncFrame,
                state.Launch.Session.Presentation.LastPureStateResyncStateHash);
            ViewSink.Render(in frame);
            _renderCount++;
        }

        private static ShooterRemoteLatencyCompensationDiagnostics CreateRemoteLatencyCompensationDiagnostics()
        {
            var queue = _gatewayInputQueue;
            if (queue == null)
            {
                return default;
            }

            return ShooterRemoteLatencyCompensationDiagnostics.FromGatewayInput(
                queue.LastResult,
                queue.HasPending,
                queue.HasQueued,
                queue.SubmittedCount,
                queue.QueuedCount,
                queue.ReplacedCount,
                queue.CompletedCount,
                queue.FailedCount,
                queue.ResyncRequestedCount);
        }

        private static void StopRunningSession()
        {
            _gatewayInputQueue?.Reset();
            _gatewayInputQueue = null;
            _state?.Dispose();
            _state = null;
            _options = default;
            _pausedResumeOptions = default;
            _lastInput = default;
            _lastSubmitResult = default;
            _lastTickResult = default;
            _stepCount = 0;
            _renderCount = 0;
            _timeAnchors = null;
            _lastRemoteTimeAnchor = default;
            _lastRemoteLatencyCompensationDiagnostics = default;
            _isStarting = false;
            _isPaused = false;
            _isAutoReconnecting = false;
        }

        private static ShooterRemoteStateSyncLaunchOptions BuildReconnectLaunchOptions(ShooterRemoteStateSyncLaunchOptions source, string roomId)
        {
            return new ShooterRemoteStateSyncLaunchOptions(
                source.SessionOptions,
                source.Endpoint,
                source.SessionToken,
                source.Region,
                source.ServerId,
                ShooterRemoteStateSyncLaunchMode.RestoreOnly,
                source.Timeout,
                string.IsNullOrWhiteSpace(roomId) ? source.RoomId : roomId,
                source.RoomLaunchSpec);
        }

        private static bool TryBeginAutoReconnectAfterSocketLoss(ShooterRemoteStateSyncRuntimeState state)
        {
            if (_isStarting || _isPaused || _isAutoReconnecting)
            {
                return false;
            }

            var connection = state.Launcher.Connection;
            if (connection.IsConnected && connection.State == ConnectionState.Connected)
            {
                return false;
            }

            if (connection.State == ConnectionState.Connecting)
            {
                return false;
            }

            _isAutoReconnecting = true;
            _pausedResumeOptions = BuildReconnectLaunchOptions(_options, state.Launch.Flow.RoomId);
            _gatewayInputQueue?.Reset();
            _gatewayInputQueue = null;
            state.Launcher.Close();
            NotifyStateChanged();
            _ = ResumeAfterSocketLossAsync(_pausedResumeOptions);
            return true;
        }

        private static async Task ResumeAfterSocketLossAsync(ShooterRemoteStateSyncLaunchOptions resumeOptions)
        {
            try
            {
                await StartAsync(resumeOptions).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _lastError = ex;
                _isAutoReconnecting = false;
                Debug.LogException(ex);
                NotifyStateChanged();
            }
        }

        private static async Task RequestInitialFullStateSyncIfNeededAsync(
            ShooterRemoteStateSyncConnectionResult connectionResult,
            ShooterClientNetworkLauncher launcher,
            TimeSpan timeout,
            int tickRate)
        {
            if (!ShouldRequestInitialFullStateSync(connectionResult.EntryKind))
            {
                return;
            }

            var request = connectionResult.Launch.Battle.RequestFullSnapshotBaselineAsync(timeout);
            var deadline = DateTime.UtcNow + timeout;
            var fixedDeltaTime = 1f / Math.Max(1, tickRate);

            while (!request.IsCompleted)
            {
                if (DateTime.UtcNow >= deadline)
                {
                    throw new TimeoutException("Timed out requesting initial Shooter full-state sync.");
                }

                launcher.Tick(fixedDeltaTime);
                await Task.Delay(10).ConfigureAwait(false);
            }

            var result = await request.ConfigureAwait(false);
            if (!result.Success || !result.Accepted)
            {
                throw new InvalidOperationException($"Initial Shooter full-state sync request was rejected. Success={result.Success}, Accepted={result.Accepted}, Message={result.Message}");
            }
        }

        private static bool ShouldRequestInitialFullStateSync(ShooterRoomGatewayEntryKind entryKind)
        {
            return entryKind == ShooterRoomGatewayEntryKind.LateJoin
                || entryKind == ShooterRoomGatewayEntryKind.Reconnect;
        }

        private static ShooterStartGamePayload BuildStartPayload(ShooterPlayModeSessionOptions options)
        {
            var players = new List<ShooterStartPlayer>(options.PlayerCount);
            for (var i = 0; i < options.PlayerCount; i++)
            {
                players.Add(new ShooterStartPlayer(i + 1, $"P{i + 1}", i * 4f, 0f));
            }

            return new ShooterStartGamePayload(
                $"unity-remote-state-sync-{options.RandomSeed}",
                options.TickRate,
                options.RandomSeed,
                players.ToArray());
        }

        private static void TickFromPlayerLoop()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            Tick(Time.deltaTime);
        }

        private static void OnApplicationQuitting()
        {
            Uninstall();
        }

        private static void NotifyStateChanged()
        {
            StateChanged?.Invoke();
        }

        private static void InstallPlayerLoop()
        {
            if (_playerLoopInstalled)
            {
                return;
            }

            var loop = PlayerLoop.GetCurrentPlayerLoop();
            if (!InsertIntoUpdate(ref loop))
            {
                Debug.LogWarning("[ShooterRemoteStateSyncPlayModeHost] Failed to install PlayerLoop update node.");
                return;
            }

            PlayerLoop.SetPlayerLoop(loop);
            _playerLoopInstalled = true;
        }

        private static void UninstallPlayerLoop()
        {
            if (!_playerLoopInstalled)
            {
                return;
            }

            var loop = PlayerLoop.GetCurrentPlayerLoop();
            if (RemoveFromPlayerLoop(ref loop))
            {
                PlayerLoop.SetPlayerLoop(loop);
            }

            _playerLoopInstalled = false;
        }

        private static bool InsertIntoUpdate(ref PlayerLoopSystem root)
        {
            if (root.subSystemList == null)
            {
                return false;
            }

            for (var i = 0; i < root.subSystemList.Length; i++)
            {
                ref var system = ref root.subSystemList[i];
                if (system.type == typeof(Update))
                {
                    system.subSystemList = AppendOrReplace(system.subSystemList, new PlayerLoopSystem
                    {
                        type = typeof(ShooterRemoteStateSyncPlayerLoopNode),
                        updateDelegate = TickFromPlayerLoop
                    });
                    return true;
                }

                if (InsertIntoUpdate(ref system))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool RemoveFromPlayerLoop(ref PlayerLoopSystem root)
        {
            if (root.subSystemList == null || root.subSystemList.Length == 0)
            {
                return false;
            }

            for (var i = 0; i < root.subSystemList.Length; i++)
            {
                if (root.subSystemList[i].type == typeof(ShooterRemoteStateSyncPlayerLoopNode))
                {
                    root.subSystemList = RemoveAt(root.subSystemList, i);
                    return true;
                }

                var child = root.subSystemList[i];
                if (RemoveFromPlayerLoop(ref child))
                {
                    root.subSystemList[i] = child;
                    return true;
                }
            }

            return false;
        }

        private static PlayerLoopSystem[] AppendOrReplace(PlayerLoopSystem[]? systems, PlayerLoopSystem node)
        {
            if (systems == null || systems.Length == 0)
            {
                return new[] { node };
            }

            for (var i = 0; i < systems.Length; i++)
            {
                if (systems[i].type == node.type)
                {
                    systems[i] = node;
                    return systems;
                }
            }

            var result = new PlayerLoopSystem[systems.Length + 1];
            Array.Copy(systems, result, systems.Length);
            result[result.Length - 1] = node;
            return result;
        }

        private static PlayerLoopSystem[] RemoveAt(PlayerLoopSystem[] systems, int index)
        {
            if (systems.Length == 1)
            {
                return Array.Empty<PlayerLoopSystem>();
            }

            var result = new PlayerLoopSystem[systems.Length - 1];
            if (index > 0)
            {
                Array.Copy(systems, 0, result, 0, index);
            }

            if (index < systems.Length - 1)
            {
                Array.Copy(systems, index + 1, result, index, systems.Length - index - 1);
            }

            return result;
        }

        private sealed class ShooterRemoteStateSyncRuntimeState : IDisposable
        {
            private bool _disposed;

            public ShooterRemoteStateSyncRuntimeState(
                ShooterBattleWorldSession runtimeWorld,
                ShooterClientNetworkLauncher launcher,
                ShooterClientNetworkLaunchResult launch,
                ShooterCoordinatorInputBridge coordinatorInputBridge)
            {
                RuntimeWorld = runtimeWorld ?? throw new ArgumentNullException(nameof(runtimeWorld));
                Launcher = launcher ?? throw new ArgumentNullException(nameof(launcher));
                Launch = launch ?? throw new ArgumentNullException(nameof(launch));
                CoordinatorInputBridge = coordinatorInputBridge ?? throw new ArgumentNullException(nameof(coordinatorInputBridge));
            }

            public ShooterBattleWorldSession RuntimeWorld { get; }
            public ShooterClientNetworkLauncher Launcher { get; }
            public ShooterClientNetworkLaunchResult Launch { get; }
            public ShooterCoordinatorInputBridge CoordinatorInputBridge { get; }

            public void Dispose()
            {
                if (_disposed)
                {
                    return;
                }

                _disposed = true;
                CoordinatorInputBridge.Dispose();
                Launcher.Dispose();
                RuntimeWorld.Dispose();
            }
        }

        private struct ShooterRemoteStateSyncPlayerLoopNode
        {
        }
    }
}
