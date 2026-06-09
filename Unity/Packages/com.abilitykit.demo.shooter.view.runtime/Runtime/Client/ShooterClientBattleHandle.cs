#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;
using AbilityKit.Protocol.Shooter;

namespace AbilityKit.Demo.Shooter.View
{
    public sealed class ShooterClientBattleHandle
    {
        private readonly ShooterClientSession _session;
        private readonly ShooterRoomGatewayFlowResult _flow;

        public ShooterClientBattleHandle(ShooterClientSession session, ShooterRoomGatewayFlowResult flow)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
            if (string.IsNullOrWhiteSpace(flow.SessionToken))
            {
                throw new ArgumentException("sessionToken is required.", nameof(flow));
            }

            if (string.IsNullOrWhiteSpace(flow.BattleId))
            {
                throw new ArgumentException("battleId is required.", nameof(flow));
            }

            if (flow.PlayerId == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(flow));
            }

            _flow = flow;
        }

        public ShooterClientSession Session => _session;

        public ShooterRoomGatewayFlowResult Flow => _flow;

        public string RoomId => _flow.RoomId;

        public ulong NumericRoomId => _flow.NumericRoomId;

        public string BattleId => _flow.BattleId;

        public ulong WorldId => _flow.WorldId;

        public uint PlayerId => _flow.PlayerId;

        public int CurrentFrame => _session.CurrentFrame;

        public ShooterGatewayBattleInputContext CreateCurrentFrameInputContext()
        {
            return _flow.CreateBattleInputContext(_session.CurrentFrame);
        }

        public Task<ShooterClientGatewayInputSubmitResult> SubmitLocalInputToGatewayAsync(
            ShooterPlayerCommand command,
            TimeSpan? timeout = null,
            CancellationToken cancellationToken = default)
        {
            return _session.SubmitLocalInputToGatewayAsync(CreateCurrentFrameInputContext(), command, timeout, cancellationToken);
        }

        public Task<ShooterClientGatewayInputSubmitResult> SubmitLocalInputToGatewayAsync(
            float moveX,
            float moveY,
            float aimX,
            float aimY,
            bool fire,
            TimeSpan? timeout = null,
            CancellationToken cancellationToken = default)
        {
            var command = ShooterClientInputBuilder.CreateCommand(GetPlayerIdAsInt(), moveX, moveY, aimX, aimY, fire);
            return SubmitLocalInputToGatewayAsync(command, timeout, cancellationToken);
        }

        private int GetPlayerIdAsInt()
        {
            if (_flow.PlayerId > int.MaxValue)
            {
                throw new InvalidOperationException("playerId is too large for ShooterPlayerCommand.");
            }

            return (int)_flow.PlayerId;
        }
    }
}
