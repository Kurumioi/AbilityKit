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
    /// Hybrid controller for <see cref="NetworkSyncModel.HybridHeroPrediction"/>.
    /// Local simulation and authoritative correction still delegate to predict/rollback, while decoded
    /// remote actor samples are buffered and replayed through delayed authoritative interpolation.
    /// </summary>
    public sealed class ShooterClientHybridHeroPredictionSyncController : IShooterClientSyncController, IInterpolationDiagnosticsProvider
    {
        private readonly ShooterClientPredictRollbackSyncController _rollback;
        private readonly ShooterPresentationFacade _presentation;
        private readonly ShooterGatewaySnapshotDecoder _decoder;
        private readonly RemoteInterpolationPlayback<ShooterRemoteSnapshotSample> _playback;
        private readonly ShooterRemoteSnapshotProjector _projector = new ShooterRemoteSnapshotProjector();

        public ShooterClientHybridHeroPredictionSyncController(
            IShooterBattleRuntimePort runtime,
            ShooterPresentationFacade presentation,
            int tickRate,
            ShooterGatewaySnapshotDecoder? decoder,
            IShooterRoomGatewayClient? gateway)
            : this(runtime, presentation, tickRate, decoder, gateway, InterpolationConfig.Default)
        {
        }

        public ShooterClientHybridHeroPredictionSyncController(
            IShooterBattleRuntimePort runtime,
            ShooterPresentationFacade presentation,
            int tickRate,
            ShooterGatewaySnapshotDecoder? decoder,
            IShooterRoomGatewayClient? gateway,
            InterpolationConfig config)
        {
            _presentation = presentation ?? throw new ArgumentNullException(nameof(presentation));
            _decoder = decoder ?? new ShooterGatewaySnapshotDecoder();
            _rollback = new ShooterClientPredictRollbackSyncController(
                runtime,
                presentation,
                tickRate,
                decoder,
                gateway);
            _playback = new RemoteInterpolationPlayback<ShooterRemoteSnapshotSample>(config);
        }

        public NetworkSyncModel SyncModel => NetworkSyncModel.HybridHeroPrediction;

        public bool IsStarted => _rollback.IsStarted;

        public int CurrentFrame => _rollback.CurrentFrame;

        public ShooterClientFrameSyncController FrameSync => _rollback.FrameSync;

        public ShooterClientFrameSyncCoordinator FrameSyncCoordinator => _rollback.FrameSyncCoordinator;

        public ShooterClientInputCoordinator InputCoordinator => _rollback.InputCoordinator;

        public ShooterClientReconciliationResult LastReconciliationResult => _rollback.LastReconciliationResult;

        public bool NeedsFullSnapshotResync => _rollback.NeedsFullSnapshotResync;

        public ShooterClientRecoveryState RecoveryState => _rollback.RecoveryState;

        public FastReconnectPhase FastReconnectPhase => _rollback.FastReconnectPhase;

        public IReadOnlyList<SyncHealthEvent> LastFastReconnectHealthEvents => _rollback.LastFastReconnectHealthEvents;

        public ShooterClientResyncReason LastResyncReason => _rollback.LastResyncReason;

        public int LastResyncClientFrame => _rollback.LastResyncClientFrame;

        public int LastResyncAuthoritativeFrame => _rollback.LastResyncAuthoritativeFrame;

        public uint LastResyncClientStateHash => _rollback.LastResyncClientStateHash;

        public uint LastResyncAuthoritativeStateHash => _rollback.LastResyncAuthoritativeStateHash;

        public bool HasGateway => _rollback.HasGateway;

        /// <summary>Number of remote authoritative snapshots currently buffered for interpolation.</summary>
        public int BufferedRemoteSnapshotCount => _playback.BufferedSampleCount;

        /// <summary>The current delayed remote playback time, in timeline ticks.</summary>
        public long RemotePlaybackTicks => _playback.PlaybackTicks;

        /// <summary>The current local estimate of authoritative server time, in timeline ticks.</summary>
        public long EstimatedServerTicks => _playback.EstimatedServerTicks;

        /// <summary>Whether at least one remote interpolation frame has been published to presentation.</summary>
        public bool HasPublishedRemoteFrame => _playback.HasPublished;

        /// <summary>Whether remote interpolation is holding the newest sample because the buffer is starved.</summary>
        public bool IsRemotePlaybackStarved => _playback.IsStarved;

        public bool StartGame(in ShooterStartGamePayload startGame)
        {
            return _rollback.StartGame(in startGame);
        }

        public ShooterClientInputSubmitResult SubmitLocalInput(int playerId, float moveX, float moveY, float aimX, float aimY, bool fire)
        {
            return _rollback.SubmitLocalInput(playerId, moveX, moveY, aimX, aimY, fire);
        }

        public ShooterClientInputSubmitResult SubmitLocalInput(in ShooterPlayerCommand command)
        {
            return _rollback.SubmitLocalInput(in command);
        }

        public Task<ShooterClientGatewayInputSubmitResult> SubmitLocalInputToGatewayAsync(
            ShooterGatewayBattleInputContext context,
            ShooterPlayerCommand command,
            TimeSpan? timeout = null,
            CancellationToken cancellationToken = default)
        {
            return _rollback.SubmitLocalInputToGatewayAsync(context, command, timeout, cancellationToken);
        }

        public Task<ShooterClientGatewayInputSubmitResult> SubmitAcceptedInputToGatewayAsync(
            ShooterGatewayBattleInputContext context,
            ShooterClientInputSubmitResult local,
            TimeSpan? timeout = null,
            CancellationToken cancellationToken = default)
        {
            return _rollback.SubmitAcceptedInputToGatewayAsync(context, local, timeout, cancellationToken);
        }

        public ShooterClientFrameTickResult Tick(float deltaTime)
        {
            var result = _rollback.Tick(deltaTime);
            _playback.Advance(deltaTime);
            PublishInterpolatedRemoteFrame();
            return result;
        }

        public ShooterClientFrameTickResult CatchUpToFrame(int targetFrame)
        {
            return _rollback.CatchUpToFrame(targetFrame);
        }

        public bool TryEnterCatchUp(int authoritativeFrame)
        {
            return _rollback.TryEnterCatchUp(authoritativeFrame);
        }

        public ShooterSnapshotApplyResult ApplyGatewayPush(uint opCode, ArraySegment<byte> payload)
        {
            var rollbackResult = _rollback.ApplyGatewayPush(opCode, payload);
            if (!_decoder.IsSnapshotPush(opCode))
            {
                return rollbackResult;
            }

            var snapshot = _decoder.Decode(payload);
            var interpolationResult = BufferRemoteSnapshot(in snapshot);
            return rollbackResult == ShooterSnapshotApplyResult.Ignored ? interpolationResult : rollbackResult;
        }

        /// <summary>Buffers a decoded remote authoritative snapshot for delayed interpolation.</summary>
        public ShooterSnapshotApplyResult BufferRemoteSnapshot(in ShooterGatewaySnapshot snapshot)
        {
            var sample = new ShooterRemoteSnapshotSample(
                snapshot.WorldId,
                snapshot.Frame,
                snapshot.ServerTicks,
                snapshot.Actors);

            return _playback.Observe(sample)
                ? ShooterSnapshotApplyResult.AppliedActorSnapshot
                : ShooterSnapshotApplyResult.IgnoredStaleSnapshot;
        }

        public InterpolationDiagnostics GetInterpolationDiagnostics()
        {
            return _playback.GetDiagnostics();
        }

        private void PublishInterpolatedRemoteFrame()
        {
            if (!_playback.TrySample(out var interpolation))
            {
                return;
            }

            var projected = _projector.Project(in interpolation);
            _presentation.ApplyInterpolatedGatewaySnapshot(in projected);
        }

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
            _playback.Observe(sample);
        }

        SyncReconciliationReport IClientSyncStrategy<ShooterPlayerCommand, ShooterRemoteSnapshotSample>.GetReconciliationReport()
        {
            return ShooterClientSyncStrategyMapping.ToReconciliationReport(this);
        }
    }
}
