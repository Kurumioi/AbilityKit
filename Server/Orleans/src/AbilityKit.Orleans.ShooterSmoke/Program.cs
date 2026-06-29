extern alias Gateway;

using AbilityKit.Orleans.Grains.Battle;
using AbilityKit.Orleans.Grains.Persistence;
using AbilityKit.Orleans.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Orleans;
using GatewayNetworking = Gateway::AbilityKit.Orleans.Gateway.Networking;

var options = ShooterSmokeProgramOptions.Parse(args);
var tcpGatewayHost = options.Host;

if (options.ClientMode)
{
    var clientOptions = options.ToClientProcessOptions();

    try
    {
        var result = await ShooterSmokeClientProcessRunner.RunAsync(clientOptions);
        Console.WriteLine(ShooterSmokeClientProcessRunner.FormatResult(result));
    }
    catch (Exception ex)
    {
        Console.WriteLine(ShooterSmokeClientProcessRunner.FormatFailure(clientOptions, ex));
        Environment.ExitCode = 1;
    }

    return;
}

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddAbilityKitServerOptions(builder.Configuration);
builder.Logging.AddAbilityKitServerLogging(builder.Configuration, "AbilityKit.Orleans.ShooterSmoke");

var storageOptions = builder.Configuration.GetAbilityKitStorageOptions();
builder.Services.AddAbilityKitGrainStateStorage(
    storageOptions.SessionStateProvider,
    storageOptions.RoomStateProvider);

builder.Services.AddSingleton<ServerBattleWorldManager>(sp =>
    new ServerBattleWorldManager(sp.GetRequiredService<ILogger<ServerBattleWorldManager>>()));
builder.Services.AddShooterSmokeGateway(options.TcpGatewayPort);

builder.UseAbilityKitLocalOrleansSilo();

using var host = builder.Build();
await host.StartAsync();

using var transportCts = new CancellationTokenSource();
var transportServer = host.Services.GetRequiredService<GatewayNetworking.TcpTransportServer>();
var transportTask = transportServer.StartAsync(transportCts.Token);
await ShooterSmokeScenarioBase.WaitForTcpAsync(tcpGatewayHost, options.TcpGatewayPort, TimeSpan.FromSeconds(5));

try
{
    if (options.ServerMode)
    {
        Console.WriteLine($"Shooter state-sync server listening on {tcpGatewayHost}:{options.TcpGatewayPort}.");
        Console.WriteLine("Press Ctrl+C to stop.");
        await WaitForShutdownAsync();
    }
    else
    {
        var clusterClient = host.Services.GetRequiredService<IClusterClient>();
        var result = await ShooterSmokeRunner.RunAsync(clusterClient, tcpGatewayHost, options.TcpGatewayPort);
        Console.WriteLine(ShooterSmokeResultFormatter.FormatPassed(result));
    }
}
finally
{
    transportCts.Cancel();
    await transportServer.StopAsync();
    await AwaitTransportShutdownAsync(transportTask);
    await host.StopAsync();
}

static Task WaitForShutdownAsync()
{
    var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
    Console.CancelKeyPress += (_, eventArgs) =>
    {
        eventArgs.Cancel = true;
        completion.TrySetResult();
    };

    AppDomain.CurrentDomain.ProcessExit += (_, _) => completion.TrySetResult();
    return completion.Task;
}

static async Task AwaitTransportShutdownAsync(Task transportTask)
{
    try
    {
        await transportTask;
    }
    catch (OperationCanceledException)
    {
    }
}

