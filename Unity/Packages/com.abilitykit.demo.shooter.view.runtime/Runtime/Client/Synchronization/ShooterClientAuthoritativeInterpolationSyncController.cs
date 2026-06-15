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
        private readonly ShooterClientFrameSyncCoordinator _frameSync;
        private readonly ShooterClientInputCoordinator _input;
        private readonly ShooterPresentationFacade _presentation;
        private readonly ShooterGatewaySnapshotDecoder _decoder;
        private readonly RemoteInterpolationPlayback<ShooterRemoteSnapshotSample> _playback;
        private readonly ShooterRemoteSnapshotProjector _projector = new ShooterRemoteSnapshotProjector();

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
            _presentation = presentation ?? throw new ArgumentNullException(nameof(presentation));
            _frameSync = new ShooterClientFrameSyncCoordinator(runtime, presentation, tickRate, decoder);
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

        public bool StartGame(in ShooterStartGamePayload startGame)
        {
            return _frameSync.StartGame(in startGame);
        }

        public ShooterClientInputSubmitResult SubmitLocalInput(int playerId, float moveX, float moveY, float aimX, float aimY, bool fire)
        {
            return _input.SubmitLocalInput(playerId, moveX, moveY, aimX, aimY, fire);
        }

        public ShooterClientInputSubmitResult SubmitLocalInput(in ShooterPlayerCommand command)
        {
            return _input.SubmitLocalInput(in command);
        }

        public Task<ShooterClientGatewayInputSubmitResult> SubmitLocalInputToGatewayAsync(
            ShooterGatewayBattleInputContext context,
            ShooterPlayerCommand command,
            TimeSpan? timeout = null,
            CancellationToken cancellationToken = default)
        {
            return _input.SubmitLocalInputToGatewayAsync(context, command, timeout, cancellationToken);
        }

        public ShooterClientFrameTickResult Tick(float deltaTime)
        {
            var result = _frameSync.Tick(deltaTime);
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

            return _playback.Observe(sample)
                ? ShooterSnapshotApplyResult.AppliedActorSnapshot
                : ShooterSnapshotApplyResult.IgnoredStaleSnapshot;
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
            _presentation.ApplyGatewaySnapshot(in projected);
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
}
