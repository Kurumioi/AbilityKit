#nullable enable
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AbilityKit.Game.Battle.Agent;

namespace AbilityKit.Game.Flow
{
    /// <summary>
    /// 将权威 <see cref="ClientRoomStore"/> 投影为多人流程使用的稳定视图。
    /// </summary>
    public sealed class ClientRoomSnapshotProvider : IRoomSnapshotProvider, IDisposable
    {
        private readonly ClientRoomStore _store;

        public ClientRoomSnapshotProvider(ClientRoomStore store)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _store.OnSnapshotChanged += HandleSnapshotChanged;
        }

        public MultiplayerRoomSnapshot? Current => Project(_store.Current);

        public event Action<MultiplayerRoomSnapshot>? OnSnapshotChanged;

        public void Dispose()
        {
            _store.OnSnapshotChanged -= HandleSnapshotChanged;
        }

        private void HandleSnapshotChanged(ClientRoomSnapshot snapshot)
        {
            var projected = Project(snapshot);
            if (projected != null)
            {
                OnSnapshotChanged?.Invoke(projected);
            }
        }

        private static MultiplayerRoomSnapshot? Project(ClientRoomSnapshot? snapshot)
        {
            if (snapshot == null)
            {
                return null;
            }

            return new MultiplayerRoomSnapshot
            {
                RoomId = snapshot.RoomId,
                NumericRoomId = snapshot.NumericRoomId,
                Phase = (MultiplayerRoomPhase)snapshot.Phase,
                CanStart = snapshot.CanStart,
                BattleId = snapshot.BattleId,
                WorldId = snapshot.WorldId,
                LaunchGeneration = snapshot.LaunchGeneration,
                LaunchManifestVersion = snapshot.LaunchManifestVersion,
                LaunchManifestHash = snapshot.LaunchManifestHash,
                RoomRevision = snapshot.RoomRevision
            };
        }
    }

    /// <summary>
    /// 基于 MOBA 原生 Gateway API 的正式多人房间会话适配器。
    /// 每个写命令完成后补拉权威快照并写入 <see cref="ClientRoomStore"/>。
    /// </summary>
    public sealed class GatewayMultiplayerRoomSession : IMultiplayerRoomSession
    {
        private static readonly IReadOnlyDictionary<string, string> EmptyTags =
            new Dictionary<string, string>();

        private readonly IGatewayRoomClient _client;
        private readonly ClientRoomStore _store;
        private readonly uint _guestLoginOpCode;
        private readonly TimeSpan _requestTimeout;
        private readonly TimeSpan _pollInterval;
        private readonly TimeSpan _battleStartTimeout;
        private string _sessionToken = string.Empty;

        public string SessionToken => _sessionToken;

        public GatewayMultiplayerRoomSession(
            IGatewayRoomClient client,
            ClientRoomStore store,
            uint guestLoginOpCode = 100u,
            TimeSpan? requestTimeout = null,
            TimeSpan? pollInterval = null,
            TimeSpan? battleStartTimeout = null)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _guestLoginOpCode = guestLoginOpCode;
            _requestTimeout = requestTimeout ?? TimeSpan.FromSeconds(10);
            _pollInterval = pollInterval ?? TimeSpan.FromMilliseconds(500);
            _battleStartTimeout = battleStartTimeout ?? TimeSpan.FromSeconds(30);
        }

        public async Task<string> CreateRoomAsync(
            MultiplayerRoomLaunchSpec spec,
            CancellationToken cancellationToken)
        {
            ValidateSpec(spec);
            var token = await EnsureSessionTokenAsync(spec.SessionToken, cancellationToken).ConfigureAwait(false);
            var result = await _client.CreateRoomAsync(
                token,
                spec.Region,
                spec.ServerId,
                spec.RoomType,
                spec.RoomTitle,
                true,
                spec.MaxPlayers,
                EmptyTags,
                _requestTimeout,
                cancellationToken).ConfigureAwait(false);

            if (string.IsNullOrWhiteSpace(result.RoomId))
            {
                throw new InvalidOperationException("Gateway create room did not return a room id.");
            }

            return result.RoomId;
        }

        public async Task JoinRoomAsync(
            MultiplayerRoomLaunchSpec spec,
            string roomId,
            CancellationToken cancellationToken)
        {
            ValidateSpec(spec);
            ValidateRoomId(roomId);
            var token = await EnsureSessionTokenAsync(spec.SessionToken, cancellationToken).ConfigureAwait(false);
            await _client.JoinRoomAsync(
                token,
                spec.Region,
                spec.ServerId,
                roomId,
                _requestTimeout,
                cancellationToken).ConfigureAwait(false);
            await RefreshSnapshotAsync(roomId, cancellationToken).ConfigureAwait(false);
        }

        public async Task ConfigureLoadoutAsync(
            string roomId,
            MultiplayerLoadoutSpec loadout,
            CancellationToken cancellationToken)
        {
            ValidateActiveSession(roomId);
            await _client.PickHeroAsync(
                _sessionToken,
                roomId,
                loadout.HeroId,
                loadout.TeamId,
                loadout.SpawnPointId,
                loadout.Level,
                loadout.AttributeTemplateId,
                loadout.BasicAttackSkillId,
                loadout.SkillIds,
                _requestTimeout,
                cancellationToken).ConfigureAwait(false);
            await RefreshSnapshotAsync(roomId, cancellationToken).ConfigureAwait(false);
        }

        public async Task SetReadyAsync(
            string roomId,
            bool ready,
            CancellationToken cancellationToken)
        {
            ValidateActiveSession(roomId);
            await _client.SetReadyAsync(
                _sessionToken,
                roomId,
                ready,
                _requestTimeout,
                cancellationToken).ConfigureAwait(false);
            await RefreshSnapshotAsync(roomId, cancellationToken).ConfigureAwait(false);
        }

        public async Task BeginLoadingAsync(string roomId, CancellationToken cancellationToken)
        {
            ValidateActiveSession(roomId);
            var current = RequireCurrentSnapshot(roomId);
            var result = await _client.BeginLoadingAsync(
                _sessionToken,
                roomId,
                current.RoomRevision,
                NewCommandId("begin-loading"),
                _requestTimeout,
                cancellationToken).ConfigureAwait(false);
            EnsureOperationSucceeded(result, "begin loading");
            await ApplyOperationOrRefreshAsync(roomId, result, cancellationToken).ConfigureAwait(false);
        }

        public async Task ReportAssetsLoadedAsync(string roomId, CancellationToken cancellationToken)
        {
            ValidateActiveSession(roomId);
            var current = RequireCurrentSnapshot(roomId);
            if (current.Phase != ClientRoomPhase.Loading)
            {
                throw new InvalidOperationException($"Cannot report assets in room phase {current.Phase}.");
            }

            var result = await _client.ReportAssetsLoadedAsync(
                _sessionToken,
                roomId,
                current.LaunchGeneration,
                current.LaunchManifestVersion,
                current.LaunchManifestHash,
                NewCommandId("assets-loaded"),
                _requestTimeout,
                cancellationToken).ConfigureAwait(false);
            EnsureOperationSucceeded(result, "report assets loaded");
            await ApplyOperationOrRefreshAsync(roomId, result, cancellationToken).ConfigureAwait(false);
        }

        public async Task WaitForBattleStartAsync(string roomId, CancellationToken cancellationToken)
        {
            ValidateActiveSession(roomId);
            var deadline = DateTime.UtcNow + _battleStartTimeout;
            while (DateTime.UtcNow < deadline)
            {
                var snapshot = await RefreshSnapshotAsync(roomId, cancellationToken).ConfigureAwait(false);
                if (snapshot.Phase == ClientRoomPhase.InBattle &&
                    !string.IsNullOrWhiteSpace(snapshot.BattleId) &&
                    snapshot.WorldId != 0UL)
                {
                    return;
                }

                if (!snapshot.IsActive)
                {
                    throw new InvalidOperationException(
                        $"Room entered terminal phase {snapshot.Phase} while waiting for battle.");
                }

                await Task.Delay(_pollInterval, cancellationToken).ConfigureAwait(false);
            }

            throw new TimeoutException($"Wait for battle start timed out for room {roomId}.");
        }

        private async Task<string> EnsureSessionTokenAsync(
            string requestedToken,
            CancellationToken cancellationToken)
        {
            if (!string.IsNullOrWhiteSpace(requestedToken))
            {
                _sessionToken = requestedToken;
                return _sessionToken;
            }

            if (!string.IsNullOrWhiteSpace(_sessionToken))
            {
                return _sessionToken;
            }

            _sessionToken = await _client.GuestLoginAsync(
                _guestLoginOpCode,
                _requestTimeout,
                cancellationToken).ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(_sessionToken))
            {
                throw new InvalidOperationException("Gateway guest login did not return a session token.");
            }

            return _sessionToken;
        }

        internal async Task<ClientRoomSnapshot> RefreshSnapshotAsync(
            string roomId,
            CancellationToken cancellationToken)
        {
            var result = await _client.GetSnapshotAsync(
                _sessionToken,
                roomId,
                _requestTimeout,
                cancellationToken).ConfigureAwait(false);
            if (!result.Success || result.Snapshot == null)
            {
                throw new InvalidOperationException(
                    $"Gateway get snapshot failed: {result.Message}");
            }

            if (result.NumericRoomId == 0UL)
            {
                throw new InvalidOperationException(
                    $"Gateway get snapshot returned an invalid numeric room id for room {roomId}.");
            }

            result.Snapshot.NumericRoomId = result.NumericRoomId;
            _store.ApplySnapshot(result.Snapshot);
            _store.MarkRefreshed();
            return result.Snapshot;
        }

        private async Task ApplyOperationOrRefreshAsync(
            string roomId,
            GatewayRoomOperationResult result,
            CancellationToken cancellationToken)
        {
            if (result.Snapshot != null)
            {
                _store.ApplySnapshot(result.Snapshot);
                return;
            }

            await RefreshSnapshotAsync(roomId, cancellationToken).ConfigureAwait(false);
        }

        private ClientRoomSnapshot RequireCurrentSnapshot(string roomId)
        {
            var current = _store.Current;
            if (current == null || !string.Equals(current.RoomId, roomId, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Current authoritative room snapshot is unavailable.");
            }

            return current;
        }

        private static void EnsureOperationSucceeded(
            GatewayRoomOperationResult result,
            string operation)
        {
            if (!result.Success)
            {
                throw new InvalidOperationException(
                    $"Gateway {operation} failed ({result.ErrorCode}): {result.Message}");
            }
        }

        private void ValidateActiveSession(string roomId)
        {
            ValidateRoomId(roomId);
            if (string.IsNullOrWhiteSpace(_sessionToken))
            {
                throw new InvalidOperationException("Gateway session is not authenticated.");
            }
        }

        private static void ValidateSpec(MultiplayerRoomLaunchSpec spec)
        {
            if (spec == null) throw new ArgumentNullException(nameof(spec));
            if (string.IsNullOrWhiteSpace(spec.Region)) throw new ArgumentException("Region is required.", nameof(spec));
            if (string.IsNullOrWhiteSpace(spec.ServerId)) throw new ArgumentException("ServerId is required.", nameof(spec));
            if (spec.MaxPlayers <= 0) throw new ArgumentOutOfRangeException(nameof(spec));
        }

        private static void ValidateRoomId(string roomId)
        {
            if (string.IsNullOrWhiteSpace(roomId))
            {
                throw new ArgumentException("RoomId is required.", nameof(roomId));
            }
        }

        private static string NewCommandId(string operation)
        {
            return operation + ":" + Guid.NewGuid().ToString("N");
        }
    }
}
