using AbilityKit.Orleans.Gateway.TcpGateway;
using AbilityKit.Orleans.Gateway.TcpGateway.Handler;
using AbilityKit.Orleans.Gateway.TcpGateway.StateSync;
using Orleans.Configuration;
using Orleans.Hosting;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOptions<TcpGatewayOptions>()
    .Bind(builder.Configuration.GetSection("TcpGateway"));

// 注册 Session Registry
builder.Services.AddSingleton<ITcpGatewaySessionRegistry, TcpGatewaySessionRegistry>();

// 注册 Handler Registry（基于 Attribute 的 Handler 注册）
var handlerRegistry = new HandlerRegistry();
handlerRegistry.RegisterFromAssembly(typeof(GuestLoginRequestHandler).Assembly);
builder.Services.AddSingleton(handlerRegistry);

// 注册所有 Handler
builder.Services.AddSingleton<GuestLoginRequestHandler>();
builder.Services.AddSingleton<CreateRoomRequestHandler>();
builder.Services.AddSingleton<JoinRoomRequestHandler>();
builder.Services.AddSingleton<LeaveRoomRequestHandler>();
builder.Services.AddSingleton<ListRoomsRequestHandler>();
builder.Services.AddSingleton<CloseRoomRequestHandler>();
builder.Services.AddSingleton<RenewSessionRequestHandler>();
builder.Services.AddSingleton<LogoutRequestHandler>();
builder.Services.AddSingleton<CreateSessionForAccountRequestHandler>();
builder.Services.AddSingleton<TimeSyncRequestHandler>();
builder.Services.AddSingleton<FrameSyncObserverHub>();
builder.Services.AddSingleton<SubmitFrameInputRequestHandler>();

// StateSync Handler
builder.Services.AddSingleton<TcpGatewayStateSyncHandler>();
builder.Services.AddSingleton<IStateSyncHandler>(sp => sp.GetRequiredService<TcpGatewayStateSyncHandler>());

// 注册 Router
builder.Services.AddSingleton<TcpGatewayRequestRouter>();
builder.Services.AddHostedService<TcpGatewayListener>();

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

app.UseStaticFiles();

app.MapGet("/health", () => Results.Ok("OK"));
app.MapGet("/debug", () => Results.Redirect("/debug/"));

app.Run("http://localhost:5001");
