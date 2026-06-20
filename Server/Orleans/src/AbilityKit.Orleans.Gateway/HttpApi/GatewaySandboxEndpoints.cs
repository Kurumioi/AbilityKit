namespace AbilityKit.Orleans.Gateway.HttpApi;

using AbilityKit.Orleans.Contracts.Automation;
using Orleans;

public static class GatewaySandboxEndpoints
{
    public static RouteGroupBuilder MapGatewaySandboxEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api")
            .WithTags("Gameplay", "Sandbox");

        group.MapGet("/gameplays", () => Results.Ok(Gameplays))
            .WithName("Gateway.ListGameplays")
            .Produces(StatusCodes.Status200OK);

        group.MapPost("/shooter-sandbox/start", async (StartShooterSandboxHttpRequest wire, IClusterClient client) =>
        {
            var sandbox = client.GetGrain<IShooterSandboxGrain>(string.IsNullOrWhiteSpace(wire?.SandboxId) ? "default" : wire.SandboxId);
            var resp = await sandbox.StartAsync(new StartShooterSandboxRequest(
                wire?.Region ?? "dev",
                wire?.ServerId ?? "default",
                wire?.BotCount ?? 4,
                wire?.MaxPlayers ?? 32,
                wire?.TickRate ?? 30,
                wire?.Title,
                wire?.Tags == null ? null : new Dictionary<string, string>(wire.Tags)));
            return Results.Ok(resp);
        })
        .WithName("Gateway.StartShooterSandbox")
        .Accepts<StartShooterSandboxHttpRequest>("application/json")
        .Produces(StatusCodes.Status200OK);

        group.MapGet("/shooter-sandbox/{sandboxId?}", async (string? sandboxId, IClusterClient client) =>
        {
            var sandbox = client.GetGrain<IShooterSandboxGrain>(string.IsNullOrWhiteSpace(sandboxId) ? "default" : sandboxId);
            var resp = await sandbox.GetStateAsync();
            return Results.Ok(resp);
        })
        .WithName("Gateway.GetShooterSandboxState")
        .Produces(StatusCodes.Status200OK);

        group.MapPost("/shooter-sandbox/stop", async (ShooterSandboxControlHttpRequest wire, IClusterClient client) =>
        {
            var sandbox = client.GetGrain<IShooterSandboxGrain>(string.IsNullOrWhiteSpace(wire?.SandboxId) ? "default" : wire.SandboxId);
            await sandbox.StopAsync();
            return Results.Ok(new { Success = true });
        })
        .WithName("Gateway.StopShooterSandbox")
        .Accepts<ShooterSandboxControlHttpRequest>("application/json")
        .Produces(StatusCodes.Status200OK);

        return group;
    }
}
