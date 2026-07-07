#nullable enable

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AbilityKit.Demo.Shooter.Runtime;
using AbilityKit.Network.Runtime;
using AbilityKit.Network.Runtime.Sync;
using AbilityKit.Protocol.Shooter;

namespace AbilityKit.Demo.Shooter.View
{
    /// <summary>
    /// <see cref="NetworkSyncModel.AuthoritativeInterpolation"/> 客户端控制器原型。
    /// 本地玩家仍走现有预测链路
    /// （<see cref="ShooterClientFrameSyncCoordinator"/> + <see cref="ShooterClientInputCoordinator"/>），
    /// 但远端权威快照不会导入或触发回滚，而是按服务器 tick 缓冲，并以固定插值延迟落后于最新权威样本播放，
    /// 从而让远端 actor 平滑移动且不校正本地模拟。
    /// </summary>
    public sealed class ShooterClientAuthoritativeInterpolationSyncController : IShooterClientSyncController, IInterpolationDiagnosticsProvider
    {
        private readonly IShooterBattleRuntimePort _runtime;
        private readonly ShooterClientSyncCore _core;
        private readonly ShooterPresentationFacade _presentation;
        private readonly ShooterGatewaySnapshotDecoder _decoder;
        private readonly RemoteInterpolationPlayback<ShooterRemoteSnapshotSample> _playback;
        private readonly ShooterRemoteSnapshotProjector _projector = new ShooterRemoteSnapshotProjector();
        private readonly NetworkSyncModel _syncModel;
        private ShooterPlayerCommand _lastPredictedCommand;
        private ShooterStateSyncPredictionState _predictionState = ShooterStateSyncPredictionState.Empty;

        public ShooterClientAuthoritativeInterpolationSyncController(
            IShooterBattleRuntimePort runtime,
            ShooterPresentationFacade presentation,
            int tickRate,
            ShooterGatewaySnapshotDecoder? decoder,
            IShooterRoomGatewayClient? gateway)
            : this(runtime, presentation, tickRate, decoder, gateway, InterpolationConfig.Default)
        {
        }

        public ShooterClientAuthoritativeInterpolationSyncController(
            IShooterBattleRuntimePort runtime,
            ShooterPresentationFacade presentation,
            int tickRate,
            ShooterGatewaySnapshotDecoder? decoder,
            IShooterRoomGatewayClient? gateway,
            InterpolationConfig config)
            : this(runtime, presentation, tickRate, decoder, gateway, config, NetworkSyncModel.AuthoritativeInterpolation)
        {
        }

        public ShooterClientAuthoritativeInterpolationSyncController(
            IShooterBattleRuntimePort runtime,
            ShooterPresentationFacade presentation,
            int tickRate,
            ShooterGatewaySnapshotDecoder? decoder,
            IShooterRoomGatewayClient? gateway,
            InterpolationConfig config,
            NetworkSyncModel syncModel)
        {
            _runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
            _presentation = presentation ?? throw new ArgumentNullException(nameof(presentation));
            _core = new ShooterClientSyncCore(_runtime, presentation, tickRate, decoder, gateway);
            _decoder = decoder ?? new ShooterGatewaySnapshotDecoder();
            _playback = new RemoteInterpolationPlayback<ShooterRemoteSnapshotSample>(config);
            _syncModel = syncModel;
        }

        public NetworkSyncModel SyncModel => _syncModel;

        public bool IsStarted => _core.IsStarted;

        public int CurrentFrame => _core.CurrentFrame;

        public ShooterClientFrameSyncController FrameSync => _core.FrameSync;

        public ShooterClientInputCoordinator InputCoordinator => _core.InputCoordinator;

        public ShooterFrameworkSnapshotPipelineDiagnostics FrameworkSnapshotPipelineDiagnostics => _core.FrameworkSnapshotPipelineDiagnostics;

