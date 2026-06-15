extern alias Gateway;

using AbilityKit.Orleans.Grains.Battle;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using GatewayAbstractions = Gateway::AbilityKit.Orleans.Gateway.Abstractions;
using GatewayCore = Gateway::AbilityKit.Orleans.Gateway.Core;
using GatewayHandlers = Gateway::AbilityKit.Orleans.Gateway.Handlers;
using GatewayNetworking = Gateway::AbilityKit.Orleans.Gateway.Networking;
internal static class ShooterSmokeGatewayServiceCollectionExtensions
{
    public static IServiceCollection AddShooterSmokeGateway(this IServiceCollection services, int tcpGatewayPort)
    {
        services.Configure<GatewayCore.GatewayOptions>(options =>
        {
            options.RequestTimeoutMs = 30000;
            options.MaxFrameLength = 1024 * 1024;
        });

        services.Configure<GatewayNetworking.TcpTransportOptions>(options =>
        {
            options.Enabled = true;
            options.Host = "127.0.0.1";
            options.Port = tcpGatewayPort;
            options.RequestTimeoutMs = 30000;
            options.MaxFrameLength = 1024 * 1024;
        });

        services.AddSingleton<GatewayCore.GatewaySessionRegistry>();
        services.AddSingleton<GatewayAbstractions.IGatewaySessionRegistry>(sp => sp.GetRequiredService<GatewayCore.GatewaySessionRegistry>());

        services.AddSingleton<GatewayHandlers.GuestLoginHandler>();
        services.AddSingleton<GatewayHandlers.TimeSyncHandler>();
        services.AddSingleton<GatewayHandlers.CreateRoomHandler>();
        services.AddSingleton<GatewayHandlers.JoinRoomHandler>();
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

        services.AddSingleton<GatewayNetworking.IGatewayTransportEvents, GatewayCore.GatewayTransportHandler>();
        services.AddSingleton<GatewayCore.GatewayTransportHandler>();
        services.AddSingleton<GatewayNetworking.TcpTransportServer>();

        services.AddSingleton<GatewayCore.GatewayPushTargetGrain>();
        services.AddSingleton<AbilityKit.Orleans.Contracts.Battle.IGatewayPushTargetGrain>(sp => sp.GetRequiredService<GatewayCore.GatewayPushTargetGrain>());
        return services;
    }
}
