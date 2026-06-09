using System.Threading.Tasks;
using AbilityKit.Demo.Shooter.Runtime;
using AbilityKit.Demo.Shooter.View;
using AbilityKit.Protocol.Room;
using AbilityKit.Protocol.Shooter;
using Xunit;

namespace AbilityKit.Demo.Shooter.Runtime.Tests;

public sealed class ShooterClientNetworkLauncherTests
{
    [Fact]
    public async Task ClientNetworkLauncherOpensConnectionLaunchesRoomAndDispatchesPushes()
    {
        var runtime = new ShooterBattleRuntimePort();
        var presentation = new ShooterPresentationFacade();
        var connection = new FakeGatewayConnection { AutoRespondRoomGateway = true };
        connection.Close();
        using var launcher = new ShooterClientNetworkLauncher(connection);
        var start = new ShooterStartGamePayload(
            "network-launch-session",
            30,
            4904,
            new[]
            {
                new ShooterStartPlayer(51, "P51", 0f, 0f),
                new ShooterStartPlayer(52, "P52", 5f, 0f)
            });

        var launched = await launcher.CreateReadyStartAndSubscribeAsync(
            "127.0.0.1",
            17001,
            runtime,
            presentation,
            start,
            "session-token",
            ShooterRoomLaunchSpec.CreateDefault("client-network"),
            playerId: 51u);

        Assert.True(launcher.IsConnected);
        Assert.True(connection.IsConnected);
        Assert.Equal("127.0.0.1", connection.OpenHost);
        Assert.Equal(17001, connection.OpenPort);
        Assert.True(launched.Session.IsStarted);
        Assert.True(launched.Session.HasGateway);
        Assert.Equal(launched.Session, launched.Battle.Session);
        Assert.Equal("battle-launch", launched.Battle.BattleId);
        Assert.Equal(launcher.GatewayConnection, launched.GatewayConnection);
        Assert.Equal(5, connection.SentOpCodes.Count);
        Assert.Equal(RoomGatewayOpCodes.CreateRoom, connection.SentOpCodes[0]);
        Assert.Equal(RoomGatewayOpCodes.SubscribeStateSync, connection.SentOpCodes[4]);

        launcher.Tick(1f / 30f);
        var submit = await launched.Battle.SubmitLocalInputToGatewayAsync(moveX: 1f, moveY: 0f, aimX: 1f, aimY: 0f, fire: true);

        Assert.Equal(1, connection.TickCount);
        Assert.True(submit.Remote.Success);
        Assert.Equal(RoomGatewayOpCodes.SubmitBattleInput, connection.SentOpCodes[5]);
        var inputWire = WireRoomGatewayBinary.Deserialize<WireSubmitBattleInputReq>(connection.LastSentPayload);
        Assert.Equal("battle-launch", inputWire.BattleId);
        Assert.Equal(9041ul, inputWire.WorldId);
        Assert.Equal(51u, inputWire.PlayerId);

        var authority = new ShooterBattleRuntimePort();
        Assert.True(authority.StartGame(in start));
        authority.SubmitInput(0, new[] { new ShooterPlayerCommand(51, 0f, 1f, 1f, 0f, true) });
        Assert.True(authority.Tick(1f / 30f));
        var packed = authority.ExportPackedSnapshot(9041ul, isFullSnapshot: true, authorityOverride: true);
        var push = new WireStateSyncSnapshotPush
        {
            WorldId = packed.WorldId,
            Frame = packed.Frame,
            Timestamp = 4904.5,
            IsFullSnapshot = true,
            Actors = null,
            PayloadOpCode = ShooterOpCodes.Snapshot.PackedState,
            Payload = ShooterPackedSnapshotCodec.Serialize(in packed)
        };

        connection.Push(RoomGatewayOpCodes.SnapshotPushed, WireRoomGatewayBinary.Serialize(in push));

        Assert.Equal(ShooterSnapshotApplyResult.AppliedPackedSnapshot, launcher.GatewayConnection.LastPushResult);
        Assert.Equal(authority.CurrentFrame, launched.Session.CurrentFrame);
        Assert.Equal(authority.ComputeStateHash(), runtime.ComputeStateHash());
        Assert.Equal(authority.CurrentFrame, presentation.ViewModel.Frame);
    }

    [Fact]
    public async Task ClientNetworkLauncherCanBeCreatedFromConnectionFactoryAndEndpoint()
    {
        var connection = new FakeGatewayConnection { AutoRespondRoomGateway = true };
        connection.Close();
        var factory = new ShooterClientConnectionFactory(() => connection);
        using var launcher = ShooterClientNetworkLauncher.Create(factory);
        var endpoint = ShooterClientNetworkEndpoint.Localhost(17002);
        var runtime = new ShooterBattleRuntimePort();
        var presentation = new ShooterPresentationFacade();
        var start = new ShooterStartGamePayload(
            "network-factory-session",
            30,
            4905,
            new[]
            {
                new ShooterStartPlayer(61, "P61", 0f, 0f),
                new ShooterStartPlayer(62, "P62", 5f, 0f)
            });

        var launched = await launcher.CreateReadyStartAndSubscribeAsync(
            endpoint,
            runtime,
            presentation,
            start,
            "session-token",
            ShooterRoomLaunchSpec.CreateDefault("client-factory"),
            playerId: 61u);

        Assert.Equal(connection, launcher.Connection);
        Assert.Equal(connection, launched.Connection);
        Assert.Equal("127.0.0.1", connection.OpenHost);
        Assert.Equal(17002, connection.OpenPort);
        Assert.True(launched.Session.IsStarted);
        Assert.Equal("battle-launch", launched.Battle.BattleId);
        Assert.Equal(9041ul, launched.Battle.WorldId);
        Assert.Equal(61u, launched.Battle.PlayerId);
    }
}
