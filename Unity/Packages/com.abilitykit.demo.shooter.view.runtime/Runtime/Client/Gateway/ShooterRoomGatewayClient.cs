#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;
using AbilityKit.Protocol.Room;

namespace AbilityKit.Demo.Shooter.View
{
    public interface IShooterRoomGatewayRequestTransport
    {
        Task<ArraySegment<byte>> SendRequestAsync(uint opCode, ArraySegment<byte> payload, TimeSpan? timeout = null, CancellationToken cancellationToken = default);
    }

    public interface IShooterRoomGatewayClient
    {
        Task<ShooterGatewayBattleInputResult> SubmitBattleInputAsync(
            ShooterGatewayBattleInputContext context,
            ShooterInputPacket packet,
            TimeSpan? timeout = null,
            CancellationToken cancellationToken = default);
    }

    public sealed class ShooterRoomGatewayClient : IShooterRoomGatewayClient
    {
        private readonly IShooterRoomGatewayRequestTransport _transport;
        private readonly uint _submitBattleInputOpCode;

        public ShooterRoomGatewayClient(IShooterRoomGatewayRequestTransport transport)
            : this(transport, RoomGatewayOpCodes.SubmitBattleInput)
        {
        }

        public ShooterRoomGatewayClient(IShooterRoomGatewayRequestTransport transport, uint submitBattleInputOpCode)
        {
            _transport = transport ?? throw new ArgumentNullException(nameof(transport));
            _submitBattleInputOpCode = submitBattleInputOpCode;
        }

        public async Task<ShooterGatewayBattleInputResult> SubmitBattleInputAsync(
            ShooterGatewayBattleInputContext context,
            ShooterInputPacket packet,
            TimeSpan? timeout = null,
            CancellationToken cancellationToken = default)
        {
            Validate(in context);

            var req = new WireSubmitBattleInputReq
            {
                SessionToken = context.SessionToken,
                BattleId = context.BattleId,
                WorldId = context.WorldId,
                Frame = context.Frame,
                PlayerId = context.PlayerId,
                InputOpCode = packet.OpCode,
                Payload = packet.Payload ?? Array.Empty<byte>()
            };
            var payload = WireRoomGatewayBinary.Serialize(in req);
            var responsePayload = await _transport.SendRequestAsync(_submitBattleInputOpCode, payload, timeout, cancellationToken).ConfigureAwait(false);
            var wire = WireRoomGatewayBinary.Deserialize<WireSubmitBattleInputRes>(responsePayload);
            return new ShooterGatewayBattleInputResult(wire.Success, wire.AcceptedFrame, wire.Message ?? string.Empty);
        }

        private static void Validate(in ShooterGatewayBattleInputContext context)
        {
            if (string.IsNullOrWhiteSpace(context.SessionToken)) throw new ArgumentException("sessionToken is required.", nameof(context));
            if (string.IsNullOrWhiteSpace(context.BattleId)) throw new ArgumentException("battleId is required.", nameof(context));
            if (context.WorldId == 0) throw new ArgumentOutOfRangeException(nameof(context));
            if (context.Frame < 0) throw new ArgumentOutOfRangeException(nameof(context));
            if (context.PlayerId == 0) throw new ArgumentOutOfRangeException(nameof(context));
        }
    }

    public readonly struct ShooterGatewayBattleInputContext
    {
        public readonly string SessionToken;
        public readonly string BattleId;
        public readonly ulong WorldId;
        public readonly int Frame;
        public readonly uint PlayerId;

        public ShooterGatewayBattleInputContext(string sessionToken, string battleId, ulong worldId, int frame, uint playerId)
        {
            SessionToken = sessionToken ?? string.Empty;
            BattleId = battleId ?? string.Empty;
            WorldId = worldId;
            Frame = frame;
            PlayerId = playerId;
        }
    }

    public readonly struct ShooterGatewayBattleInputResult
    {
        public readonly bool Success;
        public readonly int AcceptedFrame;
        public readonly string Message;

        public ShooterGatewayBattleInputResult(bool success, int acceptedFrame, string message)
        {
            Success = success;
            AcceptedFrame = acceptedFrame;
            Message = message ?? string.Empty;
        }
    }
}
