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
    /// Client side synchronization controller for one Shooter <see cref="NetworkSyncModel"/>.
    /// It owns the runtime behaviour the <see cref="ShooterClientSession"/> facade delegates to:
    /// starting the world, advancing frames, submitting local input, consuming gateway pushes
    /// and driving recovery. Different sync models (predict rollback, authoritative interpolation,
    /// batch state sync) provide different implementations while the session stays mode-agnostic.
    ///
    /// As of migration step 3 this also binds the framework's gameplay-agnostic
    /// <see cref="IClientSyncStrategy{TInput, TSample}"/> contract (with Shooter's command and
    /// remote-sample types), so the demo's sync models are reachable through the shared A-axis
    /// abstraction while keeping their richer demo-facing surface.
    /// </summary>
    public interface IShooterClientSyncController
        : INetworkSyncController, IClientSyncStrategy<ShooterPlayerCommand, ShooterRemoteSnapshotSample>
    {
        ShooterClientFrameSyncController FrameSync { get; }

        ShooterClientFrameSyncCoordinator FrameSyncCoordinator { get; }

        ShooterClientInputCoordinator InputCoordinator { get; }

        ShooterClientReconciliationResult LastReconciliationResult { get; }

        bool NeedsFullSnapshotResync { get; }

        ShooterClientRecoveryState RecoveryState { get; }

        /// <summary>
        /// Current framework <see cref="FastReconnectPhase"/> the embedded
        /// <see cref="FastReconnectSession"/> projects from <see cref="RecoveryState"/>
        /// (audit §10.4: real consumer of FastReconnect).
        /// </summary>
        FastReconnectPhase FastReconnectPhase { get; }

        /// <summary>
        /// Framework <see cref="SyncHealthEvent"/>s the session emitted during the most recent
        /// recovery/heartbeat step, for forwarding into DemoHarness telemetry (design §4.4.5).
        /// </summary>
        System.Collections.Generic.IReadOnlyList<SyncHealthEvent> LastFastReconnectHealthEvents { get; }

        ShooterClientResyncReason LastResyncReason { get; }

        int LastResyncClientFrame { get; }

        int LastResyncAuthoritativeFrame { get; }

        uint LastResyncClientStateHash { get; }

        uint LastResyncAuthoritativeStateHash { get; }

        bool HasGateway { get; }

        bool StartGame(in ShooterStartGamePayload startGame);

        ShooterClientInputSubmitResult SubmitLocalInput(int playerId, float moveX, float moveY, float aimX, float aimY, bool fire);

        ShooterClientInputSubmitResult SubmitLocalInput(in ShooterPlayerCommand command);

        Task<ShooterClientGatewayInputSubmitResult> SubmitLocalInputToGatewayAsync(
            ShooterGatewayBattleInputContext context,
            ShooterPlayerCommand command,
            TimeSpan? timeout = null,
            CancellationToken cancellationToken = default);

        ShooterClientFrameTickResult Tick(float deltaTime);

        ShooterClientFrameTickResult CatchUpToFrame(int targetFrame);

        bool TryEnterCatchUp(int authoritativeFrame);

        ShooterSnapshotApplyResult ApplyGatewayPush(uint opCode, ArraySegment<byte> payload);
    }
}
