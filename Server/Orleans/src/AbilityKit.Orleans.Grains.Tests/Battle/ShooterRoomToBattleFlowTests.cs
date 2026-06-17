using AbilityKit.Demo.Shooter;
using AbilityKit.Orleans.Contracts.Battle;
using AbilityKit.Orleans.Contracts.Rooms;
using AbilityKit.Orleans.Grains.Battle;
using AbilityKit.Orleans.Grains.Battle.Gameplay;
using AbilityKit.Orleans.Grains.Rooms.Gameplay;
using AbilityKit.Protocol.Shooter;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace AbilityKit.Orleans.Grains.Tests.Battle;

public sealed class ShooterRoomToBattleFlowTests
{
    [Fact]
    public void ReadyShooterRoom_WhenResolvedThroughBattleRegistry_StartsRuntimeAndPublishesPackedSnapshot()
    {
        var roomAdapter = new ShooterRoomGameplayAdapter();
        var roomSummary = CreateRoomSummary();
        var roomState = roomAdapter.CreateState(roomSummary);
        roomAdapter.Join(roomState, roomSummary, new HashSet<string>(), "player-a");
        roomAdapter.Join(roomState, roomSummary, new HashSet<string> { "player-a" }, "player-b");
        roomAdapter.SetReady(roomState, new RoomReadyRequest("player-a", true));
        roomAdapter.SetReady(roomState, new RoomReadyRequest("player-b", true));

        var initParams = roomAdapter.BuildBattleInitParams(
            roomState,
            roomSummary,
            new StartRoomBattleRequest(
                "player-a",
                GameplayId: 0,
                RuleSetId: 11,
                ConfigVersion: 12,
                ProtocolVersion: 13,
                WorldType: null,
                ClientId: "client-a"));

        using var worldManager = new ServerMobaWorldManager(NullLogger.Instance);
        var shooterRuntimeAdapter = new ShooterBattleRuntimeAdapter(worldManager);
        var registry = new BattleRuntimeRegistry(
            new IBattleRuntimeAdapter[]
            {
                shooterRuntimeAdapter
            },
            defaultRoomType: ShooterGameplay.RoomType);
        var battleAdapter = registry.Resolve(initParams.RoomType);
        using var session = battleAdapter.CreateSession("shooter-room-to-battle-flow-test");

        var start = session.Start(initParams);

        Assert.True(roomAdapter.CanStart(roomState));
        Assert.IsType<ShooterBattleRuntimeAdapter>(battleAdapter);
        Assert.True(start.Succeeded, start.Error);
        Assert.Equal(ShooterGameplay.RoomType, initParams.RoomType);
        Assert.Equal(ShooterGameplay.WorldType, initParams.WorldType);
        Assert.Equal(ShooterGameplay.GameplayId, initParams.GameplayId);
        Assert.Equal(30, initParams.TickRate);
        Assert.Equal(77, initParams.MapId);
        Assert.Equal(2468, initParams.RandomSeed);
        Assert.Equal(11, initParams.RuleSetId);
        Assert.Equal(12, initParams.ConfigVersion);
        Assert.Equal(13, initParams.ProtocolVersion);
        Assert.Equal(4, initParams.InputDelayFrames);
        Assert.NotNull(initParams.SyncOptions);
        Assert.Equal("runtime-snapshot-interpolation", initParams.SyncOptions!.SyncTemplateId);
        Assert.Equal(3, initParams.SyncOptions.SyncModel);
        Assert.Equal("wan-lossy", initParams.SyncOptions.NetworkEnvironmentId);
        Assert.Equal("OrleansGateway", initParams.SyncOptions.CarrierName);
        Assert.False(initParams.SyncOptions.EnableAuthoritativeWorld);
        Assert.True(initParams.SyncOptions.InterpolationEnabled);
        Assert.Equal(4, initParams.SyncOptions.InputDelayFrames);

        var initialSnapshot = session.GetSnapshot(0);
        Assert.NotNull(initialSnapshot);
        Assert.Equal(0, initialSnapshot!.Frame);
        Assert.Collection(
            initialSnapshot.Actors,
            first =>
            {
                Assert.Equal(1, first.ActorId);
                Assert.Equal(0f, first.X);
            },
            second =>
            {
                Assert.Equal(2, second.ActorId);
                Assert.Equal(2f, second.X);
            });

        var accepted = session.SubmitInputs(
            0,
            new[]
            {
                new BattleInputItem
                {
                    PlayerId = 1,
                    OpCode = ShooterOpCodes.Input.PlayerCommand,
                    Payload = ShooterInputCodec.Serialize(new[]
                    {
                        new ShooterPlayerCommand(1, 1f, 0f, 1f, 0f, true)
                    })
                }
            });

        Assert.Equal(1, accepted);
        Assert.True(session.Tick(frame: 1, tickRate: initParams.TickRate, deltaTime: 1f / initParams.TickRate));

        var snapshot = session.GetSnapshot(1);
        Assert.NotNull(snapshot);
        Assert.Equal(1, snapshot!.Frame);
        Assert.True(snapshot.Actors[0].X > 0f);

        var push = session.CreateStateSyncPush(initParams.WorldId, frame: 1, isFullSnapshot: true);
        Assert.Equal(initParams.WorldId, push.WorldId);
        Assert.Equal(1, push.Frame);
        Assert.True(push.IsFullSnapshot);
        Assert.Equal(ShooterOpCodes.Snapshot.PackedState, push.PayloadOpCode);
        Assert.NotNull(push.Payload);
        Assert.NotEmpty(push.Payload!);

        var packed = ShooterPackedSnapshotCodec.Deserialize(push.Payload!);
        Assert.Equal(initParams.WorldId, packed.WorldId);
        Assert.Equal(push.Frame, packed.Frame);
        Assert.Equal(3, packed.EntityCount);
        Assert.NotEqual(0u, packed.StateHash);
    }

    private static RoomSummary CreateRoomSummary()
    {
        return new RoomSummary(
            Region: "local",
            ServerId: "server-a",
            RoomId: "shooter-room-to-battle-flow",
            RoomType: ShooterGameplay.RoomType,
            Title: "Shooter Room To Battle Flow",
            IsPublic: true,
            MaxPlayers: 2,
            PlayerCount: 0,
            OwnerAccountId: "player-a",
            CreatedAtUnixMs: 0,
            Tags: new Dictionary<string, string>
            {
                ["tickRate"] = "30",
                ["mapId"] = "77",
                ["randomSeed"] = "2468",
                ["syncTemplateId"] = "runtime-snapshot-interpolation",
                ["syncModel"] = "3",
                ["networkEnvironmentId"] = "wan-lossy",
                ["carrierName"] = "OrleansGateway",
                ["enableAuthoritativeWorld"] = "false",
                ["interpolationEnabled"] = "true",
                ["inputDelayFrames"] = "4"
            });
    }
}
