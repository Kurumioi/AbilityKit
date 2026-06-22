namespace AbilityKit.Orleans.Gateway.HttpApi;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using AbilityKit.Orleans.Hosting;

public static class GatewayModuleExtensions
{
    public static IServiceCollection AddAbilityKitGatewayModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<AbilityKitGatewayOptions>()
            .Bind(configuration.GetSection(AbilityKitServerConfigurationSections.Gateway));

        services.AddGatewayHttpApi();
        return services;
    }

    public static WebApplication MapAbilityKitGatewayPipeline(this WebApplication app)
    {
        var options = app.Services.GetRequiredService<Microsoft.Extensions.Options.IOptions<AbilityKitGatewayOptions>>().Value;
        app.UseDefaultFiles();
        app.UseStaticFiles();
        app.MapAbilityKitGatewayHealthEndpoints(options);
        app.MapGatewayHttpApi();
        app.MapGet("/admin", () => Results.Redirect("/admin/index.html"))
            .WithName("Gateway.AdminConsole")
            .Produces(StatusCodes.Status302Found);
        app.MapGet("/debug", () => Results.Redirect("/debug/index.html"))
            .WithName("Gateway.DebugConsole")
            .Produces(StatusCodes.Status302Found);
        return app;
    }
}
