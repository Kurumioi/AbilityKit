using System;
using System.Threading.Tasks;
using AbilityKit.Demo.Shooter.View;
using Xunit;

namespace AbilityKit.Demo.Shooter.Runtime.Tests;

public sealed class ShooterRoomGatewayFlowTests
{
    [Fact]
    public async Task RoomGatewayFlowCreatesReadyStartsSubscribesAndBuildsBattleInputContext()
    {
        var roomClient = new ScriptedShooterRoomClient();
        var flow = new ShooterRoomGatewayFlow(roomClient);
        var launchSpec = ShooterRoomLaunchSpec.CreateDefault("client-a");

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
        Assert.Equal(ShooterGameplay.WorldType, roomClient.LastStartBattleRequest.WorldType);
        Assert.Equal("client-a", roomClient.LastStartBattleRequest.ClientId);
        Assert.Equal("room-1", result.RoomId);
        Assert.Equal(1001ul, result.NumericRoomId);
        Assert.Equal("battle-1", result.BattleId);
        Assert.Equal(9001ul, result.WorldId);
        Assert.Equal(21u, result.PlayerId);
        Assert.True(result.CanStart);
        Assert.True(result.Started);
        Assert.True(result.Subscribed);
        Assert.Equal(12, result.WorldStartAnchor.StartFrame);
        Assert.Equal("subscribed", result.Message);

        var inputContext = result.CreateBattleInputContext(frame: 8);
        Assert.Equal("session-token", inputContext.SessionToken);
        Assert.Equal("battle-1", inputContext.BattleId);
        Assert.Equal(9001ul, inputContext.WorldId);
        Assert.Equal(8, inputContext.Frame);
        Assert.Equal(21u, inputContext.PlayerId);
    }

    [Fact]
    public async Task RoomGatewayFlowJoinsExistingRoomWithoutCreate()
    {
        var roomClient = new ScriptedShooterRoomClient();
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
        Assert.Equal(31u, result.PlayerId);
    }
}
