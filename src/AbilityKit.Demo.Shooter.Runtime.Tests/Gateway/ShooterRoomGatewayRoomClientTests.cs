using System.Collections.Generic;
using System.Threading.Tasks;
using AbilityKit.Demo.Shooter.View;
using AbilityKit.Protocol.Room;
using Xunit;

namespace AbilityKit.Demo.Shooter.Runtime.Tests;

public sealed class ShooterRoomGatewayRoomClientTests
{
    [Fact]
    public async Task RoomGatewayRoomClientUsesGenericRoomLifecycleProtocol()
    {
        var transport = new RecordingShooterRoomFlowTransport();
        var roomClient = new ShooterRoomGatewayRoomClient(transport);

        transport.SetResponse(new WireCreateRoomRes
        {
            Success = true,
            RoomId = "room-1",
            NumericRoomId = 1001ul,
            Message = "created"
        });
        var create = await roomClient.CreateRoomAsync(new ShooterGatewayCreateRoomRequest(
            "session-token",
            "cn",
            "server-a",
            "shooter",
            "Shooter Room",
            isPublic: true,
            maxPlayers: 4,
            tags: new Dictionary<string, string> { ["mode"] = "duo" }));
        Assert.Equal(RoomGatewayOpCodes.CreateRoom, transport.LastOpCode);
        var createWire = WireRoomGatewayBinary.Deserialize<WireCreateRoomReq>(transport.LastPayload);
        Assert.Equal("session-token", createWire.SessionToken);
        Assert.Equal("cn", createWire.Region);
        Assert.Equal("server-a", createWire.ServerId);
        Assert.Equal("shooter", createWire.RoomType);
        Assert.Equal("Shooter Room", createWire.Title);
        Assert.True(createWire.IsPublic);
        Assert.Equal(4, createWire.MaxPlayers);
        Assert.NotNull(createWire.Tags);
        Assert.Equal("duo", createWire.Tags!["mode"]);
        Assert.True(create.Success);
        Assert.Equal("room-1", create.RoomId);
        Assert.Equal(1001ul, create.NumericRoomId);
        Assert.Equal("created", create.Message);

        transport.SetResponse(new WireJoinRoomRes
        {
            Success = true,
            RoomId = "room-1",
            NumericRoomId = 1001ul,
            Snapshot = new WireRoomSnapshot { BattleId = "battle-1", CanStart = true },
            WorldStartAnchor = new WireWorldStartAnchor
            {
                StartServerTicks = 123456L,
                ServerTickFrequency = 10000000L,
                StartFrame = 12,
                FixedDeltaSeconds = 1d / 30d
            },
            Message = "joined"
        });
        var join = await roomClient.JoinRoomAsync(new ShooterGatewayJoinRoomRequest("session-token", "cn", "server-a", "room-1"));
        Assert.Equal(RoomGatewayOpCodes.JoinRoom, transport.LastOpCode);
        var joinWire = WireRoomGatewayBinary.Deserialize<WireJoinRoomReq>(transport.LastPayload);
        Assert.Equal("session-token", joinWire.SessionToken);
        Assert.Equal("cn", joinWire.Region);
        Assert.Equal("server-a", joinWire.ServerId);
        Assert.Equal("room-1", joinWire.RoomId);
        Assert.True(join.Success);
        Assert.Equal("battle-1", join.BattleId);
        Assert.True(join.CanStart);
        Assert.Equal(123456L, join.WorldStartAnchor.StartServerTicks);
        Assert.Equal(12, join.WorldStartAnchor.StartFrame);

        transport.SetResponse(new WireRoomSnapshotRes
        {
            Success = true,
            RoomId = "room-1",
            NumericRoomId = 1001ul,
            Snapshot = new WireRoomSnapshot { BattleId = "battle-1", CanStart = true },
            Message = "ready"
        });
        var ready = await roomClient.SetReadyAsync(new ShooterGatewayReadyRequest("session-token", "room-1", ready: true));
        Assert.Equal(RoomGatewayOpCodes.SetReady, transport.LastOpCode);
        var readyWire = WireRoomGatewayBinary.Deserialize<WireRoomReadyReq>(transport.LastPayload);
        Assert.Equal("session-token", readyWire.SessionToken);
        Assert.Equal("room-1", readyWire.RoomId);
        Assert.True(readyWire.Ready);
        Assert.True(ready.Success);
        Assert.Equal("battle-1", ready.BattleId);
        Assert.True(ready.CanStart);

        transport.SetResponse(new WireStartRoomBattleRes
        {
            Success = true,
            BattleId = "battle-1",
            WorldId = 9001ul,
            Started = true,
            WorldStartAnchor = new WireWorldStartAnchor
            {
                StartServerTicks = 223456L,
                ServerTickFrequency = 10000000L,
                StartFrame = 0,
                FixedDeltaSeconds = 1d / 30d
            },
            ServerNowTicks = 323456L,
            Message = "started"
        });
        var start = await roomClient.StartBattleAsync(new ShooterGatewayStartBattleRequest("session-token", "room-1", 2, 1, 3, 1, "shooter-runtime", "client-a"));
        Assert.Equal(RoomGatewayOpCodes.StartBattle, transport.LastOpCode);
        var startWire = WireRoomGatewayBinary.Deserialize<WireStartRoomBattleReq>(transport.LastPayload);
        Assert.Equal("session-token", startWire.SessionToken);
        Assert.Equal("room-1", startWire.RoomId);
        Assert.Equal(2, startWire.GameplayId);
        Assert.Equal(1, startWire.RuleSetId);
        Assert.Equal(3, startWire.ConfigVersion);
        Assert.Equal(1, startWire.ProtocolVersion);
        Assert.Equal("shooter-runtime", startWire.WorldType);
        Assert.Equal("client-a", startWire.ClientId);
        Assert.True(start.Success);
        Assert.True(start.Started);
        Assert.Equal("battle-1", start.BattleId);
        Assert.Equal(9001ul, start.WorldId);
        Assert.Equal(223456L, start.WorldStartAnchor.StartServerTicks);
        Assert.Equal(323456L, start.ServerNowTicks);

        transport.SetResponse(new WireSubscribeStateSyncRes
        {
            Success = true,
            Message = "subscribed"
        });
        var subscribe = await roomClient.SubscribeStateSyncAsync(new ShooterGatewayStateSyncSubscriptionRequest("session-token", "battle-1", "room-1"));
        Assert.Equal(RoomGatewayOpCodes.SubscribeStateSync, transport.LastOpCode);
        var subscribeWire = WireRoomGatewayBinary.Deserialize<WireSubscribeStateSyncReq>(transport.LastPayload);
        Assert.Equal("session-token", subscribeWire.SessionToken);
        Assert.Equal("battle-1", subscribeWire.BattleId);
        Assert.Equal("room-1", subscribeWire.RoomId);
        Assert.True(subscribe.Success);
        Assert.Equal("subscribed", subscribe.Message);
    }
}
