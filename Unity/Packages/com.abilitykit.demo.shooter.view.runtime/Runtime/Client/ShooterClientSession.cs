#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;
using AbilityKit.Demo.Shooter.Runtime;
using AbilityKit.Protocol.Shooter;

namespace AbilityKit.Demo.Shooter.View
{
    public sealed class ShooterClientSession
    {
        private readonly IShooterBattleRuntimePort _runtime;
        private readonly ShooterPresentationFacade _presentation;
        private readonly ShooterClientFrameSyncController _frameSync;
        private readonly IShooterRoomGatewayClient? _gateway;

        public ShooterClientSession(IShooterBattleRuntimePort runtime, ShooterPresentationFacade presentation, int tickRate)
            : this(runtime, presentation, tickRate, (ShooterGatewaySnapshotDecoder?)null)
        {
        }

        public ShooterClientSession(IShooterBattleRuntimePort runtime, ShooterPresentationFacade presentation, int tickRate, ShooterGatewaySnapshotDecoder? decoder)
            : this(runtime, presentation, tickRate, decoder, null)
        {
        }

        public ShooterClientSession(IShooterBattleRuntimePort runtime, ShooterPresentationFacade presentation, int tickRate, ShooterGatewaySnapshotDecoder? decoder, IShooterRoomGatewayClient? gateway)
        {
            _runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
            _presentation = presentation ?? throw new ArgumentNullException(nameof(presentation));
            _frameSync = new ShooterClientFrameSyncController(_runtime, _presentation, tickRate, decoder);
            _gateway = gateway;
        }

        public bool IsStarted => _runtime.IsStarted;

        public int CurrentFrame => _frameSync.CurrentFrame;

        public ShooterPresentationFacade Presentation => _presentation;

        public ShooterClientFrameSyncController FrameSync => _frameSync;

        public ShooterClientReconciliationResult LastReconciliationResult => _frameSync.LastReconciliationResult;

        public bool HasGateway => _gateway != null;

        public bool StartGame(in ShooterStartGamePayload startGame)
        {
            if (!_runtime.StartGame(in startGame))
            {
                return false;
            }

            _presentation.ApplyShooterSnapshot(_runtime.GetSnapshot());
            return true;
        }

        public ShooterClientInputSubmitResult SubmitLocalInput(int playerId, float moveX, float moveY, float aimX, float aimY, bool fire)
        {
            var packet = ShooterClientInputBuilder.CreatePacket(playerId, moveX, moveY, aimX, aimY, fire);
            var accepted = _frameSync.SubmitLocalInput(packet.Command);
            return new ShooterClientInputSubmitResult(accepted, CurrentFrame, packet);
        }

        public ShooterClientInputSubmitResult SubmitLocalInput(in ShooterPlayerCommand command)
        {
            var payload = ShooterInputCodec.Serialize(new[] { command });
            var packet = new ShooterInputPacket(ShooterOpCodes.Input.PlayerCommand, payload, in command);
            var accepted = _frameSync.SubmitLocalInput(in command);
            return new ShooterClientInputSubmitResult(accepted, CurrentFrame, packet);
        }

        public async Task<ShooterClientGatewayInputSubmitResult> SubmitLocalInputToGatewayAsync(
            ShooterGatewayBattleInputContext context,
            ShooterPlayerCommand command,
            TimeSpan? timeout = null,
            CancellationToken cancellationToken = default)
        {
            if (_gateway == null)
            {
                throw new InvalidOperationException("Shooter room gateway client is not configured.");
            }

            var local = SubmitLocalInput(in command).WithRequestedFrame(context.Frame);
            var remote = await _gateway.SubmitBattleInputAsync(context, local.Packet, timeout, cancellationToken).ConfigureAwait(false);
            return new ShooterClientGatewayInputSubmitResult(in local, in remote);
        }

        public ShooterClientFrameTickResult Tick(float deltaTime)
        {
            return _frameSync.Tick(deltaTime);
        }

        public ShooterSnapshotApplyResult ApplyGatewayPush(uint opCode, ArraySegment<byte> payload)
        {
            return _frameSync.ApplyGatewayPush(opCode, payload);
        }
    }

    public readonly struct ShooterClientGatewayInputSubmitResult
    {
        public readonly ShooterClientInputSubmitResult Local;
        public readonly ShooterGatewayBattleInputResult Remote;

        public ShooterClientGatewayInputSubmitResult(in ShooterClientInputSubmitResult local, in ShooterGatewayBattleInputResult remote)
        {
            Local = local;
            Remote = remote;
        }
    }

    public readonly struct ShooterClientInputSubmitResult
    {
        public readonly int AcceptedInputs;
        public readonly int RequestedFrame;
        public readonly ShooterInputPacket Packet;

        public ShooterClientInputSubmitResult(int acceptedInputs, int requestedFrame, in ShooterInputPacket packet)
        {
            AcceptedInputs = acceptedInputs;
            RequestedFrame = requestedFrame;
            Packet = packet;
        }

        public ShooterClientInputSubmitResult WithRequestedFrame(int requestedFrame)
        {
            return new ShooterClientInputSubmitResult(AcceptedInputs, requestedFrame, in Packet);
        }
    }
}
