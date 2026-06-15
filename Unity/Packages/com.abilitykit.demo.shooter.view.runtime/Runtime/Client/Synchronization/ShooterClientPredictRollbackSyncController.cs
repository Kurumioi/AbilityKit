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
    /// <see cref="NetworkSyncModel.PredictRollback"/> client controller.
    /// Wraps the existing local prediction, authoritative snapshot, rollback and replay chain
    /// (<see cref="ShooterClientFrameSyncCoordinator"/> + <see cref="ShooterClientInputCoordinator"/>)
    /// behind the common <see cref="IShooterClientSyncController"/> seam so the session can
    /// delegate without knowing the active synchronization model.
    /// </summary>
    public sealed class ShooterClientPredictRollbackSyncController : IShooterClientSyncController
    {
        private readonly ShooterClientFrameSyncCoordinator _frameSync;
        private readonly ShooterClientInputCoordinator _input;

        public ShooterClientPredictRollbackSyncController(
            IShooterBattleRuntimePort runtime,
            ShooterPresentationFacade presentation,
            int tickRate,
            ShooterGatewaySnapshotDecoder? decoder,
            IShooterRoomGatewayClient? gateway)
        {
            _frameSync = new ShooterClientFrameSyncCoordinator(runtime, presentation, tickRate, decoder);
            _input = new ShooterClientInputCoordinator(_frameSync, gateway);
        }

        public NetworkSyncModel SyncModel => NetworkSyncModel.PredictRollback;

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
            return _frameSync.Tick(deltaTime);
        }

        public ShooterClientFrameTickResult CatchUpToFrame(int targetFrame)
        {
            return _frameSync.CatchUpToFrame(targetFrame);
        }

        public bool TryEnterCatchUp(int authoritativeFrame)
        {
            return _frameSync.TryEnterCatchUp(authoritativeFrame);
        }

        public ShooterSnapshotApplyResult ApplyGatewayPush(uint opCode, ArraySegment<byte> payload)
        {
            return _frameSync.ApplyGatewayPush(opCode, payload);
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
            // Predict-rollback does not consume per-actor remote samples: it reconciles by importing
            // packed authoritative snapshot bytes via ApplyGatewayPush and rolling the local
            // simulation back/forward. Observing a decoded sample is therefore a no-op for this model.
        }

        SyncReconciliationReport IClientSyncStrategy<ShooterPlayerCommand, ShooterRemoteSnapshotSample>.GetReconciliationReport()
        {
            return ShooterClientSyncStrategyMapping.ToReconciliationReport(this);
        }
    }
}
