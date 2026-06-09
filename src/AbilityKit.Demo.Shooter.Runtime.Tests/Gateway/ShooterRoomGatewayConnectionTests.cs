using System.Threading.Tasks;
using AbilityKit.Demo.Shooter.Runtime;
using AbilityKit.Demo.Shooter.View;
using AbilityKit.Network.Protocol;
using AbilityKit.Protocol.Room;
using AbilityKit.Protocol.Shooter;
using Xunit;

namespace AbilityKit.Demo.Shooter.Runtime.Tests;

public sealed class ShooterRoomGatewayConnectionTests
{
    [Fact]
    public async Task GatewayConnectionUsesTransportNeutralConnectionForRequestsAndSnapshotPushes()
    {
        var runtime = new ShooterBattleRuntimePort();
        var presentation = new ShooterPresentationFacade();
        var connection = new FakeGatewayConnection();
        using var gatewayConnection = new ShooterRoomGatewayConnection(connection);
        var gateway = new ShooterRoomGatewayClient(gatewayConnection);
        var session = new ShooterClientSession(runtime, presentation, tickRate: 30, decoder: null, gateway);
        gatewayConnection.AttachSession(session);

        var start = new ShooterStartGamePayload(
            "connection-session",
            30,
            5901,
            new[]
            {
                new ShooterStartPlayer(21, "P21", 0f, 0f),
                new ShooterStartPlayer(22, "P22", 5f, 0f)
            });
        Assert.True(session.StartGame(in start));

        var context = new ShooterGatewayBattleInputContext("session-token", "battle-2", 9010ul, frame: 2, playerId: 21u);
        var command = new ShooterPlayerCommand(21, 1f, 0f, 1f, 0f, false);
        var requestTask = session.SubmitLocalInputToGatewayAsync(context, command);
        Assert.Equal(RoomGatewayOpCodes.SubmitBattleInput, connection.LastSentOpCode);
        Assert.Equal(NetworkPacketFlags.Request, connection.LastSentFlags);
        Assert.True(connection.LastSentSeq > 0);
        var requestWire = WireRoomGatewayBinary.Deserialize<WireSubmitBattleInputReq>(connection.LastSentPayload);
        Assert.Equal("session-token", requestWire.SessionToken);
        Assert.Equal("battle-2", requestWire.BattleId);
        Assert.Equal(9010ul, requestWire.WorldId);
        Assert.Equal(2, requestWire.Frame);
        Assert.Equal(21u, requestWire.PlayerId);
        Assert.Equal(ShooterOpCodes.Input.PlayerCommand, requestWire.InputOpCode);
        Assert.NotNull(requestWire.Payload);

        connection.CompleteResponse(
            connection.LastSentOpCode,
            connection.LastSentSeq,
            new WireSubmitBattleInputRes
            {
                Success = true,
                AcceptedFrame = 3,
                Message = "ok"
            });
        var submitResult = await requestTask;
        Assert.True(submitResult.Remote.Success);
        Assert.Equal(3, submitResult.Remote.AcceptedFrame);
        Assert.Equal("ok", submitResult.Remote.Message);

        var authority = new ShooterBattleRuntimePort();
        Assert.True(authority.StartGame(in start));
        authority.SubmitInput(0, new[] { new ShooterPlayerCommand(21, 0f, 1f, 1f, 0f, true) });
        Assert.True(authority.Tick(1f / 30f));
        var packed = authority.ExportPackedSnapshot(9010ul, isFullSnapshot: true, authorityOverride: true);
        var wire = new WireStateSyncSnapshotPush
        {
            WorldId = packed.WorldId,
            Frame = packed.Frame,
            Timestamp = 9010.5,
            IsFullSnapshot = true,
            Actors = null,
            PayloadOpCode = ShooterOpCodes.Snapshot.PackedState,
            Payload = ShooterPackedSnapshotCodec.Serialize(in packed)
        };
        var pushPayload = WireRoomGatewayBinary.Serialize(in wire);
        var dispatchedCount = 0;
        var dispatchedResult = ShooterSnapshotApplyResult.Ignored;
        gatewayConnection.SnapshotPushDispatched += (_, _, result) =>
        {
            dispatchedCount++;
            dispatchedResult = result;
        };

        connection.Push(RoomGatewayOpCodes.SnapshotPushed, pushPayload);

        Assert.Equal(1, dispatchedCount);
        Assert.Equal(ShooterSnapshotApplyResult.AppliedPackedSnapshot, dispatchedResult);
        Assert.Equal(ShooterSnapshotApplyResult.AppliedPackedSnapshot, gatewayConnection.LastPushResult);
        Assert.Equal(authority.CurrentFrame, session.CurrentFrame);
        Assert.Equal(authority.ComputeStateHash(), runtime.ComputeStateHash());
        Assert.Equal(authority.CurrentFrame, presentation.ViewModel.Frame);
        Assert.True(presentation.ViewModel.Players.ContainsKey(21));
        Assert.True(presentation.ViewModel.Players.ContainsKey(22));
    }
}
