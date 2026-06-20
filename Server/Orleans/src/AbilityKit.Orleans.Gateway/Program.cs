using AbilityKit.Orleans.Gateway.HttpApi;
using AbilityKit.Orleans.Hosting;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddAbilityKitServerOptions(builder.Configuration);
builder.Services.AddAbilityKitDeploymentOptions(builder.Configuration);
builder.Services.AddAbilityKitSiloRoleOptions(builder.Configuration);
builder.Services.AddAbilityKitSiloRuntimeProfileOptions(builder.Configuration);
builder.Services.AddAbilityKitDeploymentModeOptions(builder.Configuration);
builder.Logging.AddAbilityKitServerLogging(builder.Configuration, "AbilityKit.Orleans.Gateway");
builder.Services.AddAbilityKitGatewayModule(builder.Configuration);
builder.Host.UseAbilityKitLocalOrleansClient(builder.Configuration);

var app = builder.Build();
app.MapAbilityKitGatewayPipeline();

app.Run(app.Services.GetRequiredService<IOptions<AbilityKitGatewayOptions>>().Value.Http.Url);
