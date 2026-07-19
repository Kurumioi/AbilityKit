namespace AbilityKit.Orleans.Gateway.HttpApi;

using AbilityKit.Orleans.Contracts.Battle;
using AbilityKit.Orleans.Gateway.Handlers;
using AbilityKit.Orleans.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

public static class GatewayModuleExtensions
{
    public static IServiceCollection AddAbilityKitGatewayModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<AbilityKitGatewayOptions>()
            .Bind(configuration.GetSection(AbilityKitServerConfigurationSections.Gateway));
        services.AddOptions<BattleInputSecurityOptions>()
            .Bind(configuration.GetSection(BattleInputSecurityOptions.ConfigurationSection))
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<BattleInputSecurityOptions>, BattleInputSecurityOptionsValidator>();
        services.AddSingleton(serviceProvider =>
            new GatewayBattleInputGuard(
                serviceProvider.GetRequiredService<IOptions<BattleInputSecurityOptions>>().Value));
        services.AddSingleton<Core.GatewaySessionBinder>();
        services.AddSingleton<Core.GatewayFrameSyncSubscriptionManager>();

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

    private sealed class BattleInputSecurityOptionsValidator : IValidateOptions<BattleInputSecurityOptions>
    {
        public ValidateOptionsResult Validate(string? name, BattleInputSecurityOptions options)
        {
            var failures = BattleInputSecurityOptions.GetValidationFailures(options);
            return failures.Count == 0
                ? ValidateOptionsResult.Success
                : ValidateOptionsResult.Fail(failures);
        }
    }
}