        public ShooterClientReconciliationResult LastReconciliationResult => _core.LastReconciliationResult;

        public bool NeedsFullSnapshotResync => _core.NeedsFullSnapshotResync;

        public ShooterClientRecoveryState RecoveryState => _core.RecoveryState;

        public AbilityKit.Network.Runtime.Sync.FastReconnectPhase FastReconnectPhase => _core.FastReconnectPhase;

        public IReadOnlyList<SyncHealthEvent> LastFastReconnectHealthEvents => _core.LastFastReconnectHealthEvents;

        public ShooterClientResyncReason LastResyncReason => _core.LastResyncReason;

        public int LastResyncClientFrame => _core.LastResyncClientFrame;

        public int LastResyncAuthoritativeFrame => _core.LastResyncAuthoritativeFrame;

        public uint LastResyncClientStateHash => _core.LastResyncClientStateHash;

        public uint LastResyncAuthoritativeStateHash => _core.LastResyncAuthoritativeStateHash;

        public bool HasGateway => _core.HasGateway;

        /// <summary>当前为插值缓冲的远端权威快照数量。</summary>
        public int BufferedRemoteSnapshotCount => _playback.BufferedSampleCount;

        /// <summary>当前延迟远端播放时间，单位为时间线 tick。</summary>
        public long RemotePlaybackTicks => _playback.PlaybackTicks;

        /// <summary>当前本地估算的权威服务器时间，单位为时间线 tick。</summary>
        public long EstimatedServerTicks => _playback.EstimatedServerTicks;

        /// <summary>是否已经向表现层发布过至少一帧远端插值结果。</summary>
        public bool HasPublishedRemoteFrame => _playback.HasPublished;

        /// <summary>
        /// 最近一次发布尝试是否发现延迟播放时间已经超过最新缓冲快照
        /// <see cref="InterpolationConfig.MaxExtrapolationTicks"/> 以上。
        /// 表示远端缓冲已经饥饿（例如快照停止到达），播放会保持最后一个权威姿态，而不是继续外推。
        /// </summary>
        public bool IsRemotePlaybackStarved => _playback.IsStarved;

        public ShooterStateSyncPredictionState PredictionState => _predictionState;

        public bool StartGame(in ShooterStartGamePayload startGame)
        {
            return _core.StartGame(in startGame);
        }

        public ShooterClientInputSubmitResult SubmitLocalInput(int playerId, float moveX, float moveY, float aimX, float aimY, bool fire)
        {
            var command = new ShooterPlayerCommand(playerId, moveX, moveY, aimX, aimY, fire);
            return SubmitLocalInput(in command);
        }

        public ShooterClientInputSubmitResult SubmitLocalInput(in ShooterPlayerCommand command)
        {
            var result = _core.SubmitLocalInput(in command);
            if (command.PlayerId > 0)
            {
                _lastPredictedCommand = command;
                RefreshPredictedPose(command.PlayerId, CurrentFrame, EstimatedServerTicks);
            }

            return result;
        }

        public Task<ShooterClientGatewayInputSubmitResult> SubmitLocalInputToGatewayAsync(
            ShooterGatewayBattleInputContext context,
            ShooterPlayerCommand command,
            TimeSpan? timeout = null,
            CancellationToken cancellationToken = default)
        {
            return _core.SubmitLocalInputToGatewayAsync(context, command, timeout, cancellationToken);
        }

        public Task<ShooterClientGatewayInputSubmitResult> SubmitAcceptedInputToGatewayAsync(
            ShooterGatewayBattleInputContext context,
            ShooterClientInputSubmitResult local,
            TimeSpan? timeout = null,
            CancellationToken cancellationToken = default)
        {
            return _core.SubmitAcceptedInputToGatewayAsync(context, local, timeout, cancellationToken);
        }

