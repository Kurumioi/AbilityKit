using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AbilityKit.Demo.Shooter.View;
using Xunit;

namespace AbilityKit.Demo.Shooter.Runtime.Tests;

public sealed class ShooterRoomGatewayFlowTests
{
    [Fact]
    public async Task RoomGatewayFlowCreatesReadyStartsSubscribesAndBuildsBattleInputContext()
    {
        var roomClient = new ScriptedShooterRoomClient
        {
            JoinCurrentPlayerId = 121u
        };
        var flow = new ShooterRoomGatewayFlow(roomClient);
        var launchSpec = new ShooterRoomLaunchSpec(
            "local",
            "dev",
            "Shooter Room",
            ShooterGameplay.DefaultMaxPlayers,
            ShooterGameplay.GameplayId,
            ruleSetId: 1,
            configVersion: 1,
            protocolVersion: 1,
            ShooterGameplay.WorldType,
            "client-a",
            new Dictionary<string, string>
            {
                ["mode"] = "duo",
                ["syncTemplateId"] = "runtime-snapshot-interpolation",
                ["syncModel"] = "2",
                ["networkEnvironmentId"] = "wan-90ms",
                ["carrierName"] = "server",
                ["enableAuthoritativeWorld"] = "True",
                ["interpolationEnabled"] = "True",
                ["inputDelayFrames"] = "4"
            },
            "runtime-snapshot-interpolation",
            syncModel: 2,
            networkEnvironmentId: "wan-90ms",
            carrierName: "server",
            enableAuthoritativeWorld: true,
            interpolationEnabled: true,
            inputDelayFrames: 4);

        var result = await flow.CreateReadyStartAndSubscribeAsync("session-token", launchSpec, playerId: 21u);

        Assert.Equal("create:shooter", roomClient.Calls[0]);
        Assert.Equal("join:room-1", roomClient.Calls[1]);
        Assert.Equal("ready:room-1:True", roomClient.Calls[2]);
        Assert.Equal("start:room-1:2", roomClient.Calls[3]);
        Assert.Equal("subscribe:room-1:battle-1", roomClient.Calls[4]);
        Assert.Equal("session-token", roomClient.LastCreateRequest.SessionToken);
        Assert.Equal("local", roomClient.LastCreateRequest.Region);
        Assert.Equal("dev", roomClient.LastCreateRequest.ServerId);
        Assert.Equal(ShooterGameplay.RoomType, roomClient.LastCreateRequest.RoomType);
        Assert.Equal(ShooterGameplay.DefaultMaxPlayers, roomClient.LastCreateRequest.MaxPlayers);
        Assert.NotNull(roomClient.LastCreateRequest.Tags);
        var createTags = roomClient.LastCreateRequest.Tags!;
        Assert.Equal("duo", createTags["mode"]);
        Assert.Equal("runtime-snapshot-interpolation", createTags["syncTemplateId"]);
        Assert.Equal("2", createTags["syncModel"]);
        Assert.Equal("wan-90ms", createTags["networkEnvironmentId"]);
        Assert.Equal("server", createTags["carrierName"]);
        Assert.Equal("True", createTags["enableAuthoritativeWorld"]);
        Assert.Equal("True", createTags["interpolationEnabled"]);
        Assert.Equal("4", createTags["inputDelayFrames"]);
        Assert.Equal(ShooterGameplay.WorldType, roomClient.LastStartBattleRequest.WorldType);
        Assert.Equal("client-a", roomClient.LastStartBattleRequest.ClientId);
        Assert.Equal("runtime-snapshot-interpolation", roomClient.LastStartBattleRequest.SyncTemplateId);
        Assert.Equal(2, roomClient.LastStartBattleRequest.SyncModel);
        Assert.Equal("wan-90ms", roomClient.LastStartBattleRequest.NetworkEnvironmentId);
        Assert.Equal("server", roomClient.LastStartBattleRequest.CarrierName);
        Assert.True(roomClient.LastStartBattleRequest.EnableAuthoritativeWorld);
        Assert.True(roomClient.LastStartBattleRequest.InterpolationEnabled);
        Assert.Equal(4, roomClient.LastStartBattleRequest.InputDelayFrames);
        Assert.Equal("room-1", result.RoomId);
        Assert.Equal(1001ul, result.NumericRoomId);
        Assert.Equal("battle-1", result.BattleId);
        Assert.Equal(9001ul, result.WorldId);
        Assert.Equal(121u, result.PlayerId);
        Assert.True(result.CanStart);
        Assert.True(result.Started);
        Assert.True(result.Subscribed);
        Assert.Equal(30, result.WorldStartAnchor.StartFrame);
        Assert.Equal(1200000L, result.ServerNowTicks);
        Assert.Equal(33, result.TargetFrame);
        Assert.Equal(3, result.CatchUpFrames);
        Assert.Equal(ShooterRoomGatewayEntryKind.TeamLobby, result.EntryKind);
        Assert.Equal("subscribed", result.Message);

        var inputContext = result.CreateBattleInputContext(frame: 8);
        Assert.Equal("session-token", inputContext.SessionToken);
        Assert.Equal("battle-1", inputContext.BattleId);
        Assert.Equal(9001ul, inputContext.WorldId);
        Assert.Equal(8, inputContext.Frame);
        Assert.Equal(121u, inputContext.PlayerId);
    }

