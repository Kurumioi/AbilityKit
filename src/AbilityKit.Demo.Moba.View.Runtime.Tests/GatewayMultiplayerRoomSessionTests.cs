using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AbilityKit.Game.Battle.Agent;
using AbilityKit.Game.Flow;
using Xunit;

namespace AbilityKit.Demo.Moba.View.Runtime.Tests;

public sealed class GatewayMultiplayerRoomSessionTests
{
    [Fact]
    public void SnapshotProvider_FirstStoreApply_PublishesProjectedSnapshot()
    {
        var store = new ClientRoomStore();
        using var provider = new ClientRoomSnapshotProvider(store);
        MultiplayerRoomSnapshot? published = null;
        provider.OnSnapshotChanged += snapshot => published = snapshot;

        var result = store.ApplySnapshot(Snapshot(ClientRoomPhase.Loading, revision: 3, sequence: 1));

        Assert.Equal(ClientRoomSnapshotApplyResult.Applied, result);
        Assert.NotNull(published);
        Assert.Equal("room-1", published!.RoomId);
        Assert.Equal(MultiplayerRoomPhase.Loading, published.Phase);
        Assert.Equal(3, published.RoomRevision);
    }

    [Fact]
    public async Task ReportAssetsLoaded_UsesAuthoritativeManifestIdentity()
    {
        var client = new StubGatewayRoomClient();
        client.Snapshots.Enqueue(Snapshot(ClientRoomPhase.Lobby, revision: 1, sequence: 1));
        var store = new ClientRoomStore();
        var session = NewSession(client, store);
        var spec = NewSpec();

        await session.JoinRoomAsync(spec, "room-1", CancellationToken.None);
        store.ApplySnapshot(Snapshot(ClientRoomPhase.Loading, revision: 2, sequence: 2));
        await session.ReportAssetsLoadedAsync("room-1", CancellationToken.None);

        Assert.Equal(7, client.ReportGeneration);
        Assert.Equal(4, client.ReportManifestVersion);
        Assert.Equal("manifest-hash", client.ReportManifestHash);
        Assert.StartsWith("assets-loaded:", client.ReportCommandId);
    }

    [Fact]
    public async Task WaitForBattleStart_WaitsPastStartingUntilCommittedIdentityExists()
    {
        var client = new StubGatewayRoomClient();
        client.Snapshots.Enqueue(Snapshot(ClientRoomPhase.Lobby, revision: 1, sequence: 1));
        client.Snapshots.Enqueue(Snapshot(ClientRoomPhase.Starting, revision: 2, sequence: 2));
        client.Snapshots.Enqueue(Snapshot(
            ClientRoomPhase.InBattle,
            revision: 3,
            sequence: 3,
            battleId: "battle-1",
            worldId: 42));
        var store = new ClientRoomStore();
        var session = NewSession(client, store);

        await session.JoinRoomAsync(NewSpec(), "room-1", CancellationToken.None);
        await session.WaitForBattleStartAsync("room-1", CancellationToken.None);

        Assert.Equal(3, client.GetSnapshotCalls);
        Assert.Equal(ClientRoomPhase.InBattle, store.Current.Phase);
        Assert.Equal("battle-1", store.Current.BattleId);
        Assert.Equal(42UL, store.Current.WorldId);
    }

    [Fact]
    public async Task PushSynchronizer_IgnoresNonRoomPush()
    {
        var client = new StubGatewayRoomClient();
        var store = new ClientRoomStore();
        var refreshCalls = 0;
        var synchronizer = new ClientRoomPushSynchronizer(
            client,
            store,
            _ =>
            {
                refreshCalls++;
                return Task.CompletedTask;
            });

        var handled = await synchronizer.HandleServerPushAsync(123u, default);

        Assert.False(handled);
        Assert.Null(store.Current);
        Assert.Equal(0, refreshCalls);
    }

    [Fact]
    public async Task PushSynchronizer_CoalescesConcurrentStaleRefreshes()
    {
        var client = new StubGatewayRoomClient();
        client.PushSnapshots.Enqueue(Snapshot(ClientRoomPhase.Lobby, 3, 3));
        client.PushSnapshots.Enqueue(Snapshot(ClientRoomPhase.Lobby, 5, 5));
        var store = new ClientRoomStore();
        store.ApplySnapshot(Snapshot(ClientRoomPhase.Lobby, 1, 1));
        var refreshCalls = 0;
        var refreshGate = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var synchronizer = new ClientRoomPushSynchronizer(
            client,
            store,
            _ =>
            {
                refreshCalls++;
                return refreshGate.Task;
            });

        var first = synchronizer.HandleServerPushAsync(StubGatewayRoomClient.RoomPushOpCode, default);
        var second = synchronizer.HandleServerPushAsync(StubGatewayRoomClient.RoomPushOpCode, default);

        Assert.Equal(1, refreshCalls);
        Assert.Equal(5, store.Current.RoomRevision);
        refreshGate.SetResult(true);
        Assert.True(await first);
        Assert.True(await second);
    }

