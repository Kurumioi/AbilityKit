using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AbilityKit.Demo.Shooter.View;

namespace AbilityKit.Demo.Shooter.Runtime.Tests;

internal sealed class ScriptedShooterRoomClient : IShooterRoomGatewayRoomClient
{
    public readonly List<string> Calls = new List<string>();

    public ShooterGatewayCreateRoomRequest LastCreateRequest { get; private set; }

    public ShooterGatewayJoinRoomRequest LastJoinRequest { get; private set; }

    public ShooterGatewayReadyRequest LastReadyRequest { get; private set; }

    public ShooterGatewayStartBattleRequest LastStartBattleRequest { get; private set; }

    public ShooterGatewayStateSyncSubscriptionRequest LastSubscribeRequest { get; private set; }

    public ShooterGatewayFullStateSyncRequest LastFullStateSyncRequest { get; private set; }

    public ShooterGatewayRoomJoinKind JoinKind { get; set; } = ShooterGatewayRoomJoinKind.TeamLobby;

    public string JoinBattleId { get; set; } = "battle-prestart";

    public ulong JoinWorldId { get; set; } = 0ul;

    public long JoinServerNowTicks { get; set; } = 223456L;

    public bool JoinCanStart { get; set; } = true;

    public ShooterGatewayWorldStartAnchor JoinWorldStartAnchor { get; set; } = new ShooterGatewayWorldStartAnchor(123456L, 10000000L, 12, 1d / 30d);

    public Task<ShooterGatewayGuestLoginResult> GuestLoginAsync(ShooterGatewayGuestLoginRequest request, TimeSpan? timeout = null, CancellationToken cancellationToken = default)
    {
        Calls.Add("guest-login:" + request.GuestId);
        return Task.FromResult(new ShooterGatewayGuestLoginResult(true, "session-token", "account-1", "guest-login-ok"));
    }

    public Task<ShooterGatewayListRoomsResult> ListRoomsAsync(ShooterGatewayListRoomsRequest request, TimeSpan? timeout = null, CancellationToken cancellationToken = default)
    {
        Calls.Add("list-rooms:" + request.Region + ":" + request.ServerId + ":" + request.RoomType);
        return Task.FromResult(new ShooterGatewayListRoomsResult(true, Array.Empty<ShooterGatewayRoomSummary>(), 0, "list-rooms-ok"));
    }

    public Task<ShooterGatewayCreateRoomResult> CreateRoomAsync(ShooterGatewayCreateRoomRequest request, TimeSpan? timeout = null, CancellationToken cancellationToken = default)
    {
        LastCreateRequest = request;
        Calls.Add("create:" + request.RoomType);
        return Task.FromResult(new ShooterGatewayCreateRoomResult(true, "room-1", 1001ul, "created"));
    }

    public Task<ShooterGatewayJoinRoomResult> JoinRoomAsync(ShooterGatewayJoinRoomRequest request, TimeSpan? timeout = null, CancellationToken cancellationToken = default)
    {
        LastJoinRequest = request;
        Calls.Add("join:" + request.RoomId);
        var anchor = JoinWorldStartAnchor;
        return Task.FromResult(new ShooterGatewayJoinRoomResult(
            true,
            request.RoomId,
            1001ul,
            in anchor,
            "joined",
            JoinBattleId,
            JoinCanStart,
            JoinKind,
            JoinServerNowTicks,
            JoinWorldId));
    }

    public Task<ShooterGatewayRoomSnapshotResult> SetReadyAsync(ShooterGatewayReadyRequest request, TimeSpan? timeout = null, CancellationToken cancellationToken = default)
    {
        LastReadyRequest = request;
        Calls.Add("ready:" + request.RoomId + ":" + request.Ready);
        return Task.FromResult(new ShooterGatewayRoomSnapshotResult(true, request.RoomId, 1001ul, "ready", "battle-ready", canStart: true));
    }

    public Task<ShooterGatewayStartBattleResult> StartBattleAsync(ShooterGatewayStartBattleRequest request, TimeSpan? timeout = null, CancellationToken cancellationToken = default)
    {
        LastStartBattleRequest = request;
        Calls.Add("start:" + request.RoomId + ":" + request.GameplayId);
        var anchor = new ShooterGatewayWorldStartAnchor(200000L, 10000000L, 30, 1d / 30d);
        return Task.FromResult(new ShooterGatewayStartBattleResult(true, "battle-1", 9001ul, started: true, in anchor, 1200000L, "started"));
    }

    public Task<ShooterGatewayStateSyncSubscriptionResult> SubscribeStateSyncAsync(ShooterGatewayStateSyncSubscriptionRequest request, TimeSpan? timeout = null, CancellationToken cancellationToken = default)
    {
        LastSubscribeRequest = request;
        Calls.Add("subscribe:" + request.RoomId + ":" + request.BattleId);
        return Task.FromResult(new ShooterGatewayStateSyncSubscriptionResult(true, "subscribed"));
    }

    public Task<ShooterGatewayFullStateSyncRequestResult> RequestFullStateSyncAsync(ShooterGatewayFullStateSyncRequest request, TimeSpan? timeout = null, CancellationToken cancellationToken = default)
    {
        LastFullStateSyncRequest = request;
        Calls.Add("request-full-state:" + request.RoomId + ":" + request.BattleId + ":" + request.Reason);
        return Task.FromResult(new ShooterGatewayFullStateSyncRequestResult(true, true, "accepted", 123456789L));
    }

    public Task<ShooterGatewayRestoreRoomResult> RestoreRoomAsync(ShooterGatewayRestoreRoomRequest request, TimeSpan? timeout = null, CancellationToken cancellationToken = default)
    {
        Calls.Add("restore:" + request.Region + ":" + request.ServerId);
        var anchor = JoinWorldStartAnchor;
        return Task.FromResult(new ShooterGatewayRestoreRoomResult(
            true,
            true,
            false,
            "room-1",
            1001ul,
            in anchor,
            "restored",
            JoinBattleId,
            JoinCanStart,
            JoinKind,
            JoinServerNowTicks,
            JoinWorldId));
    }
}
