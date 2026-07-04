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
        private readonly IShooterRoomGatewayRoomClient? _roomClient;
        private readonly ShooterRoomGatewayFlowResult _flow;
        private ShooterClientFullStateSyncRequestKey _lastFullStateSyncRequestKey;

        public ShooterClientBattleHandle(ShooterClientSession session, ShooterRoomGatewayFlowResult flow)
            : this(session, flow, null)
        {
        }

        public ShooterClientBattleHandle(ShooterClientSession session, ShooterRoomGatewayFlowResult flow, IShooterRoomGatewayRoomClient? roomClient)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _roomClient = roomClient;
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

        public async Task<ShooterClientGatewayInputSubmitResult> SubmitLocalInputToGatewayAsync(
            ShooterPlayerCommand command,
            TimeSpan? timeout = null,
            CancellationToken cancellationToken = default)
        {
            var result = await _session.SubmitLocalInputToGatewayAsync(CreateCurrentFrameInputContext(), command, timeout, cancellationToken).ConfigureAwait(false);
            await RequestFullSnapshotResyncIfNeededAsync(timeout, cancellationToken).ConfigureAwait(false);
            return result;
        }

        public async Task<ShooterClientGatewayInputSubmitResult> SubmitAcceptedInputToGatewayAsync(
            ShooterClientInputSubmitResult local,
            TimeSpan? timeout = null,
            CancellationToken cancellationToken = default)
        {
            var context = _flow.CreateBattleInputContext(local.RequestedFrame);
            var result = await _session.SubmitAcceptedInputToGatewayAsync(context, local, timeout, cancellationToken).ConfigureAwait(false);
            await RequestFullSnapshotResyncIfNeededAsync(timeout, cancellationToken).ConfigureAwait(false);
            return result;
        }

        public async Task<ShooterSnapshotApplyResult> ApplyGatewayPushAndRequestFullSnapshotResyncIfNeededAsync(
            uint opCode,
            ArraySegment<byte> payload,
            TimeSpan? timeout = null,
            CancellationToken cancellationToken = default)
        {
            var result = _session.ApplyGatewayPush(opCode, payload);
            await RequestFullSnapshotResyncIfNeededAsync(timeout, cancellationToken).ConfigureAwait(false);
            return result;
        }

        public Task<ShooterGatewayFullStateSyncRequestResult> RequestFullSnapshotResyncIfNeededAsync(
            TimeSpan? timeout = null,
            CancellationToken cancellationToken = default)
        {
            return ShouldRequestFullStateSync()
                ? RequestFullSnapshotAsync(CreateFullStateSyncRequest(), timeout, cancellationToken)
                : Task.FromResult(ShooterGatewayFullStateSyncRequestResult.NotRequested);
        }

        public Task<ShooterGatewayFullStateSyncRequestResult> RequestFullSnapshotBaselineAsync(
            TimeSpan? timeout = null,
            CancellationToken cancellationToken = default)
        {
            return RequestFullSnapshotAsync(CreateBaselineFullStateSyncRequest(), timeout, cancellationToken);
        }

        public ShooterGatewayFullStateSyncRequest CreateFullStateSyncRequest()
        {
            if (_session.NeedsFullSnapshotResync)
            {
                return new ShooterGatewayFullStateSyncRequest(
                    _flow.SessionToken,
                    _flow.BattleId,
                    _flow.RoomId,
                    _flow.WorldId,
                    _session.LastResyncClientFrame,
                    _session.LastResyncAuthoritativeFrame,
                    _session.LastResyncClientStateHash,
                    _session.LastResyncAuthoritativeStateHash,
                    _session.LastResyncReason.ToString());
            }

            if (_session.Presentation.NeedsPureStateFullBaselineResync)
            {
                var reason = $"PureState{_session.Presentation.LastPureStateResyncReason}";
                return new ShooterGatewayFullStateSyncRequest(
                    _flow.SessionToken,
                    _flow.BattleId,
                    _flow.RoomId,
                    _flow.WorldId,
                    _session.Presentation.LastPureStateAppliedFrame,
                    _session.Presentation.LastPureStateResyncFrame,
                    _session.Presentation.LastPureStateAppliedStateHash,
                    _session.Presentation.LastPureStateResyncStateHash,
                    reason);
            }

            return CreateBaselineFullStateSyncRequest();
        }

        private ShooterGatewayFullStateSyncRequest CreateBaselineFullStateSyncRequest()
        {
            return new ShooterGatewayFullStateSyncRequest(
                _flow.SessionToken,
                _flow.BattleId,
                _flow.RoomId,
                _flow.WorldId,
                _session.CurrentFrame,
                _session.CurrentFrame,
                0u,
                0u,
                ShooterClientResyncReason.None.ToString());
        }

        private async Task<ShooterGatewayFullStateSyncRequestResult> RequestFullSnapshotAsync(
            ShooterGatewayFullStateSyncRequest request,
            TimeSpan? timeout,
            CancellationToken cancellationToken)
        {
            if (_roomClient == null)
            {
                return ShooterGatewayFullStateSyncRequestResult.NotRequested;
            }

            var requestKey = ShooterClientFullStateSyncRequestKey.FromRequest(in request);
            if (requestKey.Equals(_lastFullStateSyncRequestKey))
            {
                return ShooterGatewayFullStateSyncRequestResult.NotRequested;
            }

            var result = await _session.RequestFullSnapshotResyncAsync(_roomClient, request, timeout, cancellationToken).ConfigureAwait(false);
            if (result.Accepted)
            {
                _lastFullStateSyncRequestKey = requestKey;
            }

            return result;
        }

        private bool ShouldRequestFullStateSync()
        {
            return _session.NeedsFullSnapshotResync || _session.Presentation.NeedsPureStateFullBaselineResync;
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
            return SubmitLocalInputToGatewayAsync(moveX, moveY, aimX, aimY, fire, ShooterPlayerAttackSlots.Primary, timeout, cancellationToken);
        }

        public Task<ShooterClientGatewayInputSubmitResult> SubmitLocalInputToGatewayAsync(
            float moveX,
            float moveY,
            float aimX,
            float aimY,
            bool fire,
            int attackSlot,
            TimeSpan? timeout = null,
            CancellationToken cancellationToken = default)
        {
            var command = ShooterClientInputBuilder.CreateCommand(GetPlayerIdAsInt(), moveX, moveY, aimX, aimY, fire, attackSlot);
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

    internal readonly struct ShooterClientFullStateSyncRequestKey : IEquatable<ShooterClientFullStateSyncRequestKey>
    {
        private readonly string _sessionToken;
        private readonly string _battleId;
        private readonly string _roomId;
        private readonly ulong _worldId;
        private readonly int _clientFrame;
        private readonly int _lastAuthoritativeFrame;
        private readonly uint _clientStateHash;
        private readonly uint _authoritativeStateHash;
        private readonly string _reason;

        private ShooterClientFullStateSyncRequestKey(
            string sessionToken,
            string battleId,
            string roomId,
            ulong worldId,
            int clientFrame,
            int lastAuthoritativeFrame,
            uint clientStateHash,
            uint authoritativeStateHash,
            string reason)
        {
            _sessionToken = sessionToken ?? string.Empty;
            _battleId = battleId ?? string.Empty;
            _roomId = roomId ?? string.Empty;
            _worldId = worldId;
            _clientFrame = clientFrame;
            _lastAuthoritativeFrame = lastAuthoritativeFrame;
            _clientStateHash = clientStateHash;
            _authoritativeStateHash = authoritativeStateHash;
            _reason = reason ?? string.Empty;
        }

        public static ShooterClientFullStateSyncRequestKey FromRequest(in ShooterGatewayFullStateSyncRequest request)
        {
            return new ShooterClientFullStateSyncRequestKey(
                request.SessionToken,
                request.BattleId,
                request.RoomId,
                request.WorldId,
                request.ClientFrame,
                request.LastAuthoritativeFrame,
                request.ClientStateHash,
                request.AuthoritativeStateHash,
                request.Reason);
        }

        public bool Equals(ShooterClientFullStateSyncRequestKey other)
        {
            return string.Equals(_sessionToken, other._sessionToken, StringComparison.Ordinal)
                && string.Equals(_battleId, other._battleId, StringComparison.Ordinal)
                && string.Equals(_roomId, other._roomId, StringComparison.Ordinal)
                && _worldId == other._worldId
                && _clientFrame == other._clientFrame
                && _lastAuthoritativeFrame == other._lastAuthoritativeFrame
                && _clientStateHash == other._clientStateHash
                && _authoritativeStateHash == other._authoritativeStateHash
                && string.Equals(_reason, other._reason, StringComparison.Ordinal);
        }

        public override bool Equals(object? obj)
        {
            return obj is ShooterClientFullStateSyncRequestKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = StringComparer.Ordinal.GetHashCode(_sessionToken);
                hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(_battleId);
                hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(_roomId);
                hash = (hash * 397) ^ _worldId.GetHashCode();
                hash = (hash * 397) ^ _clientFrame;
                hash = (hash * 397) ^ _lastAuthoritativeFrame;
                hash = (hash * 397) ^ (int)_clientStateHash;
                hash = (hash * 397) ^ (int)_authoritativeStateHash;
                hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(_reason);
                return hash;
            }
        }
    }
}