readonly record struct ShooterSmokeProgramOptions(
    bool ServerMode,
    bool ClientMode,
    ShooterSmokeClientProcessMode ClientProcessMode,
    string Host,
    int TcpGatewayPort,
    string RoomId,
    uint PlayerId,
    string ClientId,
    int InputCount,
    int Seed,
    TimeSpan Timeout,
    bool WaitForMatchEnd,
    bool ReconnectOnce,
    int ReconnectDelayMs,
    SmokeNetworkConditionOptions NetworkCondition,
    string ReplayOutputPath)
{
    public ShooterSmokeClientProcessOptions ToClientProcessOptions()
    {
        return new ShooterSmokeClientProcessOptions(
            ClientProcessMode,
            Host,
            TcpGatewayPort,
            RoomId,
            PlayerId,
            ClientId,
            InputCount,
            Seed,
            Timeout,
            WaitForMatchEnd,
            ReconnectOnce,
            ReconnectDelayMs,
            NetworkCondition,
            ReplayOutputPath);
    }

    public static ShooterSmokeProgramOptions Parse(string[] args)
    {
        var serverMode = false;
        var clientMode = false;
        var clientProcessMode = ShooterSmokeClientProcessMode.Create;
        var host = ShooterSmokeScenarioBase.DefaultTcpGatewayHost;
        var tcpGatewayPort = 41001;
        var roomId = string.Empty;
        uint playerId = 1;
        var clientId = $"shooter-mp-{Environment.ProcessId}";
        var inputCount = 3;
        var seed = 20260610;
        var timeout = TimeSpan.FromSeconds(15);
        var waitForMatchEnd = false;
        var reconnectOnce = false;
        var reconnectDelayMs = 500;
        var conditionLatencyMs = 0;
        var conditionJitterMs = 0;
        var conditionPacketLossRate = 0d;
        var conditionSeed = 20260610;
        var replayOutputPath = string.Empty;

        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            if (string.Equals(arg, "--server", StringComparison.OrdinalIgnoreCase))
            {
                serverMode = true;
            }
            else if (string.Equals(arg, "--client", StringComparison.OrdinalIgnoreCase))
            {
                clientMode = true;
            }
            else if (string.Equals(arg, "--client-mode", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
            {
                var value = args[++i];
                if (string.Equals(value, "join", StringComparison.OrdinalIgnoreCase))
                {
                    clientProcessMode = ShooterSmokeClientProcessMode.Join;
                }
                else if (string.Equals(value, "create", StringComparison.OrdinalIgnoreCase))
                {
                    clientProcessMode = ShooterSmokeClientProcessMode.Create;
                }
            }
            else if (string.Equals(arg, "--host", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
            {
                var value = args[++i];
                if (!string.IsNullOrWhiteSpace(value))
                {
                    host = value;
                }
            }
            else if (string.Equals(arg, "--tcp-port", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
            {
                if (int.TryParse(args[++i], out var parsedPort) && parsedPort > 0 && parsedPort <= 65535)
                {
                    tcpGatewayPort = parsedPort;
                }
            }
            else if (string.Equals(arg, "--room-id", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
            {
                roomId = args[++i];
            }
            else if (string.Equals(arg, "--player-id", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
            {
                if (uint.TryParse(args[++i], out var parsedPlayerId) && parsedPlayerId > 0)
                {
                    playerId = parsedPlayerId;
                }
            }
            else if (string.Equals(arg, "--client-id", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
            {
                var value = args[++i];
                if (!string.IsNullOrWhiteSpace(value))
                {
                    clientId = value;
                }
            }
            else if (string.Equals(arg, "--inputs", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
            {
                if (int.TryParse(args[++i], out var parsedInputs) && parsedInputs >= 0)
                {
                    inputCount = parsedInputs;
                }
            }
            else if (string.Equals(arg, "--seed", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
            {
                if (int.TryParse(args[++i], out var parsedSeed))
                {
                    seed = parsedSeed;
                }
            }
            else if (string.Equals(arg, "--wait-for-match-end", StringComparison.OrdinalIgnoreCase))
            {
                waitForMatchEnd = true;
            }
            else if (string.Equals(arg, "--reconnect-once", StringComparison.OrdinalIgnoreCase))
            {
                reconnectOnce = true;
            }
            else if (string.Equals(arg, "--reconnect-delay-ms", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
            {
                if (int.TryParse(args[++i], out var parsedDelayMs) && parsedDelayMs >= 0)
                {
                    reconnectDelayMs = parsedDelayMs;
                }
            }
            else if (string.Equals(arg, "--condition-latency-ms", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
            {
                if (int.TryParse(args[++i], out var parsedLatencyMs) && parsedLatencyMs >= 0)
                {
                    conditionLatencyMs = parsedLatencyMs;
                }
            }
            else if (string.Equals(arg, "--condition-jitter-ms", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
            {
                if (int.TryParse(args[++i], out var parsedJitterMs) && parsedJitterMs >= 0)
                {
                    conditionJitterMs = parsedJitterMs;
                }
            }
            else if (string.Equals(arg, "--condition-packet-loss-rate", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
            {
                if (double.TryParse(args[++i], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var parsedLossRate))
                {
                    conditionPacketLossRate = parsedLossRate;
                }
            }
            else if (string.Equals(arg, "--condition-seed", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
            {
                if (int.TryParse(args[++i], out var parsedConditionSeed))
                {
                    conditionSeed = parsedConditionSeed;
                }
            }
            else if (string.Equals(arg, "--timeout-seconds", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
            {
                if (int.TryParse(args[++i], out var parsedSeconds) && parsedSeconds > 0)
                {
                    timeout = TimeSpan.FromSeconds(parsedSeconds);
                }
            }
            else if (string.Equals(arg, "--replay-output", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
            {
                replayOutputPath = args[++i];
            }
        }

        return new ShooterSmokeProgramOptions(
            serverMode,
            clientMode,
            clientProcessMode,
            host,
            tcpGatewayPort,
            roomId,
            playerId,
            clientId,
            inputCount,
            seed,
            timeout,
            waitForMatchEnd,
            reconnectOnce,
            reconnectDelayMs,
            new SmokeNetworkConditionOptions(
                conditionLatencyMs,
                conditionJitterMs,
                conditionPacketLossRate,
                conditionSeed).Normalize(),
            replayOutputPath);
    }
}

