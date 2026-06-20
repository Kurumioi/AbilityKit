namespace AbilityKit.Orleans.Gateway.HttpApi;

using AbilityKit.Orleans.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using GatewayAbstractions = AbilityKit.Orleans.Gateway.Abstractions;
using GatewayCore = AbilityKit.Orleans.Gateway.Core;
using GatewayNetworking = AbilityKit.Orleans.Gateway.Networking;

internal static class GatewayModuleExtensions
{
    public static IServiceCollection AddAbilityKitGatewayModule(this IServiceCollection services, IConfiguration configuration)
    {
        var legacyTcpGatewaySection = configuration.GetSection(AbilityKitServerConfigurationSections.LegacyTcpGateway);
        var tcpGatewaySection = configuration.GetSection(AbilityKitServerConfigurationSections.Gateway + ":Tcp");
        if (!tcpGatewaySection.Exists())
        {
            tcpGatewaySection = legacyTcpGatewaySection;
        }

        services.AddOptions<GatewayCore.GatewayOptions>()
            .Bind(tcpGatewaySection)
            .Validate(options => options.RequestTimeoutMs > 0, "Gateway request timeout must be greater than zero.")
            .Validate(options => options.MaxFrameLength > 0, "Gateway max frame length must be greater than zero.")
            .ValidateOnStart();

        services.AddOptions<GatewayNetworking.TcpTransportOptions>()
            .Bind(tcpGatewaySection)
            .Validate(options => !string.IsNullOrWhiteSpace(options.Host), "TCP gateway host is required.")
            .Validate(options => options.Port is > 0 and <= 65535, "TCP gateway port must be between 1 and 65535.")
            .Validate(options => options.MaxFrameLength > 0, "TCP gateway max frame length must be greater than zero.")
            .Validate(options => options.RequestTimeoutMs > 0, "TCP gateway request timeout must be greater than zero.")
            .ValidateOnStart();

        services.AddSingleton<GatewayCore.GatewaySessionRegistry>();
        services.AddSingleton<GatewayAbstractions.IGatewaySessionRegistry>(sp => sp.GetRequiredService<GatewayCore.GatewaySessionRegistry>());

        services.AddSingleton<GatewayHandlers.GuestLoginHandler>();
        services.AddSingleton<GatewayHandlers.TimeSyncHandler>();
        services.AddSingleton<GatewayHandlers.CreateRoomHandler>();
        services.AddSingleton<GatewayHandlers.JoinRoomHandler>();
        services.AddSingleton<GatewayHandlers.RestoreRoomHandler>();
        services.AddSingleton<GatewayHandlers.RoomReadyHandler>();
        services.AddSingleton<GatewayHandlers.RoomPickHeroHandler>();
        services.AddSingleton<GatewayHandlers.StartRoomBattleHandler>();
        services.AddSingleton<GatewayHandlers.SubmitBattleInputHandler>();
        services.AddSingleton<GatewayHandlers.SubscribeStateSyncHandler>();
        services.AddSingleton<GatewayHandlers.RequestFullStateSyncHandler>();

        services.AddSingleton<GatewayCore.GatewayHandlerRegistry>(sp =>
        {
            var registry = new GatewayCore.GatewayHandlerRegistry(sp);
            registry.RegisterFromAssembly(typeof(GatewayCore.GatewayHandlerRegistry).Assembly);
            return registry;
        });
        services.AddSingleton<GatewayAbstractions.IGatewayHandlerRegistry>(sp => sp.GetRequiredService<GatewayCore.GatewayHandlerRegistry>());

        services.AddSingleton<GatewayCore.GatewayRequestRouter>();
        services.AddSingleton<GatewayAbstractions.IGatewayRequestRouter>(sp => sp.GetRequiredService<GatewayCore.GatewayRequestRouter>());

        services.AddSingleton<GatewayCore.GatewayBackgroundTaskQueue>();
        services.AddHostedService(sp => sp.GetRequiredService<GatewayCore.GatewayBackgroundTaskQueue>());
        services.AddSingleton<GatewayAbstractions.IGatewayTransportEvents, GatewayCore.GatewayTransportHandler>();
        services.AddSingleton<GatewayCore.GatewayTransportHandler>();

        services.AddSingleton<GatewayNetworking.TcpTransportServer>();
        services.AddHostedService<GatewayCore.TcpTransportHostedService>();

        services.AddSingleton<GatewayCore.GatewayPushTargetGrain>();
        services.AddSingleton<AbilityKit.Orleans.Contracts.Battle.IGatewayPushTargetGrain>(sp => sp.GetRequiredService<GatewayCore.GatewayPushTargetGrain>());

        return services;
    }

    public static WebApplication MapAbilityKitGatewayPipeline(this WebApplication app)
    {
        var gatewayOptions = app.Services.GetRequiredService<IOptions<AbilityKitGatewayOptions>>().Value;
        var deploymentOptions = app.Services.GetRequiredService<IOptions<AbilityKitSiloPlacementOptions>>().Value;
        if (!string.IsNullOrWhiteSpace(gatewayOptions.Http.NormalizedPathBase))
        {
            app.UsePathBase(gatewayOptions.Http.NormalizedPathBase);
        }

        app.UseStaticFiles();
        app.MapAbilityKitGatewayHealthEndpoints(deploymentOptions);
        app.MapGatewayHttpApi();
        return app;
    }
}
