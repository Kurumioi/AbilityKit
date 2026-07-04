using System;
using System.Linq;
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
            223456L,
            ShooterRoomGatewayEntryKind.TeamLobby,
            canStart: true,
            started: true,
            subscribed: true,
            "ready");
        var handle = new ShooterClientBattleHandle(session, flow);

        var context = handle.CreateCurrentFrameInputContext();
        var result = await handle.SubmitLocalInputToGatewayAsync(moveX: 1f, moveY: 0f, aimX: 0f, aimY: 1f, fire: true, attackSlot: ShooterPlayerAttackSlots.Spread);

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
        Assert.Equal(0, flow.TargetFrame);
        Assert.Equal(0, flow.CatchUpFrames);
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
        Assert.Equal(ShooterPlayerAttackSlots.Spread, commands[0].AttackSlot);
    }
    [Fact]
    public async Task ClientBattleHandleAutomaticallyRequestsFullStateSyncWhenInputResponseRequiresResync()
    {
        var runtime = new ShooterBattleRuntimePort();
        var presentation = new ShooterPresentationFacade();
        var transport = new RecordingShooterRoomGatewayTransport(
            new WireSubmitBattleInputRes
            {
                Success = false,
                AcceptedFrame = 12,
                Message = "Input frame is too far ahead.",
                CurrentFrame = 8,
                Status = "RejectedTooFarFuture",
                ShouldResync = true,
                ServerTicks = 987654321L
            });
        var gateway = new ShooterRoomGatewayClient(transport);
        var roomClient = new ScriptedShooterRoomClient();
        var session = new ShooterClientSession(runtime, presentation, tickRate: 30, decoder: null, gateway);
        var start = new ShooterStartGamePayload(
            "battle-handle-resync-session",
            30,
            4906,
            new[]
            {
                new ShooterStartPlayer(11, "P11", 0f, 0f)
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
            223456L,
            ShooterRoomGatewayEntryKind.TeamLobby,
            canStart: true,
            started: true,
            subscribed: true,
            "ready");
        var handle = new ShooterClientBattleHandle(session, flow, roomClient);

        var result = await handle.SubmitLocalInputToGatewayAsync(moveX: 1f, moveY: 0f, aimX: 0f, aimY: 1f, fire: true);

        Assert.False(result.Remote.Success);
        Assert.True(result.Remote.ShouldResync);
        Assert.True(session.NeedsFullSnapshotResync);
        Assert.Equal(ShooterClientResyncReason.ClientHashRejectedByServer, session.LastResyncReason);
        Assert.Equal("session-token", roomClient.LastFullStateSyncRequest.SessionToken);
        Assert.Equal("battle-9", roomClient.LastFullStateSyncRequest.BattleId);
        Assert.Equal("room-9", roomClient.LastFullStateSyncRequest.RoomId);
        Assert.Equal(9009ul, roomClient.LastFullStateSyncRequest.WorldId);
        Assert.Equal(1, roomClient.LastFullStateSyncRequest.ClientFrame);
        Assert.Equal(8, roomClient.LastFullStateSyncRequest.LastAuthoritativeFrame);
        Assert.Equal(ShooterClientResyncReason.ClientHashRejectedByServer.ToString(), roomClient.LastFullStateSyncRequest.Reason);
        Assert.Contains("request-full-state:room-9:battle-9:ClientHashRejectedByServer", roomClient.Calls);
    }

    [Fact]
    public async Task ClientBattleHandleRequestsFullStateSyncWhenPureStateDeltaNeedsBaseline()
    {
        var source = new ShooterBattleRuntimePort();
        var sourceStart = new ShooterStartGamePayload(
            "battle-handle-pure-state-source",
            30,
            4908,
            new[]
            {
                new ShooterStartPlayer(11, "P11", 0f, 0f),
                new ShooterStartPlayer(12, "P12", 5f, 0f)
            });
        Assert.True(source.StartGame(in sourceStart));
        Assert.True(source.Tick(1f / 30f));
        var delta = source.ExportPureStateSnapshot(9009ul, isFullBaseline: false, baselineFrame: 99, baselineHash: 123u);
        var presentation = new ShooterPresentationFacade();
        var session = new ShooterClientSession(new ShooterBattleRuntimePort(), presentation, tickRate: 30);
        Assert.True(session.StartGame(in sourceStart));
        var roomClient = new ScriptedShooterRoomClient();
        var anchor = new ShooterGatewayWorldStartAnchor(123456L, 10000000L, 0, 1d / 30d);
        var flow = new ShooterRoomGatewayFlowResult(
            "session-token",
            "room-9",
            1009ul,
            "battle-9",
            9009ul,
            11u,
            in anchor,
            223456L,
            ShooterRoomGatewayEntryKind.TeamLobby,
            canStart: true,
            started: true,
            subscribed: true,
            "ready");
        var handle = new ShooterClientBattleHandle(session, flow, roomClient);

        var applyResult = session.ApplyGatewayPush(
            RoomGatewayOpCodes.DeltaSnapshotPushed,
            CreatePureStateGatewayPayload(in delta, ShooterOpCodes.Snapshot.PureStateDelta, isFullSnapshot: false));
        var requestResult = await handle.RequestFullSnapshotResyncIfNeededAsync();

        Assert.Equal(ShooterSnapshotApplyResult.PureStateBaselineResyncNeeded, applyResult);
        Assert.False(session.NeedsFullSnapshotResync);
        Assert.True(presentation.NeedsPureStateFullBaselineResync);
        Assert.True(requestResult.Accepted);
        Assert.Equal("session-token", roomClient.LastFullStateSyncRequest.SessionToken);
        Assert.Equal("battle-9", roomClient.LastFullStateSyncRequest.BattleId);
        Assert.Equal("room-9", roomClient.LastFullStateSyncRequest.RoomId);
        Assert.Equal(9009ul, roomClient.LastFullStateSyncRequest.WorldId);
        Assert.Equal(0, roomClient.LastFullStateSyncRequest.ClientFrame);
        Assert.Equal(delta.Frame, roomClient.LastFullStateSyncRequest.LastAuthoritativeFrame);
        Assert.Equal(0u, roomClient.LastFullStateSyncRequest.ClientStateHash);
        Assert.Equal(delta.StateHash, roomClient.LastFullStateSyncRequest.AuthoritativeStateHash);
        Assert.Equal("PureStateMissingBaseline", roomClient.LastFullStateSyncRequest.Reason);
        Assert.Contains("request-full-state:room-9:battle-9:PureStateMissingBaseline", roomClient.Calls);
    }

    [Fact]
    public async Task ClientBattleHandleApplyGatewayPushAndRequestsFullStateSyncWhenPureStateDeltaNeedsBaseline()
    {
        var source = new ShooterBattleRuntimePort();
        var sourceStart = new ShooterStartGamePayload(
            "battle-handle-pure-state-source-apply",
            30,
            4909,
            new[]
            {
                new ShooterStartPlayer(11, "P11", 0f, 0f),
                new ShooterStartPlayer(12, "P12", 5f, 0f)
            });
        Assert.True(source.StartGame(in sourceStart));
        Assert.True(source.Tick(1f / 30f));
        var delta = source.ExportPureStateSnapshot(9010ul, isFullBaseline: false, baselineFrame: 99, baselineHash: 123u);
        var presentation = new ShooterPresentationFacade();
        var session = new ShooterClientSession(new ShooterBattleRuntimePort(), presentation, tickRate: 30);
        Assert.True(session.StartGame(in sourceStart));
        var roomClient = new ScriptedShooterRoomClient();
        var anchor = new ShooterGatewayWorldStartAnchor(123456L, 10000000L, 0, 1d / 30d);
        var flow = new ShooterRoomGatewayFlowResult(
            "session-token",
            "room-9",
            1009ul,
            "battle-9",
            9010ul,
            11u,
            in anchor,
            223456L,
            ShooterRoomGatewayEntryKind.TeamLobby,
            canStart: true,
            started: true,
            subscribed: true,
            "ready");
        var handle = new ShooterClientBattleHandle(session, flow, roomClient);

        var result = await handle.ApplyGatewayPushAndRequestFullSnapshotResyncIfNeededAsync(
            RoomGatewayOpCodes.DeltaSnapshotPushed,
            CreatePureStateGatewayPayload(in delta, ShooterOpCodes.Snapshot.PureStateDelta, isFullSnapshot: false));

        Assert.Equal(ShooterSnapshotApplyResult.PureStateBaselineResyncNeeded, result);
        Assert.True(presentation.NeedsPureStateFullBaselineResync);
        Assert.Equal("PureStateMissingBaseline", roomClient.LastFullStateSyncRequest.Reason);
        Assert.Equal(1, roomClient.Calls.Count(call => call.StartsWith("request-full-state:")));
    }

    [Fact]
    public async Task ClientBattleHandleDoesNotRepeatSameFullStateSyncRequestWhileWaitingForSnapshot()
    {
        var runtime = new ShooterBattleRuntimePort();
        var presentation = new ShooterPresentationFacade();
        var transport = new RecordingShooterRoomGatewayTransport(
            new WireSubmitBattleInputRes
            {
                Success = false,
                AcceptedFrame = 12,
                Message = "Input frame is too far ahead.",
                CurrentFrame = 8,
                Status = "RejectedTooFarFuture",
                ShouldResync = true,
                ServerTicks = 987654321L
            });
        var gateway = new ShooterRoomGatewayClient(transport);
        var roomClient = new ScriptedShooterRoomClient();
        var session = new ShooterClientSession(runtime, presentation, tickRate: 30, decoder: null, gateway);
        var start = new ShooterStartGamePayload(
            "battle-handle-repeat-resync-session",
            30,
            4907,
            new[]
            {
                new ShooterStartPlayer(11, "P11", 0f, 0f)
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
            223456L,
            ShooterRoomGatewayEntryKind.TeamLobby,
            canStart: true,
            started: true,
            subscribed: true,
            "ready");
        var handle = new ShooterClientBattleHandle(session, flow, roomClient);

        await handle.SubmitLocalInputToGatewayAsync(moveX: 1f, moveY: 0f, aimX: 0f, aimY: 1f, fire: true);
        var repeated = await handle.RequestFullSnapshotResyncIfNeededAsync();

        Assert.False(repeated.Success);
        Assert.False(repeated.Accepted);
        Assert.Equal("not requested", repeated.Message);
        Assert.Equal(1, roomClient.Calls.Count(call => call.StartsWith("request-full-state:")));
    }

    private static ArraySegment<byte> CreatePureStateGatewayPayload(in ShooterPureStateSnapshotPayload pureState, int payloadOpCode, bool isFullSnapshot)
    {
        var wire = new WireStateSyncSnapshotPush
        {
            WorldId = pureState.WorldId,
            Frame = pureState.Frame,
            Timestamp = 456.5,
            IsFullSnapshot = isFullSnapshot,
            Actors = null,
            PayloadOpCode = payloadOpCode,
            Payload = ShooterPureStateSyncCodec.Serialize(in pureState),
            ServerTicks = pureState.ServerTick
        };
        return WireRoomGatewayBinary.Serialize(in wire);
    }
}
