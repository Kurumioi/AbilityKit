#nullable enable
using System;
using System.Threading;
using System.Threading.Tasks;
using AbilityKit.Game.Battle.Agent;

namespace AbilityKit.Game.Flow
{
    /// <summary>
    /// Applies Room state pushes and coalesces stale-store refreshes into one request.
    /// </summary>
    public sealed class ClientRoomPushSynchronizer
    {
        private readonly IGatewayRoomClient _client;
        private readonly ClientRoomStore _store;
        private readonly Func<CancellationToken, Task> _refreshSnapshotAsync;
        private int _refreshing;

        public ClientRoomPushSynchronizer(
            IGatewayRoomClient client,
            ClientRoomStore store,
            Func<CancellationToken, Task> refreshSnapshotAsync)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _refreshSnapshotAsync = refreshSnapshotAsync ?? throw new ArgumentNullException(nameof(refreshSnapshotAsync));
        }

        public async Task<bool> HandleServerPushAsync(
            uint opCode,
            ArraySegment<byte> payload,
            CancellationToken cancellationToken = default)
        {
            if (!_client.IsRoomStateChangedPush(opCode))
            {
                return false;
            }

            var snapshot = _client.DeserializeRoomStateChangedPush(payload);
            _store.ApplySnapshot(snapshot);

            if (_store.IsStale && Interlocked.CompareExchange(ref _refreshing, 1, 0) == 0)
            {
                try
                {
                    await _refreshSnapshotAsync(cancellationToken).ConfigureAwait(false);
                }
                finally
                {
                    Volatile.Write(ref _refreshing, 0);
                }
            }

            return true;
        }
    }
}
