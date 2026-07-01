using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Orleans.Configuration;
using Orleans.Hosting;

namespace AbilityKit.Orleans.Hosting;

public static class AbilityKitOrleansHostingExtensions
{
    public static HostApplicationBuilder UseAbilityKitLocalOrleansSilo(
        this HostApplicationBuilder builder,
        AbilityKitOrleansClusterOptions? options = null,
        Action<ISiloBuilder>? configureSilo = null)
    {
        options ??= builder.Configuration.GetAbilityKitOrleansOptions();

        builder.UseOrleans(silo =>
        {
            if (options.SiloPort.HasValue || options.GatewayPort.HasValue || options.PrimarySiloPort.HasValue)
            {
                silo.UseLocalhostClustering(
                    siloPort: options.SiloPort ?? 11111,
                    gatewayPort: options.GatewayPort ?? 30000,
                    primarySiloEndpoint: new System.Net.IPEndPoint(System.Net.IPAddress.Loopback, options.PrimarySiloPort ?? options.SiloPort ?? 11111));
            }
            else
            {
                silo.UseLocalhostClustering();
            }

            silo.Configure<ClusterOptions>(clusterOptions =>
            {
                clusterOptions.ClusterId = options.ClusterId;
                clusterOptions.ServiceId = options.ServiceId;
            });

            configureSilo?.Invoke(silo);
        });

        return builder;
    }

    public static IHostBuilder UseAbilityKitLocalOrleansClient(
        this IHostBuilder hostBuilder,
        IConfiguration configuration,
        AbilityKitOrleansClusterOptions? options = null)
    {
        options ??= configuration.GetAbilityKitOrleansOptions();

        return hostBuilder.UseOrleansClient(client =>
        {
            client.UseLocalhostClustering(options.GatewayPort ?? 30000);
            client.Configure<ClusterOptions>(clusterOptions =>
            {
                clusterOptions.ClusterId = options.ClusterId;
                clusterOptions.ServiceId = options.ServiceId;
            });
        });
    }
}
