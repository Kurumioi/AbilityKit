namespace AbilityKit.Orleans.Gateway.HttpApi;

using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using AbilityKit.Orleans.Hosting;

internal static class AbilityKitGatewayHealthEndpoints
{
    public static IEndpointRouteBuilder MapAbilityKitGatewayHealthEndpoints(
        this IEndpointRouteBuilder app,
        AbilityKitGatewayOptions gatewayOptions)
    {
        app.MapGet("/health/live", () => Results.Ok(new { status = "Alive" }))
            .WithName("Gateway.HealthLive")
            .Produces(StatusCodes.Status200OK);

        app.MapGet("/health/ready", (HttpContext httpContext) =>
        {
            var options = httpContext.RequestServices.GetRequiredService<IOptions<AbilityKitGatewayOptions>>().Value;
            var deployment = httpContext.RequestServices.GetRequiredService<IOptions<AbilityKitDeploymentOptions>>().Value;
            var environment = httpContext.RequestServices.GetRequiredService<IHostEnvironment>();
            var service = $"{options.Http.Scheme}://{options.Http.Host}:{options.Http.Port}";
            var deploymentDiagnostics = new AbilityKitGatewayDeploymentDiagnostics(
                Role: deployment.Role,
                Region: options.Http.Host,
                ServerId: options.Http.Url,
                Cluster: null,
                NodeName: Environment.MachineName,
                SiloId: null,
                Runtime: new AbilityKitGatewayRuntimeDiagnostics(
                    RootPath: AppContext.BaseDirectory,
                    EnvironmentName: environment.EnvironmentName,
                    IsDevelopment: environment.IsDevelopment(),
                    ReleaseVersion: typeof(AbilityKitGatewayHealthEndpoints).Assembly.GetName().Version?.ToString()),
                Gameplay: null);

            var report = AbilityKitGatewayHealthReport.Ready(
                service,
                deploymentDiagnostics,
                diagnostics: Array.Empty<string>(),
                warnings: Array.Empty<string>());

            return Results.Ok(report);
        })
        .WithName("Gateway.HealthReady")
        .Produces(StatusCodes.Status200OK);

        return app;
    }
}
