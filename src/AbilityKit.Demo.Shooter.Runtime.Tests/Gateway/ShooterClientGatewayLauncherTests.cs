using System.Threading.Tasks;
using AbilityKit.Demo.Shooter.Runtime;
using AbilityKit.Demo.Shooter.View;
using AbilityKit.Protocol.Room;
using AbilityKit.Protocol.Shooter;
using Xunit;

namespace AbilityKit.Demo.Shooter.Runtime.Tests;

public sealed class ShooterClientGatewayLauncherTests
{
    [Fact]
    public async Task ClientGatewayLauncherCreatesRoomSessionAndBattleHandle()
    {
        var runtime = new ShooterBattleRuntimePort();
        var presentation = new ShooterPresentationFacade();
        var transport = new ScriptedShooterGatewayLaunchTransport();
        var launcher = new ShooterClientGatewayLauncher(transport);
        var start = new ShooterStartGamePayload(
            "launcher-session",
            30,
            4903,
            new[]
            {
                new ShooterStartPlayer(41, "P41", 0f, 0f),
                new ShooterStartPlayer(42, "P42", 5f, 0f)
            });

        var launched = await launcher.CreateReadyStartAndSubscribeAsync(
            runtime,
            presentation,
            start,
            "session-token",
            ShooterRoomLaunchSpec.CreateDefault("client-launcher"),
            playerId: 41u);

        Assert.True(launched.Session.IsStarted);
        Assert.True(launched.Session.HasGateway);
        Assert.Equal(0, launched.Session.CurrentFrame);
        Assert.Equal(0, presentation.ViewModel.Frame);
        Assert.Equal("room-launch", launched.Flow.RoomId);
        Assert.Equal("battle-launch", launched.Flow.BattleId);
        Assert.Equal(9041ul, launched.Flow.WorldId);
        Assert.Equal(41u, launched.Flow.PlayerId);
        Assert.Equal(launched.Session, launched.Battle.Session);
        Assert.Equal("battle-launch", launched.Battle.BattleId);
        Assert.Equal(5, transport.OpCodes.Count);
        Assert.Equal(RoomGatewayOpCodes.CreateRoom, transport.OpCodes[0]);
        Assert.Equal(RoomGatewayOpCodes.JoinRoom, transport.OpCodes[1]);
        Assert.Equal(RoomGatewayOpCodes.SetReady, transport.OpCodes[2]);
        Assert.Equal(RoomGatewayOpCodes.StartBattle, transport.OpCodes[3]);
        Assert.Equal(RoomGatewayOpCodes.SubscribeStateSync, transport.OpCodes[4]);

        var submit = await launched.Battle.SubmitLocalInputToGatewayAsync(moveX: 1f, moveY: 0f, aimX: 1f, aimY: 0f, fire: false);

        Assert.True(submit.Remote.Success);
        Assert.Equal(RoomGatewayOpCodes.SubmitBattleInput, transport.OpCodes[5]);
        var wire = WireRoomGatewayBinary.Deserialize<WireSubmitBattleInputReq>(transport.LastPayload);
        Assert.Equal("session-token", wire.SessionToken);
        Assert.Equal("battle-launch", wire.BattleId);
        Assert.Equal(9041ul, wire.WorldId);
        Assert.Equal(launched.Session.CurrentFrame, wire.Frame);
        Assert.Equal(41u, wire.PlayerId);
    }
}
