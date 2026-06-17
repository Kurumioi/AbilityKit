#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;
using AbilityKit.Protocol.Shooter;

namespace AbilityKit.Demo.Shooter.View
{
    public sealed class ShooterClientInputCoordinator
    {
        private readonly ShooterClientFrameSyncCoordinator _frameSync;
        private readonly IShooterRoomGatewayClient? _gateway;

        public ShooterClientInputCoordinator(ShooterClientFrameSyncCoordinator frameSync)
            : this(frameSync, null)
        {
        }

        public ShooterClientInputCoordinator(ShooterClientFrameSyncCoordinator frameSync, IShooterRoomGatewayClient? gateway)
        {
            _frameSync = frameSync ?? throw new ArgumentNullException(nameof(frameSync));
            _gateway = gateway;
        }

        public bool HasGateway => _gateway != null;

        public ShooterClientInputSubmitResult SubmitLocalInput(int playerId, float moveX, float moveY, float aimX, float aimY, bool fire)
        {
            var packet = ShooterClientInputBuilder.CreatePacket(playerId, moveX, moveY, aimX, aimY, fire);
            var accepted = _frameSync.SubmitLocalInput(packet.Command);
            return new ShooterClientInputSubmitResult(accepted, _frameSync.CurrentFrame, packet);
        }

        public ShooterClientInputSubmitResult SubmitLocalInput(in ShooterPlayerCommand command)
        {
            var payload = ShooterInputCodec.Serialize(new[] { command });
            var packet = new ShooterInputPacket(ShooterOpCodes.Input.PlayerCommand, payload, in command);
            var accepted = _frameSync.SubmitLocalInput(in command);
            return new ShooterClientInputSubmitResult(accepted, _frameSync.CurrentFrame, packet);
        }

        public Task<ShooterClientGatewayInputSubmitResult> SubmitLocalInputToGatewayAsync(
            ShooterGatewayBattleInputContext context,
            ShooterPlayerCommand command,
            TimeSpan? timeout = null,
            CancellationToken cancellationToken = default)
        {
            var local = SubmitLocalInput(in command).WithRequestedFrame(context.Frame);
            return SubmitAcceptedInputToGatewayAsync(context, local, timeout, cancellationToken);
        }

        public async Task<ShooterClientGatewayInputSubmitResult> SubmitAcceptedInputToGatewayAsync(
            ShooterGatewayBattleInputContext context,
            ShooterClientInputSubmitResult local,
            TimeSpan? timeout = null,
            CancellationToken cancellationToken = default)
        {
            if (_gateway == null)
            {
                throw new InvalidOperationException("Shooter room gateway client is not configured.");
            }

            var remote = await _gateway.SubmitBattleInputAsync(context, local.Packet, timeout, cancellationToken).ConfigureAwait(false);
            if (remote.ShouldResync)
            {
                _frameSync.MarkGatewayInputResyncRequested(context.Frame, remote.CurrentFrame);
            }

            return new ShooterClientGatewayInputSubmitResult(in local, in remote);
        }
    }
}
