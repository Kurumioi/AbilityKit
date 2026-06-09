using System.Threading.Tasks;
using AbilityKit.Demo.Shooter.Runtime;
using AbilityKit.Demo.Shooter.View;
using AbilityKit.Protocol.Room;
using AbilityKit.Protocol.Shooter;
using Xunit;

namespace AbilityKit.Demo.Shooter.Runtime.Tests;

public sealed class ShooterClientSessionTests
{
    [Fact]
    public void ClientSessionStartsRuntimeBuildsInputPacketAndTicksPresentation()
    {
        var start = new ShooterStartGamePayload(
            "client-session",
            30,
            3901,
            new[]
            {
                new ShooterStartPlayer(1, "P1", 0f, 0f),
                new ShooterStartPlayer(2, "P2", 4f, 0f)
            });
        var runtime = new ShooterBattleRuntimePort();
        var presentation = new ShooterPresentationFacade();
        var snapshotPublishCount = 0;
        presentation.Snapshots.SnapshotApplied += _ => snapshotPublishCount++;
        var session = new ShooterClientSession(runtime, presentation, tickRate: 30);

        var started = session.StartGame(in start);
        var input = session.SubmitLocalInput(1, 2f, 0f, 0f, 3f, fire: true);
        var tick = session.Tick(1f / 30f);

        Assert.True(started);
        Assert.True(session.IsStarted);
        Assert.Equal(0, input.Packet.Command.MoveY);
        Assert.Equal(1, input.Packet.Command.MoveX);
        Assert.Equal(0, input.Packet.Command.AimX);
        Assert.Equal(1, input.Packet.Command.AimY);
        Assert.True(input.Packet.Command.Fire);
        Assert.Equal(ShooterOpCodes.Input.PlayerCommand, input.Packet.OpCode);
        Assert.NotEmpty(input.Packet.Payload);
        Assert.Equal(1, input.AcceptedInputs);
        Assert.Equal(0, input.RequestedFrame);
        Assert.Equal(1, tick.Ticks);
        Assert.Equal(1, session.CurrentFrame);
        Assert.Equal(runtime.CurrentFrame, presentation.ViewModel.Frame);
        Assert.True(presentation.ViewModel.Players.ContainsKey(1));
        Assert.True(presentation.ViewModel.Players.ContainsKey(2));
        Assert.Equal(2, snapshotPublishCount);
    }

    [Fact]
    public async Task ClientSessionSubmitsLocalInputThroughGenericRoomGatewayProtocol()
    {
        var runtime = new ShooterBattleRuntimePort();
        var presentation = new ShooterPresentationFacade();
        var transport = new RecordingShooterRoomGatewayTransport(
            new WireSubmitBattleInputRes
            {
                Success = true,
                AcceptedFrame = 7,
                Message = "accepted"
            });
        var gateway = new ShooterRoomGatewayClient(transport);
        var session = new ShooterClientSession(runtime, presentation, tickRate: 30, decoder: null, gateway);
        var start = new ShooterStartGamePayload(
            "gateway-session",
            30,
            4901,
            new[]
            {
                new ShooterStartPlayer(11, "P11", 0f, 0f),
                new ShooterStartPlayer(12, "P12", 5f, 0f)
            });
        Assert.True(session.StartGame(in start));
        var context = new ShooterGatewayBattleInputContext("session-token", "battle-1", 9009ul, frame: 3, playerId: 11u);
        var command = new ShooterPlayerCommand(11, 1f, 0f, 0f, 1f, true);

        var result = await session.SubmitLocalInputToGatewayAsync(context, command);

        Assert.True(session.HasGateway);
        Assert.Equal(1, result.Local.AcceptedInputs);
        Assert.Equal(3, result.Local.RequestedFrame);
        Assert.True(result.Remote.Success);
        Assert.Equal(7, result.Remote.AcceptedFrame);
        Assert.Equal("accepted", result.Remote.Message);
        Assert.Equal(RoomGatewayOpCodes.SubmitBattleInput, transport.LastOpCode);
        Assert.True(transport.LastPayload.Count > 0);
        var wire = WireRoomGatewayBinary.Deserialize<WireSubmitBattleInputReq>(transport.LastPayload);
        Assert.Equal("session-token", wire.SessionToken);
        Assert.Equal("battle-1", wire.BattleId);
        Assert.Equal(9009ul, wire.WorldId);
        Assert.Equal(3, wire.Frame);
        Assert.Equal(11u, wire.PlayerId);
        Assert.Equal(ShooterOpCodes.Input.PlayerCommand, wire.InputOpCode);
        Assert.NotNull(wire.Payload);
        Assert.Equal(result.Local.Packet.Payload, wire.Payload!);

        var commands = ShooterInputCodec.Deserialize(wire.Payload!);
        Assert.Single(commands);
        Assert.Equal(command.PlayerId, commands[0].PlayerId);
        Assert.True(commands[0].Fire);
    }
}
