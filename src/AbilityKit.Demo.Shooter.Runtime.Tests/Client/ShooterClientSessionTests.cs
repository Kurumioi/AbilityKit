using System.Threading.Tasks;
using AbilityKit.Demo.Shooter.Runtime;
using AbilityKit.Demo.Shooter.View;
using AbilityKit.Network.Runtime;
using AbilityKit.Network.Runtime.Sync;
using AbilityKit.Protocol.Room;
using AbilityKit.Protocol.Shooter;
using Xunit;

namespace AbilityKit.Demo.Shooter.Runtime.Tests;

[Collection(ShooterClientSyncControllerFactoryTestCollection.Name)]
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
        Assert.Contains(presentation.ViewModel.Current.EntityChanges, change => change.Key.Equals(new ShooterViewEntityKey(ShooterViewEntityKind.Player, 1)));
        Assert.Contains(presentation.ViewModel.Current.EntityChanges, change => change.Key.Equals(new ShooterViewEntityKey(ShooterViewEntityKind.Player, 2)));
        Assert.Equal(2, snapshotPublishCount);
    }

    [Fact]
    public void ClientSessionReportsPredictRollbackSyncModel()
    {
        var runtime = new ShooterBattleRuntimePort();
        var presentation = new ShooterPresentationFacade();
        var session = new ShooterClientSession(runtime, presentation, tickRate: 30);

        Assert.Equal(NetworkSyncModel.PredictRollback, session.SyncModel);
    }

    [Fact]
    public void ClientSessionDelegatesToPredictRollbackSyncController()
    {
        var runtime = new ShooterBattleRuntimePort();
        var presentation = new ShooterPresentationFacade();
        var session = new ShooterClientSession(
            runtime,
            ShooterPresentationSessionContext.CreateFromFacade(presentation),
            tickRate: 30,
            decoder: null,
            gateway: null,
            syncModel: NetworkSyncModel.PredictRollback);

        Assert.IsType<ShooterClientPredictRollbackSyncController>(session.SyncController);
        Assert.Equal(NetworkSyncModel.PredictRollback, session.SyncController.SyncModel);
        Assert.Same(session.SyncController.FrameSync, session.FrameSync);
        Assert.Same(session.SyncController.InputCoordinator, session.InputCoordinator);
    }

    [Fact]
    public void ClientSessionRejectsUnimplementedSyncModel()
    {
        var runtime = new ShooterBattleRuntimePort();
        var presentation = new ShooterPresentationFacade();

        Assert.Throws<System.NotSupportedException>(() => new ShooterClientSession(
            runtime,
            ShooterPresentationSessionContext.CreateFromFacade(presentation),
            tickRate: 30,
            decoder: null,
            gateway: null,
            syncModel: NetworkSyncModel.Lockstep));
    }

    [Fact]
    public void ClientSessionCreatesAuthoritativeInterpolationSyncController()
    {
        var runtime = new ShooterBattleRuntimePort();
        var presentation = new ShooterPresentationFacade();
        var session = new ShooterClientSession(
            runtime,
            ShooterPresentationSessionContext.CreateFromFacade(presentation),
            tickRate: 30,
            decoder: null,
            gateway: null,
            syncModel: NetworkSyncModel.AuthoritativeInterpolation);

        Assert.IsType<ShooterClientAuthoritativeInterpolationSyncController>(session.SyncController);
        Assert.Equal(NetworkSyncModel.AuthoritativeInterpolation, session.SyncModel);
    }

    [Fact]
    public void ClientSessionPassesInterpolationConfigToAuthoritativeInterpolationController()
    {
        var runtime = new ShooterBattleRuntimePort();
        var presentation = new ShooterPresentationFacade();
        // Custom millisecond timeline + snap mode so the buffered observation seeds the estimate
        // directly, proving the config reached the controller rather than the 1000-tick default.
        var config = new InterpolationConfig(
            ticksPerSecond: 1000L, interpolationDelayTicks: 250L, bufferCapacity: 8, catchUpRate: 0d);
        var session = new ShooterClientSession(
            runtime,
            ShooterPresentationSessionContext.CreateFromFacade(presentation),
            tickRate: 30,
            decoder: null,
            gateway: null,
            syncModel: NetworkSyncModel.AuthoritativeInterpolation,
            interpolationConfig: config);

        var controller = Assert.IsType<ShooterClientAuthoritativeInterpolationSyncController>(session.SyncController);
        controller.BufferRemoteSnapshot(new ShooterGatewaySnapshot(
            worldId: 9001ul, frame: 1, timestamp: 0d, serverTicks: 5000L, isFullSnapshot: true,
            actors: new[] { new ShooterGatewayActorSnapshot(actorId: 7, x: 0f, y: 0f, rotation: 0f, velocityX: 0f, velocityY: 0f, hp: 100f, hpMax: 100f, teamId: 1) }));

        // Snap mode estimate == newest observed server ticks; default config would not have been
        // observed yet but the seeding value confirms the millisecond timescale config was applied.
        Assert.Equal(5000L, controller.EstimatedServerTicks);
    }

    [Fact]
    public void ClientSessionExposesInterpolationDiagnosticsForInterpolationModel()
    {
        var runtime = new ShooterBattleRuntimePort();
        var presentation = new ShooterPresentationFacade();
        var config = new InterpolationConfig(
            ticksPerSecond: 1000L, interpolationDelayTicks: 100L, bufferCapacity: 8, catchUpRate: 0d);
        var session = new ShooterClientSession(
            runtime,
            ShooterPresentationSessionContext.CreateFromFacade(presentation),
            tickRate: 30,
            decoder: null,
            gateway: null,
            syncModel: NetworkSyncModel.AuthoritativeInterpolation,
            interpolationConfig: config);

        var controller = (ShooterClientAuthoritativeInterpolationSyncController)session.SyncController;
        controller.BufferRemoteSnapshot(new ShooterGatewaySnapshot(
            worldId: 9001ul, frame: 1, timestamp: 0d, serverTicks: 1000L, isFullSnapshot: true,
            actors: new[] { new ShooterGatewayActorSnapshot(actorId: 7, x: 0f, y: 0f, rotation: 0f, velocityX: 0f, velocityY: 0f, hp: 100f, hpMax: 100f, teamId: 1) }));
        controller.BufferRemoteSnapshot(new ShooterGatewaySnapshot(
            worldId: 9001ul, frame: 2, timestamp: 0d, serverTicks: 1100L, isFullSnapshot: true,
            actors: new[] { new ShooterGatewayActorSnapshot(actorId: 7, x: 10f, y: 0f, rotation: 0f, velocityX: 0f, velocityY: 0f, hp: 100f, hpMax: 100f, teamId: 1) }));
        session.Tick(0f);

        Assert.True(session.TryGetInterpolationDiagnostics(out var diagnostics));
        Assert.Equal(2, diagnostics.BufferedRemoteSnapshotCount);
        Assert.Equal(1100L, diagnostics.EstimatedServerTicks);
        Assert.Equal(1000L, diagnostics.RemotePlaybackTicks);
        Assert.Equal(100L, diagnostics.PlaybackDelayTicks);
        Assert.True(diagnostics.HasPublishedRemoteFrame);
        Assert.False(diagnostics.IsRemotePlaybackStarved);
    }

    [Fact]
    public void ClientSessionReportsNoInterpolationDiagnosticsForPredictRollbackModel()
    {
        var runtime = new ShooterBattleRuntimePort();
        var presentation = new ShooterPresentationFacade();
        var session = new ShooterClientSession(runtime, presentation, tickRate: 30);

        Assert.False(session.TryGetInterpolationDiagnostics(out var diagnostics));
        Assert.Equal(default, diagnostics);
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
                Message = "accepted",
                CurrentFrame = 5,
                Status = "Accepted",
                ShouldResync = false,
                ServerTicks = 123456789L
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
        Assert.Equal(5, result.Remote.CurrentFrame);
        Assert.Equal("Accepted", result.Remote.Status);
        Assert.False(result.Remote.ShouldResync);
        Assert.Equal(123456789L, result.Remote.ServerTicks);
        Assert.Contains(session.LastFastReconnectHealthEvents, e => e.Kind == SyncHealthEventKind.InputAccepted && e.Frame == 7);
        Assert.Contains(session.LastFastReconnectHealthEvents, e => e.Kind == SyncHealthEventKind.LagCompensatedValidationAccepted && e.Frame == 7);
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

    [Fact]
    public async Task ClientSessionReceivesGatewayInputResyncHint()
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
        var session = new ShooterClientSession(runtime, presentation, tickRate: 30, decoder: null, gateway);
        var start = new ShooterStartGamePayload(
            "gateway-resync-session",
            30,
            4905,
            new[]
            {
                new ShooterStartPlayer(11, "P11", 0f, 0f)
            });
        Assert.True(session.StartGame(in start));
        var context = new ShooterGatewayBattleInputContext("session-token", "battle-1", 9009ul, frame: 30, playerId: 11u);
        var command = new ShooterPlayerCommand(11, 1f, 0f, 0f, 1f, true);

        var result = await session.SubmitLocalInputToGatewayAsync(context, command);

        Assert.False(result.Remote.Success);
        Assert.Equal(12, result.Remote.AcceptedFrame);
        Assert.Equal(8, result.Remote.CurrentFrame);
        Assert.Equal("RejectedTooFarFuture", result.Remote.Status);
        Assert.True(result.Remote.ShouldResync);
        Assert.Equal(987654321L, result.Remote.ServerTicks);
        Assert.Contains(session.LastFastReconnectHealthEvents, e => e.Kind == SyncHealthEventKind.InputRejected && e.Frame == 30);
        Assert.Contains(session.LastFastReconnectHealthEvents, e => e.Kind == SyncHealthEventKind.LagCompensatedValidationRejected && e.Frame == 30);
        Assert.True(session.NeedsFullSnapshotResync);
        Assert.Equal(ShooterClientRecoveryState.AwaitingFullSnapshot, session.RecoveryState);
        Assert.Equal(ShooterClientResyncReason.ClientHashRejectedByServer, session.LastResyncReason);
    }

    [Fact]
    public async Task ClientSessionRequestsFullSnapshotResyncWithDedicatedFullStateSyncRequest()
    {
        var runtime = new ShooterBattleRuntimePort();
        var presentation = new ShooterPresentationFacade();
        var session = new ShooterClientSession(runtime, presentation, tickRate: 30);
        var roomClient = new ScriptedShooterRoomClient();
        var request = new ShooterGatewayFullStateSyncRequest(
            "session-token",
            "battle-1",
            "room-1",
            worldId: 9001ul,
            clientFrame: 123,
            lastAuthoritativeFrame: 120,
            clientStateHash: 0xABCDEF01u,
            authoritativeStateHash: 0x12345678u,
            reason: ShooterClientResyncReason.AuthoritativeHashMismatch.ToString());

        var result = await session.RequestFullSnapshotResyncAsync(roomClient, request);

        Assert.True(result.Success);
        Assert.True(result.Accepted);
        Assert.Equal("accepted", result.Message);
        Assert.Equal(123456789L, result.ServerTicks);
        Assert.Equal(request.SessionToken, roomClient.LastFullStateSyncRequest.SessionToken);
        Assert.Equal(request.BattleId, roomClient.LastFullStateSyncRequest.BattleId);
        Assert.Equal(request.RoomId, roomClient.LastFullStateSyncRequest.RoomId);
        Assert.Equal(request.WorldId, roomClient.LastFullStateSyncRequest.WorldId);
        Assert.Equal(request.ClientFrame, roomClient.LastFullStateSyncRequest.ClientFrame);
        Assert.Equal(request.LastAuthoritativeFrame, roomClient.LastFullStateSyncRequest.LastAuthoritativeFrame);
        Assert.Equal(request.ClientStateHash, roomClient.LastFullStateSyncRequest.ClientStateHash);
        Assert.Equal(request.AuthoritativeStateHash, roomClient.LastFullStateSyncRequest.AuthoritativeStateHash);
        Assert.Equal(request.Reason, roomClient.LastFullStateSyncRequest.Reason);
        Assert.Contains("request-full-state:room-1:battle-1:AuthoritativeHashMismatch", roomClient.Calls);
    }
}
