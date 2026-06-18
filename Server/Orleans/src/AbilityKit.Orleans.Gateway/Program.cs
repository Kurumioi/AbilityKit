using AbilityKit.Orleans.Gateway.HttpApi;
using AbilityKit.Orleans.Hosting;
using GatewayAbstractions = AbilityKit.Orleans.Gateway.Abstractions;
using GatewayHandlers = AbilityKit.Orleans.Gateway.Handlers;
using GatewayCore = AbilityKit.Orleans.Gateway.Core;
using GatewayNetworking = AbilityKit.Orleans.Gateway.Networking;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddAbilityKitServerOptions(builder.Configuration);
builder.Logging.AddAbilityKitServerLogging(builder.Configuration, "AbilityKit.Orleans.Gateway");

// Gateway 配置
builder.Services.AddOptions<GatewayCore.GatewayOptions>()
    .Bind(builder.Configuration.GetSection("TcpGateway"))
    .Validate(options => options.RequestTimeoutMs > 0, "Gateway request timeout must be greater than zero.")
    .Validate(options => options.MaxFrameLength > 0, "Gateway max frame length must be greater than zero.")
    .ValidateOnStart();

// TCP 传输层配置
builder.Services.AddOptions<GatewayNetworking.TcpTransportOptions>()
    .Bind(builder.Configuration.GetSection("TcpGateway"))
    .Validate(options => !string.IsNullOrWhiteSpace(options.Host), "TCP gateway host is required.")
    .Validate(options => options.Port is > 0 and <= 65535, "TCP gateway port must be between 1 and 65535.")
    .Validate(options => options.MaxFrameLength > 0, "TCP gateway max frame length must be greater than zero.")
    .Validate(options => options.RequestTimeoutMs > 0, "TCP gateway request timeout must be greater than zero.")
    .ValidateOnStart();

// 注册核心组件
builder.Services.AddSingleton<GatewayCore.GatewaySessionRegistry>();
builder.Services.AddSingleton<GatewayAbstractions.IGatewaySessionRegistry>(sp => sp.GetRequiredService<GatewayCore.GatewaySessionRegistry>());

// 注册 Handler（使用 DI）
builder.Services.AddSingleton<GatewayHandlers.GuestLoginHandler>();
builder.Services.AddSingleton<GatewayHandlers.TimeSyncHandler>();
builder.Services.AddSingleton<GatewayHandlers.CreateRoomHandler>();
builder.Services.AddSingleton<GatewayHandlers.JoinRoomHandler>();
builder.Services.AddSingleton<GatewayHandlers.RestoreRoomHandler>();
builder.Services.AddSingleton<GatewayHandlers.RoomReadyHandler>();
builder.Services.AddSingleton<GatewayHandlers.RoomPickHeroHandler>();
builder.Services.AddSingleton<GatewayHandlers.StartRoomBattleHandler>();
builder.Services.AddSingleton<GatewayHandlers.SubmitBattleInputHandler>();
builder.Services.AddSingleton<GatewayHandlers.SubscribeStateSyncHandler>();
builder.Services.AddSingleton<GatewayHandlers.RequestFullStateSyncHandler>();

// 注册 Handler Registry（需要 DI 容器）
builder.Services.AddSingleton<GatewayCore.GatewayHandlerRegistry>(sp =>
{
    var registry = new GatewayCore.GatewayHandlerRegistry(sp);
    registry.RegisterFromAssembly(typeof(GatewayCore.GatewayHandlerRegistry).Assembly);
    return registry;
});
builder.Services.AddSingleton<GatewayAbstractions.IGatewayHandlerRegistry>(sp => sp.GetRequiredService<GatewayCore.GatewayHandlerRegistry>());

// 注册 Router
builder.Services.AddSingleton<GatewayCore.GatewayRequestRouter>();
builder.Services.AddSingleton<GatewayAbstractions.IGatewayRequestRouter>(sp => sp.GetRequiredService<GatewayCore.GatewayRequestRouter>());

// 注册传输层事件处理器（在 TcpTransportServer 之前）
builder.Services.AddSingleton<GatewayCore.GatewayBackgroundTaskQueue>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<GatewayCore.GatewayBackgroundTaskQueue>());
builder.Services.AddSingleton<GatewayAbstractions.IGatewayTransportEvents, GatewayCore.GatewayTransportHandler>();
builder.Services.AddSingleton<GatewayCore.GatewayTransportHandler>();

// 注册传输层
builder.Services.AddSingleton<GatewayNetworking.TcpTransportServer>();
builder.Services.AddHostedService<GatewayCore.TcpTransportHostedService>();

// 注册 Gateway Push Target Grain
builder.Services.AddSingleton<GatewayCore.GatewayPushTargetGrain>();
builder.Services.AddSingleton<AbilityKit.Orleans.Contracts.Battle.IGatewayPushTargetGrain>(sp => sp.GetRequiredService<GatewayCore.GatewayPushTargetGrain>());

// Orleans 客户端
builder.Host.UseAbilityKitLocalOrleansClient(builder.Configuration);

var app = builder.Build();

app.UseStaticFiles();

app.MapGet("/health", () => Results.Ok("OK"));
app.MapGatewayHttpApi();

// 使用端口 5001
app.Run("http://localhost:5001");
