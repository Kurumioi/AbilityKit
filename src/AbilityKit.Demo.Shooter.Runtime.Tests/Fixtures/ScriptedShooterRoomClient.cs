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
        var anchor = new ShooterGatewayWorldStartAnchor(123456L, 10000000L, 12, 1d / 30d);
        return Task.FromResult(new ShooterGatewayJoinRoomResult(true, request.RoomId, 1001ul, in anchor, "joined", "battle-prestart", canStart: true));
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
        return Task.FromResult(new ShooterGatewayStartBattleResult(true, "battle-1", 9001ul, started: true, "started"));
    }

    public Task<ShooterGatewayStateSyncSubscriptionResult> SubscribeStateSyncAsync(ShooterGatewayStateSyncSubscriptionRequest request, TimeSpan? timeout = null, CancellationToken cancellationToken = default)
    {
        LastSubscribeRequest = request;
        Calls.Add("subscribe:" + request.RoomId + ":" + request.BattleId);
        return Task.FromResult(new ShooterGatewayStateSyncSubscriptionResult(true, "subscribed"));
    }
}
