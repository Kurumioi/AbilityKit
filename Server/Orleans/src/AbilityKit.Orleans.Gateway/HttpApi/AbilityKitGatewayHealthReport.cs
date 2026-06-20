using System.Collections.Generic;

namespace AbilityKit.Orleans.Gateway.HttpApi;

internal sealed record AbilityKitGatewayHealthReport(
    string Status,
    string Service,
    DateTimeOffset CheckedAtUtc,
    string HttpUrl,
    string TcpEndpoint,
    AbilityKitGatewayRuntimeDiagnostics Runtime,
    AbilityKitGatewayGameplayDiagnostics Gameplay,
    AbilityKitGatewayDeploymentDiagnostics Deployment)
{
    public static AbilityKitGatewayHealthReport Ready(
        string service,
        string httpUrl,
        string tcpHost,
        int tcpPort,
        AbilityKitGatewayRuntimeDiagnostics runtime,
        AbilityKitGatewayGameplayDiagnostics gameplay,
        AbilityKitGatewayDeploymentDiagnostics deployment)
    {
        return new AbilityKitGatewayHealthReport(
            "Ready",
            service,
            DateTimeOffset.UtcNow,
            httpUrl,
            $"{tcpHost}:{tcpPort}",
            runtime,
            gameplay,
            deployment);
    }
}

internal sealed record AbilityKitGatewayRuntimeDiagnostics(
    string RootPath,
    string Environment,
    string HttpScheme,
    string HttpHost,
    int HttpPort,
    bool HasCustomPathBase)
{
    public static AbilityKitGatewayRuntimeDiagnostics FromOptions(AbilityKit.Orleans.Hosting.AbilityKitGatewayOptions options, string environment)
    {
        return new AbilityKitGatewayRuntimeDiagnostics(
            options.Http.RootPath,
            environment,
            options.Http.Scheme,
            options.Http.Host,
            options.Http.Port,
            !string.IsNullOrWhiteSpace(options.Http.PathBase));
    }
}

internal sealed record AbilityKitGatewayGameplayDiagnostics(
    int ModuleCount,
    string DefaultRoomType,
    IReadOnlyList<string> SupportedRoomTypes,
    IReadOnlyList<string> SupportedSyncTemplateIds)
{
    public static AbilityKitGatewayGameplayDiagnostics FromCatalog(AbilityKit.Orleans.Grains.Gameplay.ServerGameplayModuleCatalog catalog)
    {
        var roomTypes = new List<string>();
        var templateIds = new List<string>();

        foreach (var module in catalog.Modules)
        {
            roomTypes.Add(module.RoomType);
            templateIds.AddRange(module.SyncTemplateIds);
        }

        return new AbilityKitGatewayGameplayDiagnostics(
            catalog.Modules.Count,
            catalog.DefaultRoomType,
            roomTypes,
            templateIds);
    }
}
