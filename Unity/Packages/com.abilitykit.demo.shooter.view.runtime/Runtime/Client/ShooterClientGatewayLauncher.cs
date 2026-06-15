#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;
using AbilityKit.Demo.Shooter.Runtime;
using AbilityKit.Protocol.Shooter;

namespace AbilityKit.Demo.Shooter.View
{
    public sealed class ShooterClientGatewayLauncher
    {
        private readonly IShooterRoomGatewayRequestTransport _transport;

        public ShooterClientGatewayLauncher(IShooterRoomGatewayRequestTransport transport)
        {
            _transport = transport ?? throw new ArgumentNullException(nameof(transport));
        }

        public Task<ShooterClientGatewayLaunchResult> CreateReadyStartAndSubscribeAsync(
            IShooterBattleRuntimePort runtime,
            ShooterPresentationFacade presentation,
            ShooterStartGamePayload startGame,
            string sessionToken,
            ShooterRoomLaunchSpec launchSpec,
            uint playerId,
            int tickRate = ShooterGameplay.DefaultTickRate,
            TimeSpan? timeout = null,
            CancellationToken cancellationToken = default)
        {
            return CreateReadyStartAndSubscribeAsync(
                runtime,
                ShooterPresentationSessionContext.CreateFromFacade(presentation),
                startGame,
                sessionToken,
                launchSpec,
                playerId,
                tickRate,
                timeout,
                cancellationToken);
        }

        public Task<ShooterClientGatewayLaunchResult> CreateReadyStartAndSubscribeAsync(
            IShooterBattleRuntimePort runtime,
            ShooterPresentationSessionContext presentationSession,
            ShooterStartGamePayload startGame,
            string sessionToken,
            ShooterRoomLaunchSpec launchSpec,
            uint playerId,
            int tickRate = ShooterGameplay.DefaultTickRate,
            TimeSpan? timeout = null,
            CancellationToken cancellationToken = default)
        {
            return LaunchAsync(
                runtime,
                presentationSession,
                startGame,
                sessionToken,
                launchSpec,
                playerId,
                joinRoomId: null,
                tickRate,
                timeout,
                cancellationToken);
        }

        public Task<ShooterClientGatewayLaunchResult> JoinReadyStartAndSubscribeAsync(
            IShooterBattleRuntimePort runtime,
            ShooterPresentationFacade presentation,
            ShooterStartGamePayload startGame,
            string sessionToken,
            string roomId,
            ShooterRoomLaunchSpec launchSpec,
            uint playerId,
            int tickRate = ShooterGameplay.DefaultTickRate,
            TimeSpan? timeout = null,
            CancellationToken cancellationToken = default)
        {
            return JoinReadyStartAndSubscribeAsync(
                runtime,
                ShooterPresentationSessionContext.CreateFromFacade(presentation),
                startGame,
                sessionToken,
                roomId,
                launchSpec,
                playerId,
                tickRate,
                timeout,
                cancellationToken);
        }

        public Task<ShooterClientGatewayLaunchResult> JoinReadyStartAndSubscribeAsync(
            IShooterBattleRuntimePort runtime,
            ShooterPresentationSessionContext presentationSession,
            ShooterStartGamePayload startGame,
            string sessionToken,
            string roomId,
            ShooterRoomLaunchSpec launchSpec,
            uint playerId,
            int tickRate = ShooterGameplay.DefaultTickRate,
            TimeSpan? timeout = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(roomId))
            {
                throw new ArgumentException("roomId is required.", nameof(roomId));
            }

            return LaunchAsync(
                runtime,
                presentationSession,
                startGame,
                sessionToken,
                launchSpec,
                playerId,
                roomId,
                tickRate,
                timeout,
                cancellationToken);
        }

        private async Task<ShooterClientGatewayLaunchResult> LaunchAsync(
            IShooterBattleRuntimePort runtime,
            ShooterPresentationSessionContext presentationSession,
            ShooterStartGamePayload startGame,
            string sessionToken,
            ShooterRoomLaunchSpec launchSpec,
            uint playerId,
            string? joinRoomId,
            int tickRate,
            TimeSpan? timeout,
            CancellationToken cancellationToken)
        {
            if (runtime == null)
            {
                throw new ArgumentNullException(nameof(runtime));
            }

            if (presentationSession == null)
            {
                throw new ArgumentNullException(nameof(presentationSession));
            }

            if (tickRate <= 0)
            {
                tickRate = ShooterGameplay.DefaultTickRate;
            }

            var roomClient = new ShooterRoomGatewayRoomClient(_transport);
            var flow = new ShooterRoomGatewayFlow(roomClient);
            var flowResult = joinRoomId == null
                ? await flow.CreateReadyStartAndSubscribeAsync(sessionToken, launchSpec, playerId, timeout, cancellationToken).ConfigureAwait(false)
                : await flow.JoinReadyStartAndSubscribeAsync(sessionToken, joinRoomId, launchSpec, playerId, timeout, cancellationToken).ConfigureAwait(false);

            var gatewayClient = new ShooterRoomGatewayClient(_transport);
            var session = new ShooterClientSession(runtime, presentationSession, tickRate, decoder: null, gatewayClient);
            var alignedStartGame = startGame.WithWorldStartAnchor(
                flowResult.WorldId,
                flowResult.WorldStartAnchor.StartServerTicks,
                flowResult.WorldStartAnchor.ServerTickFrequency,
                flowResult.WorldStartAnchor.StartFrame,
                flowResult.WorldStartAnchor.FixedDeltaSeconds);
            if (!session.StartGame(in alignedStartGame))
            {
                throw new InvalidOperationException("Shooter client session failed to start game.");
            }

            session.CatchUpToFrame(flowResult.TargetFrame);

            var battle = new ShooterClientBattleHandle(session, flowResult, roomClient);
            return new ShooterClientGatewayLaunchResult(
                roomClient,
                gatewayClient,
                session,
                battle,
                flowResult);
        }
    }

    public sealed class ShooterClientGatewayLaunchResult
    {
        public ShooterClientGatewayLaunchResult(
            IShooterRoomGatewayRoomClient roomClient,
            IShooterRoomGatewayClient gatewayClient,
            ShooterClientSession session,
            ShooterClientBattleHandle battle,
            ShooterRoomGatewayFlowResult flow)
        {
            RoomClient = roomClient ?? throw new ArgumentNullException(nameof(roomClient));
            GatewayClient = gatewayClient ?? throw new ArgumentNullException(nameof(gatewayClient));
            Session = session ?? throw new ArgumentNullException(nameof(session));
            Battle = battle ?? throw new ArgumentNullException(nameof(battle));
            Flow = flow;
            Summary = flow.ToSummary();
        }

        public IShooterRoomGatewayRoomClient RoomClient { get; }

        public IShooterRoomGatewayClient GatewayClient { get; }

        public ShooterClientSession Session { get; }

        public ShooterClientBattleHandle Battle { get; }

        public ShooterRoomGatewayFlowResult Flow { get; }

        public ShooterRoomGatewayLaunchSummary Summary { get; }
    }
}
