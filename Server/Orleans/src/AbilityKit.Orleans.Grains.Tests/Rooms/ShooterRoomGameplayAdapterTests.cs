using System.Collections.Generic;
using AbilityKit.Demo.Shooter;
using AbilityKit.Orleans.Contracts.Battle;
using AbilityKit.Orleans.Contracts.Rooms;
using AbilityKit.Orleans.Grains.Rooms.Gameplay;
using Xunit;

namespace AbilityKit.Orleans.Grains.Tests.Rooms;

public sealed class ShooterRoomGameplayAdapterTests
{
    [Fact]
    public void RegistryResolve_WhenRoomTypeIsShooter_ReturnsShooterAdapter()
    {
        var registry = new RoomGameplayRegistry();

        var adapter = registry.Resolve(ShooterGameplay.RoomType);

        Assert.IsType<ShooterRoomGameplayAdapter>(adapter);
    }

    [Fact]
    public void BuildBattleInitParams_WhenShooterRoomReady_MapsRoomToBattleInit()
    {
        var adapter = new ShooterRoomGameplayAdapter();
        var summary = CreateSummary(new Dictionary<string, string>
        {
            ["tickRate"] = "20",
            ["mapId"] = "3",
            ["randomSeed"] = "1234"
        });
        var state = adapter.CreateState(summary);
        adapter.Join(state, summary, new HashSet<string>(), "player-a");
        adapter.Join(state, summary, new HashSet<string> { "player-a" }, "player-b");
        adapter.SetReady(state, new RoomReadyRequest("player-a", true));
        adapter.SetReady(state, new RoomReadyRequest("player-b", true));

        var initParams = adapter.BuildBattleInitParams(
            state,
            summary,
            new StartRoomBattleRequest(
                "player-a",
                GameplayId: 0,
                RuleSetId: 7,
                ConfigVersion: 8,
                ProtocolVersion: 9,
                WorldType: null,
                ClientId: "client-a",
                SyncOptions: new BattleSyncStartOptions(
                    SyncTemplateId: "predict-rollback-authority",
                    SyncModel: 2,
                    NetworkEnvironmentId: "ideal",
                    CarrierName: "DemoHarness",
                    EnableAuthoritativeWorld: true,
                    InterpolationEnabled: false,
                    InputDelayFrames: 3)));

        Assert.True(adapter.CanStart(state));
        Assert.Equal(ShooterGameplay.RoomType, initParams.RoomType);
        Assert.Equal(ShooterGameplay.WorldType, initParams.WorldType);
        Assert.Equal(ShooterGameplay.GameplayId, initParams.GameplayId);
        Assert.Equal(20, initParams.TickRate);
        Assert.Equal(3, initParams.MapId);
        Assert.Equal(1234, initParams.RandomSeed);
        Assert.Equal(7, initParams.RuleSetId);
        Assert.Equal(8, initParams.ConfigVersion);
        Assert.Equal(9, initParams.ProtocolVersion);
        Assert.Equal("client-a", initParams.ClientId);
        Assert.Equal(3, initParams.InputDelayFrames);
        Assert.NotNull(initParams.SyncOptions);
        Assert.Equal("predict-rollback-authority", initParams.SyncOptions!.SyncTemplateId);
        Assert.Equal(2, initParams.SyncOptions.SyncModel);
        Assert.Equal("ideal", initParams.SyncOptions.NetworkEnvironmentId);
        Assert.Equal("DemoHarness", initParams.SyncOptions.CarrierName);
        Assert.True(initParams.SyncOptions.EnableAuthoritativeWorld);
        Assert.False(initParams.SyncOptions.InterpolationEnabled);
        Assert.Equal(3, initParams.SyncOptions.InputDelayFrames);
        Assert.NotEqual(0UL, initParams.WorldId);
        Assert.Collection(
            initParams.Players!,
            first =>
            {
                Assert.Equal(1U, first.PlayerId);
                Assert.Equal(0f, first.PosX);
            },
            second =>
            {
                Assert.Equal(2U, second.PlayerId);
                Assert.Equal(2f, second.PosX);
            });
    }

    private static RoomSummary CreateSummary(Dictionary<string, string>? tags = null)
    {
        return new RoomSummary(
            Region: "local",
            ServerId: "server-a",
            RoomId: "shooter-room-1",
            RoomType: ShooterGameplay.RoomType,
            Title: "Shooter Room",
            IsPublic: true,
            MaxPlayers: 2,
            PlayerCount: 0,
            OwnerAccountId: "player-a",
            CreatedAtUnixMs: 0,
            Tags: tags);
    }
}
