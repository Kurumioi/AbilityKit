namespace AbilityKit.Orleans.Gateway.HttpApi;

using AbilityKit.Orleans.Hosting;
using Microsoft.AspNetCore.Hosting;
using Orleans;

internal static class GatewayClusterDiagnostics
{
    public static AdminClusterDiagnosticsHttpResponse GetDiagnostics(
        IClusterClient client,
        AbilityKitOrleansClusterOptions clusterOptions,
        IWebHostEnvironment environment)
    {
        var warnings = new List<string>();
        var runtimeMetrics = new List<string>
        {
            "Orleans runtime metrics adapter is not connected yet.",
            "Future metrics: silo membership, activation count, request throughput, failure rate, reminder/stream status."
        };

        var clientConnected = client is not null;
        var clientStatus = clientConnected ? "ClientConfigured" : "Unknown";
        if (clusterOptions.GatewayPort is null)
        {
            warnings.Add("Orleans gateway port is not explicitly configured; localhost clustering fallback may be used.");
        }

        var nodes = new List<AdminClusterNodeProbeHttpResponse>
        {
            new(
                "gateway-client",
                "GatewayClient",
                $"localhost:{clusterOptions.GatewayPort ?? 30000}",
                clientStatus,
                "Represents the Gateway process Orleans client connection target."),
            new(
                "local-silo",
                "Silo",
                $"localhost:{clusterOptions.SiloPort ?? 11111}",
                "Configured",
                "Configured local silo endpoint. Deep membership status requires Orleans metrics/dashboard integration.")
        };

        return new AdminClusterDiagnosticsHttpResponse(
            clusterOptions.ClusterId,
            clusterOptions.ServiceId,
            clusterOptions.SiloPort,
            clusterOptions.GatewayPort,
            clientConnected,
            clientStatus,
            nodes.ToArray(),
            runtimeMetrics.ToArray(),
            warnings.ToArray(),
            GatewayAdminOperations.GetStatus(environment),
            DateTime.UtcNow.Ticks);
    }
}