        public ShooterClientFrameTickResult Tick(float deltaTime)
        {
            var result = _core.Tick(deltaTime);
            RefreshPredictedPose(_lastPredictedCommand.PlayerId, result.Frame, EstimatedServerTicks);
            _playback.Advance(deltaTime);
            PublishInterpolatedRemoteFrame();
            return result;
        }

        public ShooterClientFrameTickResult CatchUpToFrame(int targetFrame)
        {
            return _core.CatchUpToFrame(targetFrame);
        }

        public bool TryEnterCatchUp(int authoritativeFrame)
        {
            return _core.TryEnterCatchUp(authoritativeFrame);
        }

        /// <summary>
        /// 为延迟插值缓冲远端权威快照。不同于预测回滚模型，它不会把打包状态导入本地运行时，
        /// 也不会触发回滚；这里只写入插值缓冲与时间线。
        /// </summary>
        public ShooterSnapshotApplyResult ApplyGatewayPush(uint opCode, ArraySegment<byte> payload)
        {
            if (!_decoder.IsSnapshotPush(opCode))
            {
                return ShooterSnapshotApplyResult.Ignored;
            }

            var snapshot = _decoder.Decode(payload);
            if (snapshot.PureStateSnapshot.HasValue)
            {
                var pureStateResult = _presentation.ApplyPureStateGatewaySnapshot(in snapshot);
                return ShooterSnapshotApplyResults.FromPureStateResult(pureStateResult);
            }

            return BufferRemoteSnapshot(in snapshot);
        }

        /// <summary>
        /// 为延迟插值缓冲一个已经解码的网关快照。
        /// </summary>
        public ShooterSnapshotApplyResult BufferRemoteSnapshot(in ShooterGatewaySnapshot snapshot)
        {
            var sample = new ShooterRemoteSnapshotSample(
                snapshot.WorldId,
                snapshot.Frame,
                snapshot.ServerTicks,
                snapshot.Actors);

            if (!_playback.Observe(sample))
            {
                return ShooterSnapshotApplyResult.IgnoredStaleSnapshot;
            }

            ObserveAuthoritativeProjectileActions(in snapshot);
            return ShooterSnapshotApplyResult.AppliedActorSnapshot;
        }

        private void RefreshPredictedPose(int playerId, int frame, long serverTicks)
        {
            if (playerId <= 0 || !_runtime.TryGetPlayer(playerId, out var player))
            {
                return;
            }

            _predictionState = _predictionState.WithPredictedPose(
                player.PlayerId,
                player.X,
                player.Y,
                player.AimX,
                player.AimY,
                frame,
                serverTicks);
        }

        private void ObserveAuthoritativeProjectileActions(in ShooterGatewaySnapshot snapshot)
        {
            if (!snapshot.PackedSnapshot.HasValue)
            {
                return;
            }

            var chunks = snapshot.PackedSnapshot.Value.ComponentChunks;
            if (chunks == null)
            {
                return;
            }

            for (int chunkIndex = 0; chunkIndex < chunks.Length; chunkIndex++)
            {
                var chunk = chunks[chunkIndex];
                if (chunk.ComponentKind != ShooterPackedComponentKinds.EntityLifecycle || chunk.EntityKind != ShooterPackedEntityKinds.Projectile)
                {
                    continue;
                }

                var count = Math.Min(chunk.Count, Math.Min(SafeLength(chunk.EntityIds), SafeLength(chunk.OwnerIds)));
                for (int i = 0; i < count; i++)
                {
                    var ownerPlayerId = chunk.OwnerIds[i];
                    if (ownerPlayerId <= 0)
                    {
                        continue;
                    }

                    SetPredictedAction(
                        ownerPlayerId,
                        ShooterStateSyncPredictedAction.Fire,
                        snapshot.Frame,
                        snapshot.ServerTicks,
                        CurrentFrame,
                        needsCatchUp: true,
                        ownerPlayerId,
                        0,
                        chunk.EntityIds[i]);
                }
            }
        }

