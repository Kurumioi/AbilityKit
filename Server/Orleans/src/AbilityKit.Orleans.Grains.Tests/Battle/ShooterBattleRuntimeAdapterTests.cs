using AbilityKit.Demo.Shooter;
using AbilityKit.Orleans.Contracts.Battle;
using AbilityKit.Orleans.Grains.Battle;
using AbilityKit.Orleans.Grains.Battle.Gameplay;
using AbilityKit.Protocol.Shooter;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace AbilityKit.Orleans.Grains.Tests.Battle;

public sealed class ShooterBattleRuntimeAdapterTests
{
    [Fact]
    public void SessionStartTickAndSnapshotPush_UsesShooterRuntimeWorldBoundary()
    {
        using var worldManager = new ServerMobaWorldManager(NullLogger.Instance);
        var adapter = new ShooterBattleRuntimeAdapter(worldManager);
        using var session = adapter.CreateSession("shooter-battle-adapter-test");
        var initParams = CreateInitParams();

        var start = session.Start(initParams);

        Assert.True(start.Succeeded, start.Error);
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
                Assert.Equal(3f, second.X);
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
        Assert.True(session.Tick(frame: 1, tickRate: 30, deltaTime: 1f / 30f));

        var snapshot = session.GetSnapshot(1);
        Assert.NotNull(snapshot);
        Assert.Equal(1, snapshot!.Frame);
        Assert.Equal(2, snapshot.Actors.Count);
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

    private static BattleInitParams CreateInitParams()
    {
        return new BattleInitParams
        {
            WorldId = 707ul,
            TickRate = 30,
            RandomSeed = 1357,
            RoomType = ShooterGameplay.RoomType,
            WorldType = ShooterGameplay.WorldType,
            GameplayId = ShooterGameplay.GameplayId,
            Players = new List<PlayerInitInfo>
            {
                new PlayerInitInfo
                {
                    PlayerId = 1,
                    PosX = 0f,
                    PosZ = 0f,
                    TeamId = 1
                },
                new PlayerInitInfo
                {
                    PlayerId = 2,
                    PosX = 3f,
                    PosZ = 0f,
                    TeamId = 2
                }
            }
        };
    }
}
