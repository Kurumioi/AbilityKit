namespace AbilityKit.Orleans.Gateway.HttpApi;

using AbilityKit.Orleans.Hosting;

internal static class AbilityKitGatewayHealthEndpoints
{
    public static IEndpointRouteBuilder MapAbilityKitGatewayHealthEndpoints(
        this IEndpointRouteBuilder app,
        AbilityKitSiloPlacementOptions deploymentOptions)
    {
        var group = app.MapGroup("/health").WithTags("Health");

        group.MapGet("/live", () => Results.Ok(new
        {
            Status = "Alive",
            Service = "AbilityKit.Orleans.Gateway",
            CheckedAtUtc = DateTimeOffset.UtcNow,
            Deployment = new
            {
                deploymentOptions.Role,
                deploymentOptions.IsGateway,
                deploymentOptions.IsExclusive,
                deploymentOptions.LogicalGroups,
                deploymentOptions.PreferredAffinity
            }
        }))
        .WithName("Gateway.HealthLive")
        .Produces(StatusCodes.Status200OK);

        group.MapGet("", () => Results.Ok(new
        {
            Status = "Ready",
            Service = "AbilityKit.Orleans.Gateway",
            CheckedAtUtc = DateTimeOffset.UtcNow,
            Deployment = new
            {
                deploymentOptions.Role,
                deploymentOptions.IsGateway,
                deploymentOptions.IsExclusive,
                deploymentOptions.LogicalGroups,
                deploymentOptions.PreferredAffinity
            }
        }))
        .WithName("Gateway.HealthReady")
        .Produces(StatusCodes.Status200OK);

        return app;
    }
}