        private void SetPredictedAction(
            int playerId,
            ShooterStateSyncPredictedAction action,
            int sourceFrame,
            long sourceServerTicks,
            int playbackFrame,
            bool needsCatchUp,
            int sourcePlayerId,
            int targetPlayerId,
            int bulletId)
        {
            if (playerId <= 0 || action == ShooterStateSyncPredictedAction.None)
            {
                return;
            }

            _predictionState = _predictionState.WithAction(
                playerId,
                action,
                sourceFrame,
                sourceServerTicks,
                playbackFrame,
                needsCatchUp,
                Math.Max(0, playbackFrame - sourceFrame),
                sourcePlayerId,
                targetPlayerId,
                bulletId);
        }

        private static int SafeLength<T>(T[]? values)
        {
            return values?.Length ?? 0;
        }

        private void PublishInterpolatedRemoteFrame()
        {
            // 框架播放层负责缓冲、时间线以及外推/饥饿策略；
            // Shooter 控制器只提供“投影 + 应用到表现层”这一半循环。
            if (!_playback.TrySample(out var interpolation))
            {
                return;
            }

            var projected = _projector.Project(in interpolation);
            _presentation.ApplyInterpolatedGatewaySnapshot(in projected);
        }

        private static IReadOnlyList<SyncHealthEvent> MergeHealthEvents(
            IReadOnlyList<SyncHealthEvent> primary,
            IReadOnlyList<SyncHealthEvent> secondary)
        {
            if (primary.Count == 0)
            {
                return secondary;
            }

            if (secondary.Count == 0)
            {
                return primary;
            }

            var merged = new SyncHealthEvent[primary.Count + secondary.Count];
            for (int i = 0; i < primary.Count; i++)
            {
                merged[i] = primary[i];
            }

            for (int i = 0; i < secondary.Count; i++)
            {
                merged[primary.Count + i] = secondary[i];
            }

            return merged;
        }

        /// <summary>
        /// 采集当前插值播放健康状态，用于诊断与 smoke 输出。
        /// </summary>
        public InterpolationDiagnostics GetInterpolationDiagnostics()
        {
            return _playback.GetDiagnostics();
        }

        // --- IClientSyncStrategy<ShooterPlayerCommand, ShooterRemoteSnapshotSample> ---
        // 显式框架契约接口，映射到现有示例行为。

        SyncTickResult IClientSyncStrategy<ShooterPlayerCommand, ShooterRemoteSnapshotSample>.Tick(float deltaSeconds)
        {
            return ShooterClientSyncStrategyMapping.ToSyncTickResult(Tick(deltaSeconds));
        }

        void IClientSyncStrategy<ShooterPlayerCommand, ShooterRemoteSnapshotSample>.SubmitInput(in ShooterPlayerCommand input)
        {
            SubmitLocalInput(in input);
        }

        void IClientSyncStrategy<ShooterPlayerCommand, ShooterRemoteSnapshotSample>.ObserveRemote(in ShooterRemoteSnapshotSample sample)
        {
            // 对权威插值来说，观察远端样本会写入延迟播放缓冲
            // （与 BufferRemoteSnapshot/ApplyGatewayPush 使用同一路径），绝不会进入本地模拟。
            _playback.Observe(sample);
        }

        SyncReconciliationReport IClientSyncStrategy<ShooterPlayerCommand, ShooterRemoteSnapshotSample>.GetReconciliationReport()
        {
            return ShooterClientSyncStrategyMapping.ToReconciliationReport(this);
        }
    }

    public enum ShooterStateSyncPredictedAction
    {
        None = 0,
        Fire = 1,
        Hit = 2
    }

