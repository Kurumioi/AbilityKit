using AbilityKit.Orleans.Contracts.Accounts;
using AbilityKit.Orleans.Contracts.Battle;
using AbilityKit.Orleans.Contracts.Rooms;
using Orleans;

namespace AbilityKit.Orleans.Gateway.HttpApi;

public static class GatewayHttpApi
{
    public static void MapGatewayHttpApi(this WebApplication app)
    {
        app.MapPost("/api/guest/login", async (IClusterClient client) =>
        {
            var session = client.GetGrain<ISessionGrain>("global");
            var resp = await session.CreateGuestAsync();
            return Results.Ok(resp);
        });

        app.MapPost("/api/rooms/create", async (CreateRoomHttpRequest wire, IClusterClient client) =>
        {
            if (wire is null || string.IsNullOrWhiteSpace(wire.SessionToken))
            {
                return Results.BadRequest("SessionToken is required");
            }

            var session = client.GetGrain<ISessionGrain>("global");
            var v = await session.ValidateAsync(new ValidateSessionRequest(wire.SessionToken));
            if (!v.IsValid || string.IsNullOrWhiteSpace(v.AccountId))
            {
                return Results.BadRequest("Invalid session");
            }

            if (string.IsNullOrWhiteSpace(wire.Region) || string.IsNullOrWhiteSpace(wire.ServerId) || string.IsNullOrWhiteSpace(wire.RoomType))
            {
                return Results.BadRequest("Region/ServerId/RoomType are required");
            }

            var directoryKey = $"{wire.Region}:{wire.ServerId}";
            var directory = client.GetGrain<IRoomDirectoryGrain>(directoryKey);

            var req = new CreateRoomRequest(
                v.AccountId,
                wire.Region,
                wire.ServerId,
                wire.RoomType,
                wire.Title ?? string.Empty,
                wire.IsPublic,
                wire.MaxPlayers,
                wire.Tags == null ? null : new Dictionary<string, string>(wire.Tags));

            var resp = await directory.CreateRoomAsync(req);
            return Results.Ok(resp);
        });

        app.MapPost("/api/rooms/join", async (JoinRoomHttpRequest wire, IClusterClient client) =>
        {
            if (wire is null || string.IsNullOrWhiteSpace(wire.SessionToken) || string.IsNullOrWhiteSpace(wire.RoomId))
            {
                return Results.BadRequest("SessionToken and RoomId are required");
            }

            var accountId = await ValidateAccountAsync(client, wire.SessionToken);
            if (string.IsNullOrWhiteSpace(accountId))
            {
                return Results.BadRequest("Invalid session");
            }

            var room = client.GetGrain<IRoomGrain>(wire.RoomId);
            await room.JoinAsync(accountId);
            var snapshot = await room.GetSnapshotAsync();
            return Results.Ok(snapshot);
        });

        app.MapPost("/api/rooms/runtime-state", async (RoomRuntimeStateHttpRequest wire, IClusterClient client) =>
        {
            if (wire is null || string.IsNullOrWhiteSpace(wire.SessionToken) || string.IsNullOrWhiteSpace(wire.RoomId))
            {
                return Results.BadRequest("SessionToken and RoomId are required");
            }

            var accountId = await ValidateAccountAsync(client, wire.SessionToken);
            if (string.IsNullOrWhiteSpace(accountId))
            {
                return Results.BadRequest("Invalid session");
            }

            var room = client.GetGrain<IRoomGrain>(wire.RoomId);
            var runtimeState = await room.GetRuntimeStateAsync();
            if (!string.Equals(runtimeState.RoomId, wire.RoomId, StringComparison.Ordinal))
            {
                return Results.BadRequest("Room mismatch");
            }

            return Results.Ok(runtimeState);
        });

        app.MapPost("/api/rooms/ready", async (RoomReadyHttpRequest wire, IClusterClient client) =>
        {
            if (wire is null || string.IsNullOrWhiteSpace(wire.SessionToken) || string.IsNullOrWhiteSpace(wire.RoomId))
            {
                return Results.BadRequest("SessionToken and RoomId are required");
            }

            var accountId = await ValidateAccountAsync(client, wire.SessionToken);
            if (string.IsNullOrWhiteSpace(accountId))
            {
                return Results.BadRequest("Invalid session");
            }

            var room = client.GetGrain<IRoomGrain>(wire.RoomId);
            await room.SetReadyAsync(new RoomReadyRequest(accountId, wire.Ready));
            var snapshot = await room.GetSnapshotAsync();
            return Results.Ok(snapshot);
        });

        app.MapPost("/api/rooms/pick-hero", async (RoomPickHeroHttpRequest wire, IClusterClient client) =>
        {
            if (wire is null || string.IsNullOrWhiteSpace(wire.SessionToken) || string.IsNullOrWhiteSpace(wire.RoomId))
            {
                return Results.BadRequest("SessionToken and RoomId are required");
            }

            var accountId = await ValidateAccountAsync(client, wire.SessionToken);
            if (string.IsNullOrWhiteSpace(accountId))
            {
                return Results.BadRequest("Invalid session");
            }

            var room = client.GetGrain<IRoomGrain>(wire.RoomId);
            await room.PickHeroAsync(new RoomPickHeroRequest(
                accountId,
                wire.HeroId,
                wire.TeamId,
                wire.SpawnPointId,
                wire.Level,
                wire.AttributeTemplateId,
                wire.BasicAttackSkillId,
                wire.SkillIds == null ? null : new List<int>(wire.SkillIds)));
            var snapshot = await room.GetSnapshotAsync();
            return Results.Ok(snapshot);
        });

        app.MapPost("/api/rooms/start-battle", async (StartRoomBattleHttpRequest wire, IClusterClient client) =>
        {
            if (wire is null || string.IsNullOrWhiteSpace(wire.SessionToken) || string.IsNullOrWhiteSpace(wire.RoomId))
            {
                return Results.BadRequest("SessionToken and RoomId are required");
            }

            var accountId = await ValidateAccountAsync(client, wire.SessionToken);
            if (string.IsNullOrWhiteSpace(accountId))
            {
                return Results.BadRequest("Invalid session");
            }

            var room = client.GetGrain<IRoomGrain>(wire.RoomId);
            var syncOptions = CreateSyncOptions(
                wire.SyncTemplateId,
                wire.SyncModel,
                wire.NetworkEnvironmentId,
                wire.CarrierName,
                wire.EnableAuthoritativeWorld,
                wire.InterpolationEnabled,
                wire.InputDelayFrames);
            var resp = await room.StartBattleAsync(new StartRoomBattleRequest(
                accountId,
                wire.GameplayId,
                wire.RuleSetId,
                wire.ConfigVersion,
                wire.ProtocolVersion,
                wire.WorldType,
                wire.ClientId,
                syncOptions));
            return Results.Ok(resp);
        });
    }

