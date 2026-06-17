#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;
using AbilityKit.Demo.Shooter.Runtime;
using AbilityKit.Network.Runtime;
using AbilityKit.Network.Runtime.Sync;
using AbilityKit.Protocol.Shooter;

namespace AbilityKit.Demo.Shooter.View
{
    /// <summary>
    /// <see cref="NetworkSyncModel.AuthoritativeInterpolation"/> client controller prototype.
    /// The local player still runs through the existing prediction chain
    /// (<see cref="ShooterClientFrameSyncCoordinator"/> + <see cref="ShooterClientInputCoordinator"/>),
    /// but remote authoritative snapshots are not imported or rolled back. Instead they are buffered
    /// by server ticks and replayed a fixed interpolation delay behind the newest authoritative
    /// sample, so remote actors move smoothly without correcting the local simulation.
    /// </summary>
    public sealed class ShooterClientAuthoritativeInterpolationSyncController : IShooterClientSyncController, IInterpolationDiagnosticsProvider
    {
        private readonly IShooterBattleRuntimePort _runtime;
        private readonly ShooterClientFrameSyncCoordinator _frameSync;
        private readonly ShooterClientInputCoordinator _input;
        private readonly ShooterPresentationFacade _presentation;
        private readonly ShooterGatewaySnapshotDecoder _decoder;
        private readonly RemoteInterpolationPlayback<ShooterRemoteSnapshotSample> _playback;
        private readonly ShooterRemoteSnapshotProjector _projector = new ShooterRemoteSnapshotProjector();
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
        {
            _runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
            _presentation = presentation ?? throw new ArgumentNullException(nameof(presentation));
            _frameSync = new ShooterClientFrameSyncCoordinator(_runtime, presentation, tickRate, decoder);
            _input = new ShooterClientInputCoordinator(_frameSync, gateway);
            _decoder = decoder ?? new ShooterGatewaySnapshotDecoder();
            _playback = new RemoteInterpolationPlayback<ShooterRemoteSnapshotSample>(config);
        }

        public NetworkSyncModel SyncModel => NetworkSyncModel.AuthoritativeInterpolation;

        public bool IsStarted => _frameSync.IsStarted;

        public int CurrentFrame => _frameSync.CurrentFrame;

        public ShooterClientFrameSyncController FrameSync => _frameSync.Controller;

        public ShooterClientFrameSyncCoordinator FrameSyncCoordinator => _frameSync;

        public ShooterClientInputCoordinator InputCoordinator => _input;

        public ShooterClientReconciliationResult LastReconciliationResult => _frameSync.LastReconciliationResult;

        public bool NeedsFullSnapshotResync => _frameSync.NeedsFullSnapshotResync;

        public ShooterClientRecoveryState RecoveryState => _frameSync.RecoveryState;

        public AbilityKit.Network.Runtime.Sync.FastReconnectPhase FastReconnectPhase => _frameSync.FastReconnectPhase;

        public System.Collections.Generic.IReadOnlyList<AbilityKit.Network.Runtime.Sync.SyncHealthEvent> LastFastReconnectHealthEvents
            => _frameSync.LastFastReconnectHealthEvents;

        public ShooterClientResyncReason LastResyncReason => _frameSync.LastResyncReason;

        public int LastResyncClientFrame => _frameSync.LastResyncClientFrame;

        public int LastResyncAuthoritativeFrame => _frameSync.LastResyncAuthoritativeFrame;

        public uint LastResyncClientStateHash => _frameSync.LastResyncClientStateHash;

        public uint LastResyncAuthoritativeStateHash => _frameSync.LastResyncAuthoritativeStateHash;

        public bool HasGateway => _input.HasGateway;

        /// <summary>Number of remote authoritative snapshots currently buffered for interpolation.</summary>
        public int BufferedRemoteSnapshotCount => _playback.BufferedSampleCount;

        /// <summary>The current delayed remote playback time, in timeline ticks.</summary>
        public long RemotePlaybackTicks => _playback.PlaybackTicks;

        /// <summary>The current local estimate of authoritative server time, in timeline ticks.</summary>
        public long EstimatedServerTicks => _playback.EstimatedServerTicks;