    public readonly struct ShooterStateSyncPredictionState
    {
        public static readonly ShooterStateSyncPredictionState Empty = new ShooterStateSyncPredictionState(
            0,
            false,
            0f,
            0f,
            0f,
            0f,
            0,
            0L,
            0,
            ShooterStateSyncPredictedAction.None,
            0,
            0L,
            0,
            false,
            0,
            0,
            0,
            0);

        public readonly int PlayerId;
        public readonly bool HasPredictedPose;
        public readonly float PredictedX;
        public readonly float PredictedY;
        public readonly float PredictedAimX;
        public readonly float PredictedAimY;
        public readonly int PredictedFrame;
        public readonly long PredictedServerTicks;
        public readonly int ActionPlayerId;
        public readonly ShooterStateSyncPredictedAction Action;
        public readonly int ActionSourceFrame;
        public readonly long ActionSourceServerTicks;
        public readonly int ActionPlaybackFrame;
        public readonly bool NeedsActionCatchUp;
        public readonly int ActionCatchUpFrames;
        public readonly int ActionSourcePlayerId;
        public readonly int ActionTargetPlayerId;
        public readonly int ActionBulletId;

        public ShooterStateSyncPredictionState(
            int playerId,
            bool hasPredictedPose,
            float predictedX,
            float predictedY,
            float predictedAimX,
            float predictedAimY,
            int predictedFrame,
            long predictedServerTicks,
            int actionPlayerId,
            ShooterStateSyncPredictedAction action,
            int actionSourceFrame,
            long actionSourceServerTicks,
            int actionPlaybackFrame,
            bool needsActionCatchUp,
            int actionCatchUpFrames,
            int actionSourcePlayerId,
            int actionTargetPlayerId,
            int actionBulletId)
        {
            PlayerId = playerId;
            HasPredictedPose = hasPredictedPose;
            PredictedX = predictedX;
            PredictedY = predictedY;
            PredictedAimX = predictedAimX;
            PredictedAimY = predictedAimY;
            PredictedFrame = predictedFrame;
            PredictedServerTicks = predictedServerTicks;
            ActionPlayerId = actionPlayerId;
            Action = action;
            ActionSourceFrame = actionSourceFrame;
            ActionSourceServerTicks = actionSourceServerTicks;
            ActionPlaybackFrame = actionPlaybackFrame;
            NeedsActionCatchUp = needsActionCatchUp;
            ActionCatchUpFrames = actionCatchUpFrames;
            ActionSourcePlayerId = actionSourcePlayerId;
            ActionTargetPlayerId = actionTargetPlayerId;
            ActionBulletId = actionBulletId;
        }

        public ShooterStateSyncPredictionState WithPredictedPose(
            int playerId,
            float x,
            float y,
            float aimX,
            float aimY,
            int frame,
            long serverTicks)
        {
            return new ShooterStateSyncPredictionState(
                playerId,
                true,
                x,
                y,
                aimX,
                aimY,
                frame,
                serverTicks,
                ActionPlayerId,
                Action,
                ActionSourceFrame,
                ActionSourceServerTicks,
                ActionPlaybackFrame,
                NeedsActionCatchUp,
                ActionCatchUpFrames,
                ActionSourcePlayerId,
                ActionTargetPlayerId,
                ActionBulletId);
        }

        public ShooterStateSyncPredictionState WithAction(
            int playerId,
            ShooterStateSyncPredictedAction action,
            int sourceFrame,
            long sourceServerTicks,
            int playbackFrame,
            bool needsCatchUp,
            int catchUpFrames,
            int sourcePlayerId,
            int targetPlayerId,
            int bulletId)
        {
            return new ShooterStateSyncPredictionState(
                PlayerId,
                HasPredictedPose,
                PredictedX,
                PredictedY,
                PredictedAimX,
                PredictedAimY,
                PredictedFrame,
                PredictedServerTicks,
                playerId,
                action,
                sourceFrame,
                sourceServerTicks,
                playbackFrame,
                needsCatchUp,
                catchUpFrames,
                sourcePlayerId,
                targetPlayerId,
                bulletId);
        }
    }
}
