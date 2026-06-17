extern alias Gateway;

using AbilityKit.Orleans.Grains.Battle;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;
using GatewayNetworking = Gateway::AbilityKit.Orleans.Gateway.Networking;

var options = ShooterSmokeProgramOptions.Parse(args);
const string tcpGatewayHost = "127.0.0.1";

var builder = Host.CreateApplicationBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

builder.Services.AddSingleton<ServerMobaWorldManager>(sp =>
    new ServerMobaWorldManager(sp.GetRequiredService<ILogger<ServerMobaWorldManager>>()));
builder.Services.AddShooterSmokeGateway(options.TcpGatewayPort);

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

using var transportCts = new CancellationTokenSource();
var transportServer = host.Services.GetRequiredService<GatewayNetworking.TcpTransportServer>();
var transportTask = transportServer.StartAsync(transportCts.Token);
await ShooterSmokeRunner.WaitForTcpAsync(tcpGatewayHost, options.TcpGatewayPort, TimeSpan.FromSeconds(5));

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

readonly record struct ShooterSmokeProgramOptions(bool ServerMode, int TcpGatewayPort)
{
    public static ShooterSmokeProgramOptions Parse(string[] args)
    {
        var serverMode = false;
        var tcpGatewayPort = 41001;

        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            if (string.Equals(arg, "--server", StringComparison.OrdinalIgnoreCase))
            {
                serverMode = true;
            }
            else if (string.Equals(arg, "--tcp-port", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
            {
                if (int.TryParse(args[++i], out var parsedPort) && parsedPort > 0 && parsedPort <= 65535)
                {
                    tcpGatewayPort = parsedPort;
                }
            }
        }

        return new ShooterSmokeProgramOptions(serverMode, tcpGatewayPort);
    }
}