    private static GatewayMultiplayerRoomSession NewSession(
        StubGatewayRoomClient client,
        ClientRoomStore store)
    {
        return new GatewayMultiplayerRoomSession(
            client,
            store,
            requestTimeout: TimeSpan.FromSeconds(1),
            pollInterval: TimeSpan.FromMilliseconds(1),
            battleStartTimeout: TimeSpan.FromSeconds(1));
    }

    private static MultiplayerRoomLaunchSpec NewSpec()
    {
        return new MultiplayerRoomLaunchSpec
        {
            SessionToken = "token-1",
            Region = "dev",
            ServerId = "server-1",
            RoomType = "moba",
            RoomTitle = "test",
            MaxPlayers = 2
        };
    }

    private static ClientRoomSnapshot Snapshot(
        ClientRoomPhase phase,
        long revision,
        long sequence,
        string battleId = "",
        ulong worldId = 0)
    {
        return new ClientRoomSnapshot
        {
            RoomId = "room-1",
            Phase = phase,
            LaunchGeneration = 7,
            LaunchManifestVersion = 4,
            LaunchManifestHash = "manifest-hash",
            RoomRevision = revision,
            LastEventSequence = sequence,
            BattleId = battleId,
            WorldId = worldId
        };
    }

    private sealed class StubGatewayRoomClient : IGatewayRoomClient
    {
        public const uint RoomPushOpCode = 777u;

        public readonly Queue<ClientRoomSnapshot> Snapshots = new();
        public readonly Queue<ClientRoomSnapshot> PushSnapshots = new();
        public int GetSnapshotCalls { get; private set; }
        public long ReportGeneration { get; private set; }
        public int ReportManifestVersion { get; private set; }
        public string ReportManifestHash { get; private set; } = string.Empty;
        public string ReportCommandId { get; private set; } = string.Empty;

        public Task<GatewayTimeSyncResult> TimeSyncAsync(uint timeSyncOpCode, long clientSendTicks, TimeSpan? timeout = null, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<string> GuestLoginAsync(uint guestLoginOpCode, TimeSpan? timeout = null, CancellationToken cancellationToken = default)
            => Task.FromResult("guest-token");

        public Task<GatewayCreateRoomResult> CreateRoomAsync(string sessionToken, string region, string serverId, string roomType, string title, bool isPublic, int maxPlayers, IReadOnlyDictionary<string, string> tags, TimeSpan? timeout = null, CancellationToken cancellationToken = default)
            => Task.FromResult(new GatewayCreateRoomResult("room-1", 1));

        public Task<GatewayJoinRoomResult> JoinRoomAsync(string sessionToken, string region, string serverId, string roomId, TimeSpan? timeout = null, CancellationToken cancellationToken = default)
            => Task.FromResult(new GatewayJoinRoomResult(1, string.Empty, default));

        public Task<GatewayRoomSnapshotResult> SetReadyAsync(string sessionToken, string roomId, bool ready, TimeSpan? timeout = null, CancellationToken cancellationToken = default)
            => Task.FromResult(new GatewayRoomSnapshotResult(roomId, 1));

        public Task<GatewayRoomSnapshotResult> PickHeroAsync(string sessionToken, string roomId, int heroId, int teamId, int spawnPointId, int level, int attributeTemplateId, int basicAttackSkillId, IReadOnlyList<int> skillIds, TimeSpan? timeout = null, CancellationToken cancellationToken = default)
            => Task.FromResult(new GatewayRoomSnapshotResult(roomId, 1));

        public Task<GatewayRoomOperationResult> BeginLoadingAsync(string sessionToken, string roomId, long? expectedRevision, string commandId, TimeSpan? timeout = null, CancellationToken cancellationToken = default)
            => Task.FromResult(new GatewayRoomOperationResult(true, true, 0, string.Empty, expectedRevision ?? 0, null!));

        public Task<GatewayRoomOperationResult> ReportAssetsLoadedAsync(string sessionToken, string roomId, long launchGeneration, int manifestVersion, string manifestHash, string commandId, TimeSpan? timeout = null, CancellationToken cancellationToken = default)
        {
            ReportGeneration = launchGeneration;
            ReportManifestVersion = manifestVersion;
            ReportManifestHash = manifestHash;
            ReportCommandId = commandId;
            return Task.FromResult(new GatewayRoomOperationResult(true, true, 0, string.Empty, 2, Snapshot(ClientRoomPhase.Starting, 3, 3)));
        }

        public Task<GatewayRoomOperationResult> CancelLoadingAsync(string sessionToken, string roomId, long? expectedRevision, string commandId, TimeSpan? timeout = null, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<GatewayGetSnapshotResult> GetSnapshotAsync(string sessionToken, string roomId, TimeSpan? timeout = null, CancellationToken cancellationToken = default)
        {
            GetSnapshotCalls++;
            var snapshot = Snapshots.Dequeue();
            return Task.FromResult(new GatewayGetSnapshotResult(true, roomId, 1, snapshot, string.Empty));
        }

        public Task<GatewayRestoreRoomResult> RestoreRoomAsync(string sessionToken, string region, string serverId, TimeSpan? timeout = null, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public ClientRoomSnapshot DeserializeRoomStateChangedPush(ArraySegment<byte> payload)
            => PushSnapshots.Dequeue();

        public bool IsRoomStateChangedPush(uint opCode) => opCode == RoomPushOpCode;
    }
}
