using AbilityKit.Orleans.Gateway.HttpApi;
using Orleans.Configuration;
using Orleans.Hosting;
using GatewayAbstractions = AbilityKit.Orleans.Gateway.Abstractions;
using GatewayHandlers = AbilityKit.Orleans.Gateway.Handlers;
using GatewayCore = AbilityKit.Orleans.Gateway.Core;
using GatewayNetworking = AbilityKit.Orleans.Gateway.Networking;

var builder = WebApplication.CreateBuilder(args);

// Gateway 配置
builder.Services.Configure<GatewayCore.GatewayOptions>(options =>
{
    options.RequestTimeoutMs = 30000;
    options.MaxFrameLength = 1024 * 1024;
});

// TCP 传输层配置
builder.Services.Configure<GatewayNetworking.TcpTransportOptions>(builder.Configuration.GetSection("TcpGateway"));

// 注册核心组件
builder.Services.AddSingleton<GatewayCore.GatewaySessionRegistry>();
builder.Services.AddSingleton<GatewayAbstractions.IGatewaySessionRegistry>(sp => sp.GetRequiredService<GatewayCore.GatewaySessionRegistry>());

// 注册 Handler（使用 DI）
builder.Services.AddSingleton<GatewayHandlers.GuestLoginHandler>();
builder.Services.AddSingleton<GatewayHandlers.TimeSyncHandler>();
builder.Services.AddSingleton<GatewayHandlers.CreateRoomHandler>();
builder.Services.AddSingleton<GatewayHandlers.JoinRoomHandler>();
builder.Services.AddSingleton<GatewayHandlers.RoomReadyHandler>();
builder.Services.AddSingleton<GatewayHandlers.RoomPickHeroHandler>();
builder.Services.AddSingleton<GatewayHandlers.StartRoomBattleHandler>();
builder.Services.AddSingleton<GatewayHandlers.SubmitBattleInputHandler>();
builder.Services.AddSingleton<GatewayHandlers.SubscribeStateSyncHandler>();

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
builder.Services.AddSingleton<GatewayNetworking.IGatewayTransportEvents, GatewayCore.GatewayTransportHandler>();
builder.Services.AddSingleton<GatewayCore.GatewayTransportHandler>();

// 注册传输层
builder.Services.AddSingleton<GatewayNetworking.TcpTransportServer>();

// 注册 Gateway Push Target Grain
builder.Services.AddSingleton<GatewayCore.GatewayPushTargetGrain>();
builder.Services.AddSingleton<AbilityKit.Orleans.Contracts.Battle.IGatewayPushTargetGrain>(sp => sp.GetRequiredService<GatewayCore.GatewayPushTargetGrain>());

// Orleans Client
builder.Host.UseOrleansClient(client =>
{
    client.UseLocalhostClustering();
    client.Configure<ClusterOptions>(options =>
    {
        options.ClusterId = "abilitykit-dev";
        options.ServiceId = "abilitykit-orleans";
    });
});

var app = builder.Build();

// 启动 TCP 传输层
var transportServer = app.Services.GetRequiredService<GatewayNetworking.TcpTransportServer>();
_ = Task.Run(() => transportServer.StartAsync());

app.UseStaticFiles();

app.MapGet("/health", () => Results.Ok("OK"));
app.MapGatewayHttpApi();

// 使用端口 5001
app.Run("http://localhost:5001");