    [Fact]
    public async Task RoomGatewayFlowJoinsExistingRoomWithoutCreate()
    {
        var roomClient = new ScriptedShooterRoomClient
        {
            JoinCurrentPlayerId = 131u
        };
        var flow = new ShooterRoomGatewayFlow(roomClient);

        var result = await flow.JoinReadyStartAndSubscribeAsync(
            "session-token",
            "existing-room",
            ShooterRoomLaunchSpec.CreateDefault("client-b"),
            playerId: 31u);

        Assert.DoesNotContain(roomClient.Calls, call => call.StartsWith("create:", StringComparison.Ordinal));
        Assert.Equal("join:existing-room", roomClient.Calls[0]);
        Assert.Equal("ready:existing-room:True", roomClient.Calls[1]);
        Assert.Equal("start:existing-room:2", roomClient.Calls[2]);
        Assert.Equal("subscribe:existing-room:battle-1", roomClient.Calls[3]);
        Assert.Equal("existing-room", result.RoomId);
        Assert.Equal("battle-1", result.BattleId);
        Assert.Equal(131u, result.PlayerId);
        Assert.Equal(30, result.WorldStartAnchor.StartFrame);
        Assert.Equal(33, result.TargetFrame);
        Assert.Equal(ShooterRoomGatewayEntryKind.TeamLobby, result.EntryKind);
    }

    [Fact]
    public async Task RoomGatewayFlowReconnectsRunningBattleWithoutReadyOrStart()
    {
        var roomClient = new ScriptedShooterRoomClient
        {
            JoinKind = ShooterGatewayRoomJoinKind.Reconnect,
            JoinBattleId = "battle-running",
            JoinWorldId = 9101ul,
            JoinServerNowTicks = 1123456L,
            JoinWorldStartAnchor = new ShooterGatewayWorldStartAnchor(123456L, 10000000L, 18, 1d / 30d),
            JoinCanStart = false,
            JoinCurrentPlayerId = 141u
        };
        var flow = new ShooterRoomGatewayFlow(roomClient);

        var result = await flow.JoinReadyStartAndSubscribeAsync(
            "session-token",
            "running-room",
            ShooterRoomLaunchSpec.CreateDefault("client-reconnect"),
            playerId: 41u);

        Assert.Equal(2, roomClient.Calls.Count);
        Assert.Equal("join:running-room", roomClient.Calls[0]);
        Assert.Equal("subscribe:running-room:battle-running", roomClient.Calls[1]);
        Assert.DoesNotContain(roomClient.Calls, call => call.StartsWith("ready:", StringComparison.Ordinal));
        Assert.DoesNotContain(roomClient.Calls, call => call.StartsWith("start:", StringComparison.Ordinal));
        Assert.Equal(ShooterRoomGatewayEntryKind.Reconnect, result.EntryKind);
        Assert.Equal("battle-running", result.BattleId);
        Assert.Equal(9101ul, result.WorldId);
        Assert.Equal(1123456L, result.ServerNowTicks);
        Assert.Equal(21, result.TargetFrame);
        Assert.Equal(3, result.CatchUpFrames);
        Assert.False(result.CanStart);
        Assert.True(result.Started);
        Assert.True(result.Subscribed);
        Assert.Equal(141u, result.PlayerId);
    }

    [Fact]
    public async Task RoomGatewayFlowLateJoinsRunningBattleWithoutReadyOrStart()
    {
        var roomClient = new ScriptedShooterRoomClient
        {
            JoinKind = ShooterGatewayRoomJoinKind.LateJoin,
            JoinBattleId = "battle-mid",
            JoinWorldId = 9201ul,
            JoinServerNowTicks = 2123456L,
            JoinWorldStartAnchor = new ShooterGatewayWorldStartAnchor(123456L, 10000000L, 24, 1d / 30d),
            JoinCanStart = false,
            JoinCurrentPlayerId = 142u
        };
        var flow = new ShooterRoomGatewayFlow(roomClient);

        var result = await flow.JoinReadyStartAndSubscribeAsync(
            "session-token",
            "mid-room",
            ShooterRoomLaunchSpec.CreateDefault("client-late"),
            playerId: 42u);

        Assert.Equal(2, roomClient.Calls.Count);
        Assert.Equal("join:mid-room", roomClient.Calls[0]);
        Assert.Equal("subscribe:mid-room:battle-mid", roomClient.Calls[1]);
        Assert.Equal(ShooterRoomGatewayEntryKind.LateJoin, result.EntryKind);
        Assert.Equal("battle-mid", result.BattleId);
        Assert.Equal(9201ul, result.WorldId);
        Assert.Equal(2123456L, result.ServerNowTicks);
        Assert.Equal(30, result.TargetFrame);
        Assert.Equal(6, result.CatchUpFrames);
        Assert.False(result.CanStart);
        Assert.True(result.Started);
        Assert.True(result.Subscribed);
        Assert.Equal(142u, result.PlayerId);
    }
}