    private static async Task<string?> ValidateAccountAsync(IClusterClient client, string sessionToken)
    {
        var session = client.GetGrain<ISessionGrain>("global");
        var v = await session.ValidateAsync(new ValidateSessionRequest(sessionToken));
        return v.IsValid && !string.IsNullOrWhiteSpace(v.AccountId) ? v.AccountId : null;
    }

    private static BattleSyncStartOptions? CreateSyncOptions(
        string? syncTemplateId,
        int? syncModel,
        string? networkEnvironmentId,
        string? carrierName,
        bool? enableAuthoritativeWorld,
        bool? interpolationEnabled,
        int? inputDelayFrames)
    {
        if (string.IsNullOrWhiteSpace(syncTemplateId)
            && syncModel is null
            && string.IsNullOrWhiteSpace(networkEnvironmentId)
            && string.IsNullOrWhiteSpace(carrierName)
            && enableAuthoritativeWorld is null
            && interpolationEnabled is null
            && inputDelayFrames is null)
        {
            return null;
        }

        return new BattleSyncStartOptions(
            syncTemplateId,
            syncModel ?? 0,
            networkEnvironmentId,
            carrierName,
            enableAuthoritativeWorld ?? true,
            interpolationEnabled ?? false,
            inputDelayFrames ?? 0);
    }

    public sealed record CreateRoomHttpRequest(
        string SessionToken,
        string Region,
        string ServerId,
        string RoomType,
        string? Title,
        bool IsPublic,
        int MaxPlayers,
        IReadOnlyDictionary<string, string>? Tags);

    public sealed record JoinRoomHttpRequest(
        string SessionToken,
        string RoomId);

    public sealed record RoomRuntimeStateHttpRequest(
        string SessionToken,
        string RoomId);

    public sealed record RoomReadyHttpRequest(
        string SessionToken,
        string RoomId,
        bool Ready);

    public sealed record RoomPickHeroHttpRequest(
        string SessionToken,
        string RoomId,
        int HeroId,
        int TeamId,
        int SpawnPointId,
        int Level,
        int AttributeTemplateId,
        int BasicAttackSkillId,
        IReadOnlyList<int>? SkillIds);

    public sealed record StartRoomBattleHttpRequest(
        string SessionToken,
        string RoomId,
        int GameplayId,
        int RuleSetId,
        int ConfigVersion,
        int ProtocolVersion,
        string? WorldType,
        string? ClientId,
        string? SyncTemplateId,
        int? SyncModel,
        string? NetworkEnvironmentId,
        string? CarrierName,
        bool? EnableAuthoritativeWorld,
        bool? InterpolationEnabled,
        int? InputDelayFrames);
}
