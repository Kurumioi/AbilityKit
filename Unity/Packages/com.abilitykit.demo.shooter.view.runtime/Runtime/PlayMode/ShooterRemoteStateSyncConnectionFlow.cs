#nullable enable

using System;
using System.Threading.Tasks;
using AbilityKit.Demo.Shooter.Runtime;
using AbilityKit.Protocol.Shooter;

namespace AbilityKit.Demo.Shooter.View.PlayMode
{
    public readonly struct ShooterRemoteStateSyncConnectionResult
    {
        public ShooterRemoteStateSyncConnectionResult(
            ShooterClientNetworkLaunchResult launch,
            ShooterRemoteStateSyncLaunchMode requestedMode,
            ShooterRoomGatewayEntryKind entryKind,
            bool usedFallbackCreate,
            Exception? restoreFailure)
        {
            Launch = launch;
            RequestedMode = requestedMode;
            EntryKind = entryKind;
            UsedFallbackCreate = usedFallbackCreate;
            RestoreFailure = restoreFailure;
        }

        public ShooterClientNetworkLaunchResult Launch { get; }
        public ShooterRemoteStateSyncLaunchMode RequestedMode { get; }
        public ShooterRoomGatewayEntryKind EntryKind { get; }
        public bool UsedFallbackCreate { get; }
        public Exception? RestoreFailure { get; }
    }

    public sealed class ShooterRemoteStateSyncConnectionFlow
    {
        public async Task<ShooterRemoteStateSyncConnectionResult> ConnectAsync(
            ShooterClientNetworkLauncher launcher,
            IShooterBattleRuntimePort runtime,
            ShooterPresentationSessionContext presentationSession,
            ShooterRemoteStateSyncLaunchOptions launchOptions,
            ShooterStartGamePayload startGame,
            ShooterRoomLaunchSpec launchSpec,
            uint playerId)
        {
            if (launcher == null)
            {
                throw new ArgumentNullException(nameof(launcher));
            }

            if (runtime == null)
            {
                throw new ArgumentNullException(nameof(runtime));
            }

            if (launchOptions.LaunchMode == ShooterRemoteStateSyncLaunchMode.CreateNew)
            {
                var created = await CreateReadyStartAndSubscribeAsync(
                    launcher,
                    runtime,
                    presentationSession,
                    launchOptions,
                    startGame,
                    launchSpec,
                    playerId).ConfigureAwait(false);
                return new ShooterRemoteStateSyncConnectionResult(
                    created,
                    launchOptions.LaunchMode,
                    created.Flow.EntryKind,
                    false,
                    null);
            }

            try
            {
                var restored = await RestoreRoomAsync(
                    launcher,
                    runtime,
                    presentationSession,
                    launchOptions,
                    startGame,
                    launchSpec,
                    playerId).ConfigureAwait(false);
                return new ShooterRemoteStateSyncConnectionResult(
                    restored,
                    launchOptions.LaunchMode,
                    restored.Flow.EntryKind,
                    false,
                    null);
            }
            catch (Exception ex) when (launchOptions.LaunchMode == ShooterRemoteStateSyncLaunchMode.RestoreFirst)
            {
                var created = await CreateReadyStartAndSubscribeAsync(
                    launcher,
                    runtime,
                    presentationSession,
                    launchOptions,
                    startGame,
                    launchSpec,
                    playerId).ConfigureAwait(false);
                return new ShooterRemoteStateSyncConnectionResult(
                    created,
                    launchOptions.LaunchMode,
                    created.Flow.EntryKind,
                    true,
                    ex);
            }
        }

        private static Task<ShooterClientNetworkRestoreResult> RestoreRoomAsync(
            ShooterClientNetworkLauncher launcher,
            IShooterBattleRuntimePort runtime,
            ShooterPresentationSessionContext presentationSession,
            ShooterRemoteStateSyncLaunchOptions launchOptions,
            ShooterStartGamePayload startGame,
            ShooterRoomLaunchSpec launchSpec,
            uint playerId)
        {
            return launcher.RestoreRoomAsync(
                launchOptions.Endpoint,
                runtime,
                presentationSession,
                startGame,
                launchOptions.SessionToken,
                launchOptions.Region,
                launchOptions.ServerId,
                launchSpec,
                playerId,
                launchOptions.SessionOptions.TickRate,
                launchOptions.Timeout);
        }

        private static Task<ShooterClientNetworkLaunchResult> CreateReadyStartAndSubscribeAsync(
            ShooterClientNetworkLauncher launcher,
            IShooterBattleRuntimePort runtime,
            ShooterPresentationSessionContext presentationSession,
            ShooterRemoteStateSyncLaunchOptions launchOptions,
            ShooterStartGamePayload startGame,
            ShooterRoomLaunchSpec launchSpec,
            uint playerId)
        {
            return launcher.CreateReadyStartAndSubscribeAsync(
                launchOptions.Endpoint,
                runtime,
                presentationSession,
                startGame,
                launchOptions.SessionToken,
                launchSpec,
                playerId,
                launchOptions.SessionOptions.TickRate,
                launchOptions.Timeout);
        }
    }
}