        /// <summary>Whether at least one remote interpolation frame has been published to presentation.</summary>
        public bool HasPublishedRemoteFrame => _playback.HasPublished;

        /// <summary>
        /// Whether the most recent publish attempt found the delayed playback time running past the
        /// newest buffered snapshot by more than <see cref="InterpolationConfig.MaxExtrapolationTicks"/>.
        /// Indicates the remote buffer is starved (e.g. snapshots stopped arriving) and playback is
        /// holding the last authoritative pose rather than extrapolating further.
        /// </summary>
        public bool IsRemotePlaybackStarved => _playback.IsStarved;

        public ShooterStateSyncPredictionState PredictionState => _predictionState;

        public bool StartGame(in ShooterStartGamePayload startGame)
        {
            return _frameSync.StartGame(in startGame);
        }

        public ShooterClientInputSubmitResult SubmitLocalInput(int playerId, float moveX, float moveY, float aimX, float aimY, bool fire)
        {
            var command = new ShooterPlayerCommand(playerId, moveX, moveY, aimX, aimY, fire);
            return SubmitLocalInput(in command);
        }

        public ShooterClientInputSubmitResult SubmitLocalInput(in ShooterPlayerCommand command)
        {
            var result = _input.SubmitLocalInput(in command);
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
            return _input.SubmitLocalInputToGatewayAsync(context, command, timeout, cancellationToken);
        }

        public Task<ShooterClientGatewayInputSubmitResult> SubmitAcceptedInputToGatewayAsync(
            ShooterGatewayBattleInputContext context,
            ShooterClientInputSubmitResult local,
            TimeSpan? timeout = null,
            CancellationToken cancellationToken = default)
        {
            return _input.SubmitAcceptedInputToGatewayAsync(context, local, timeout, cancellationToken);
        }

        public ShooterClientFrameTickResult Tick(float deltaTime)
        {
            var result = _frameSync.Tick(deltaTime);
            RefreshPredictedPose(_lastPredictedCommand.PlayerId, result.Frame, EstimatedServerTicks);
            _playback.Advance(deltaTime);
            PublishInterpolatedRemoteFrame();
            return result;
        }

        public ShooterClientFrameTickResult CatchUpToFrame(int targetFrame)
        {
            return _frameSync.CatchUpToFrame(targetFrame);
        }

        public bool TryEnterCatchUp(int authoritativeFrame)
        {
            return _frameSync.TryEnterCatchUp(authoritativeFrame);
        }

        /// <summary>
        /// Buffers a remote authoritative snapshot for delayed interpolation. Unlike the predict
        /// rollback model this never imports packed state into the local runtime or triggers rollback;
        /// it only feeds the interpolation buffer and timeline.
        /// </summary>
        public ShooterSnapshotApplyResult ApplyGatewayPush(uint opCode, ArraySegment<byte> payload)
        {
            if (!_decoder.IsSnapshotPush(opCode))
            {
                return ShooterSnapshotApplyResult.Ignored;
            }

            var snapshot = _decoder.Decode(payload);
            return BufferRemoteSnapshot(in snapshot);
        }

        /// <summary>
        /// Buffers an already decoded gateway snapshot for delayed interpolation.
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
            // The framework playback owns the buffer + timeline + extrapolation/starvation policy; the
            // Shooter controller only supplies the "project + apply to presentation" half of the loop.
            if (!_playback.TrySample(out var interpolation))
            {
                return;
            }

            var projected = _projector.Project(in interpolation);
            _presentation.ApplyInterpolatedGatewaySnapshot(in projected);
        }

        /// <summary>
        /// Captures the current interpolation playback health for diagnostics / smoke output.
        /// </summary>
        public InterpolationDiagnostics GetInterpolationDiagnostics()
        {
            return _playback.GetDiagnostics();
        }

        // --- IClientSyncStrategy<ShooterPlayerCommand, ShooterRemoteSnapshotSample> ---
        // Explicit framework-contract surface that maps onto the existing demo behaviour.

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
            // For authoritative interpolation, observing a remote sample feeds the delayed playback
            // buffer (the same path BufferRemoteSnapshot/ApplyGatewayPush use), never the local sim.
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
