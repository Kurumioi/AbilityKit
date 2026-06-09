using AbilityKit.Demo.Shooter;
using AbilityKit.Orleans.Contracts.Battle;
using AbilityKit.Orleans.Contracts.Rooms;
using AbilityKit.Orleans.Grains.Battle;
using AbilityKit.Orleans.Grains.Rooms;
using AbilityKit.Protocol.Shooter;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Orleans.Configuration;
using Orleans.Hosting;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSingleton<ServerMobaWorldManager>(sp =>
    new ServerMobaWorldManager(sp.GetRequiredService<ILogger<ServerMobaWorldManager>>()));

builder.UseOrleans(silo =>
{
    silo.UseLocalhostClustering(siloPort: 12111, gatewayPort: 31001);
    silo.Configure<ClusterOptions>(options =>
    {
        options.ClusterId = "abilitykit-shooter-smoke";
        options.ServiceId = "abilitykit-orleans-shooter-smoke";
    });
});

using var host = builder.Build();
await host.StartAsync();

try
{
    var client = host.Services.GetRequiredService<IClusterClient>();
    var result = await ShooterSmokeRunner.RunAsync(client);
    Console.WriteLine($"Shooter smoke passed. RoomId={result.RoomId}, BattleId={result.BattleId}, WorldId={result.WorldId}, Frame={result.Frame}, Actors={result.ActorCount}");
}
finally
{
    await host.StopAsync();
}

internal static class ShooterSmokeRunner
{
    public static async Task<ShooterSmokeResult> RunAsync(IClusterClient client)
    {
        if (client == null) throw new ArgumentNullException(nameof(client));

        const string region = "local";
        const string serverId = "smoke";
        const string accountId = "shooter-smoke-owner";

        var directoryKey = RoomDirectoryGrain.BuildDirectoryKey(region, serverId);
        var directory = client.GetGrain<IRoomDirectoryGrain>(directoryKey);
        var createRoom = await directory.CreateRoomAsync(new CreateRoomRequest(
            accountId,
            region,
            serverId,
            ShooterGameplay.RoomType,
            "Shooter Smoke Room",
            IsPublic: true,
            MaxPlayers: ShooterGameplay.DefaultMaxPlayers,
            Tags: new Dictionary<string, string>
            {
                ["tickRate"] = ShooterGameplay.DefaultTickRate.ToString(),
                ["mapId"] = "1",
                ["randomSeed"] = "20260608"
            }));

        var room = client.GetGrain<IRoomGrain>(createRoom.RoomId);
        await room.JoinAsync(accountId);
        await room.SetReadyAsync(new RoomReadyRequest(accountId, Ready: true));
        var start = await room.StartBattleAsync(new StartRoomBattleRequest(
            accountId,
            ShooterGameplay.GameplayId,
            RuleSetId: 1,
            ConfigVersion: 1,
            ProtocolVersion: 1,
            ShooterGameplay.WorldType,
            ClientId: "shooter-smoke"));

        if (!start.Started)
        {
            throw new InvalidOperationException("Shooter battle did not start.");
        }

        var battle = client.GetGrain<IBattleLogicHostGrain>(start.BattleId);
        await WaitForFrameAsync(battle, minimumFrame: 1);

        var inputFrame = await battle.GetCurrentFrameAsync() + 1;
        var command = new ShooterPlayerCommand(
            playerId: 1,
            moveX: 1f,
            moveY: 0f,
            aimX: 1f,
            aimY: 0f,
            fire: true);
        var submit = await battle.SubmitInputAsync(start.WorldId, inputFrame, new BattleInputItem
        {
            PlayerId = 1,
            OpCode = ShooterOpCodes.Input.PlayerCommand,
            Payload = ShooterInputCodec.Serialize(new[] { command })
        });
        if (!submit.Accepted)
        {
            throw new InvalidOperationException($"Shooter battle input was rejected. Status={submit.Status}, Message={submit.Message}");
        }

        await WaitForFrameAsync(battle, submit.AcceptedFrame + 3);
        var snapshot = await battle.GetSnapshotAsync();
        if (snapshot == null)
        {
            throw new InvalidOperationException("Shooter battle snapshot is missing.");
        }

        if (snapshot.Actors.Count == 0)
        {
            throw new InvalidOperationException("Shooter battle snapshot has no actors.");
        }

        var player = snapshot.Actors[0];
        if (Math.Abs(player.X) <= 0.0001f && Math.Abs(player.Z) <= 0.0001f)
        {
            throw new InvalidOperationException("Shooter player did not move after input was submitted.");
        }

        await battle.DestroyAsync();
        return new ShooterSmokeResult(createRoom.RoomId, start.BattleId, start.WorldId, snapshot.Frame, snapshot.Actors.Count);
    }

    private static async Task WaitForFrameAsync(IBattleLogicHostGrain battle, int minimumFrame)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        while (!timeout.IsCancellationRequested)
        {
            var frame = await battle.GetCurrentFrameAsync();
            if (frame >= minimumFrame)
            {
                return;
            }

            await Task.Delay(50, timeout.Token).WaitAsync(timeout.Token);
        }

        throw new TimeoutException($"Battle did not reach frame {minimumFrame} in time.");
    }
}

internal readonly record struct ShooterSmokeResult(string RoomId, string BattleId, ulong WorldId, int Frame, int ActorCount);
