using System.Threading.Tasks;
using AbilityKit.Demo.Shooter.Runtime;
using AbilityKit.Demo.Shooter.View;
using AbilityKit.Protocol.Room;
using AbilityKit.Protocol.Shooter;
using Xunit;

namespace AbilityKit.Demo.Shooter.Runtime.Tests;

public sealed class ShooterClientBattleHandleTests
{
    [Fact]
    public async Task ClientBattleHandleSubmitsGatewayInputWithCurrentFrameContext()
    {
        var runtime = new ShooterBattleRuntimePort();
        var presentation = new ShooterPresentationFacade();
        var transport = new RecordingShooterRoomGatewayTransport(
            new WireSubmitBattleInputRes
            {
                Success = true,
                AcceptedFrame = 2,
                Message = "accepted"
            });
        var gateway = new ShooterRoomGatewayClient(transport);
        var session = new ShooterClientSession(runtime, presentation, tickRate: 30, decoder: null, gateway);
        var start = new ShooterStartGamePayload(
            "battle-handle-session",
            30,
            4902,
            new[]
            {
                new ShooterStartPlayer(11, "P11", 0f, 0f),
                new ShooterStartPlayer(12, "P12", 5f, 0f)
            });
        Assert.True(session.StartGame(in start));
        Assert.Equal(1, session.Tick(1f / 30f).Ticks);
        var anchor = new ShooterGatewayWorldStartAnchor(123456L, 10000000L, 0, 1d / 30d);
        var flow = new ShooterRoomGatewayFlowResult(
            "session-token",
            "room-9",
            1009ul,
            "battle-9",
            9009ul,
            11u,
            in anchor,
            canStart: true,
            started: true,
            subscribed: true,
            "ready");
        var handle = new ShooterClientBattleHandle(session, flow);

        var context = handle.CreateCurrentFrameInputContext();
        var result = await handle.SubmitLocalInputToGatewayAsync(moveX: 1f, moveY: 0f, aimX: 0f, aimY: 1f, fire: true);

        Assert.Equal(session, handle.Session);
        Assert.Equal("room-9", handle.RoomId);
        Assert.Equal("battle-9", handle.BattleId);
        Assert.Equal(9009ul, handle.WorldId);
        Assert.Equal(11u, handle.PlayerId);
        Assert.Equal(session.CurrentFrame, handle.CurrentFrame);
        Assert.Equal(session.CurrentFrame, context.Frame);
        Assert.Equal("session-token", context.SessionToken);
        Assert.Equal("battle-9", context.BattleId);
        Assert.Equal(9009ul, context.WorldId);
        Assert.Equal(11u, context.PlayerId);
        Assert.Equal(context.Frame, result.Local.RequestedFrame);
        Assert.True(result.Remote.Success);
        Assert.Equal(2, result.Remote.AcceptedFrame);
        Assert.Equal(RoomGatewayOpCodes.SubmitBattleInput, transport.LastOpCode);

        var wire = WireRoomGatewayBinary.Deserialize<WireSubmitBattleInputReq>(transport.LastPayload);
        Assert.Equal("session-token", wire.SessionToken);
        Assert.Equal("battle-9", wire.BattleId);
        Assert.Equal(9009ul, wire.WorldId);
        Assert.Equal(session.CurrentFrame, wire.Frame);
        Assert.Equal(11u, wire.PlayerId);
        Assert.Equal(ShooterOpCodes.Input.PlayerCommand, wire.InputOpCode);
        Assert.NotNull(wire.Payload);
        Assert.Equal(result.Local.Packet.Payload, wire.Payload!);
        var commands = ShooterInputCodec.Deserialize(wire.Payload!);
        Assert.Single(commands);
        Assert.Equal(11, commands[0].PlayerId);
        Assert.True(commands[0].Fire);
    }
}
