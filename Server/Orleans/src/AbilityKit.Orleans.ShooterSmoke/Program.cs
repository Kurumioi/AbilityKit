extern alias Gateway;

using AbilityKit.Orleans.Grains.Battle;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;
using GatewayNetworking = Gateway::AbilityKit.Orleans.Gateway.Networking;

const int tcpGatewayPort = 41001;
const string tcpGatewayHost = "127.0.0.1";

var builder = Host.CreateApplicationBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

builder.Services.AddSingleton<ServerMobaWorldManager>(sp =>
    new ServerMobaWorldManager(sp.GetRequiredService<ILogger<ServerMobaWorldManager>>()));
builder.Services.AddShooterSmokeGateway(tcpGatewayPort);

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
var transportTask = Task.Run(() => transportServer.StartAsync(transportCts.Token));
await ShooterSmokeRunner.WaitForTcpAsync(tcpGatewayHost, tcpGatewayPort, TimeSpan.FromSeconds(5));

try
{
    var clusterClient = host.Services.GetRequiredService<IClusterClient>();
    var result = await ShooterSmokeRunner.RunAsync(clusterClient, tcpGatewayHost, tcpGatewayPort);
    Console.WriteLine(ShooterSmokeResultFormatter.FormatPassed(result));
}
finally
{
    transportCts.Cancel();
    await transportServer.StopAsync();
    await host.StopAsync();

    if (transportTask.IsFaulted && transportTask.Exception != null)
    {
        _ = transportTask.Exception;
    }
}
