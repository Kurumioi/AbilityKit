using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using AbilityKit.Orleans.Contracts.Automation;
using AbilityKit.Orleans.Contracts.Shooter;
using AbilityKit.Orleans.Hosting;

namespace AbilityKit.Orleans.Gateway.HttpApi;

internal static class GatewaySandboxEndpoints
{
    private static readonly string[] Gameplays =
    {
        "moba",
        ShooterServerProtocol.RoomType
    };

    public static IEndpointRouteBuilder MapGatewaySandboxEndpoints(
        this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/sandbox");

        group.MapGet("/gameplays", () => Results.Ok(Gameplays))
            .WithName("Gateway.ListGameplays")
            .Produces(StatusCodes.Status200OK);

        group.MapPost("/shooter-sandbox/start", async (StartShooterSandboxRequest wire, IClusterClient client) =>
        {
            var sandbox = client.GetGrain<IShooterSandboxGrain>(string.IsNullOrWhiteSpace(wire.ServerId) ? ShooterServerProtocol.DefaultSandboxId : wire.ServerId);
            var response = await sandbox.StartAsync(wire);
            return Results.Ok(response);
        })
        .WithName("Gateway.StartShooterSandbox")
        .Accepts<StartShooterSandboxRequest>("application/json")
        .Produces(StatusCodes.Status200OK);

        group.MapGet("/shooter-sandbox/state", async (string? serverId, IClusterClient client) =>
        {
            var sandbox = client.GetGrain<IShooterSandboxGrain>(string.IsNullOrWhiteSpace(serverId) ? ShooterServerProtocol.DefaultSandboxId : serverId);
            var response = await sandbox.GetStateAsync();
            return Results.Ok(response);
        })
        .WithName("Gateway.GetShooterSandboxState")
        .Produces(StatusCodes.Status200OK);

        group.MapPost("/shooter-sandbox/stop", async (string? serverId, IClusterClient client) =>
        {
            var sandbox = client.GetGrain<IShooterSandboxGrain>(string.IsNullOrWhiteSpace(serverId) ? ShooterServerProtocol.DefaultSandboxId : serverId);
            await sandbox.StopAsync();
            return Results.Ok();
        })
        .WithName("Gateway.StopShooterSandbox")
        .Produces(StatusCodes.Status200OK);

        return app;
    }
}
