namespace AbilityKit.Orleans.Gateway.HttpApi;

using AbilityKit.Orleans.Contracts.Accounts;
using AbilityKit.Orleans.Contracts.Battle;
using AbilityKit.Orleans.Contracts.Rooms;
using AbilityKit.Orleans.Contracts.Shooter;
using Orleans;

public static class GatewayRoomEndpoints
{
    public static RouteGroupBuilder MapGatewayRoomEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/rooms")
            .WithTags("Rooms");

        group.MapPost("/create", (WebCreateRoomRequest request, IClusterClient client) =>
            GatewayEndpointHelpers.ExecuteRoomOperationAsync(async () =>
            {
                var accountId = await ValidateAccountAsync(client, request.SessionToken);
                if (string.IsNullOrWhiteSpace(accountId))
                {
                    return InvalidSession();
                }

                var directory = client.GetGrain<IRoomDirectoryGrain>(CreateDirectoryKey(request.Region, request.ServerId));
                var response = await directory.CreateRoomAsync(new CreateRoomRequest(
                    accountId,
                    NormalizeRegion(request.Region),
                    NormalizeServerId(request.ServerId),
                    string.IsNullOrWhiteSpace(request.RoomType) ? ShooterServerProtocol.RoomType : request.RoomType,
                    request.Title ?? string.Empty,
                    request.IsPublic,
                    request.MaxPlayers,
                    request.Tags));

                if (request.AutoJoin)
                {
                    var room = client.GetGrain<IRoomGrain>(response.RoomId);
                    await room.JoinMemberAsync(new JoinRoomMemberRequest(accountId));
                    await BindCurrentRoomAsync(client, accountId, response.RoomId);
                }

                return Results.Ok(response);
            }))
        .WithName("Gateway.CreateRoom")
        .Accepts<WebCreateRoomRequest>("application/json")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status500InternalServerError);

        group.MapPost("/list", (WebListRoomsRequest request, IClusterClient client) =>
            GatewayEndpointHelpers.ExecuteRoomOperationAsync(async () =>
            {
                var accountId = await ValidateAccountAsync(client, request.SessionToken);
                if (string.IsNullOrWhiteSpace(accountId))
                {
                    return InvalidSession();
                }

                var directory = client.GetGrain<IRoomDirectoryGrain>(CreateDirectoryKey(request.Region, request.ServerId));
                var response = await directory.ListRoomsAsync(new ListRoomsRequest(
                    accountId,
                    NormalizeRegion(request.Region),
                    NormalizeServerId(request.ServerId),
                    request.Offset,
                    request.Limit,
                    request.RoomType));
                return Results.Ok(response);
            }))
        .WithName("Gateway.ListRooms")
        .Accepts<WebListRoomsRequest>("application/json")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status500InternalServerError);

        group.MapPost("/join", (WebRoomRequest request, IClusterClient client) =>
            GatewayEndpointHelpers.ExecuteRoomOperationAsync(async () =>
            {
                var accountId = await ValidateAccountAsync(client, request.SessionToken);
                if (string.IsNullOrWhiteSpace(accountId))
                {
                    return InvalidSession();
                }

                if (string.IsNullOrWhiteSpace(request.RoomId))
                {
                    return BadRequest("RoomId is required.");
                }

                var room = client.GetGrain<IRoomGrain>(request.RoomId);
                var response = await room.JoinMemberAsync(new JoinRoomMemberRequest(accountId));
                await BindCurrentRoomAsync(client, accountId, request.RoomId);
                return Results.Ok(response);
            }))
        .WithName("Gateway.JoinRoom")
        .Accepts<WebRoomRequest>("application/json")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status500InternalServerError);

        group.MapPost("/snapshot", (WebRoomRequest request, IClusterClient client) =>
            GatewayEndpointHelpers.ExecuteRoomOperationAsync(async () =>
            {
                var accountId = await ValidateAccountAsync(client, request.SessionToken);
                if (string.IsNullOrWhiteSpace(accountId))
                {
                    return InvalidSession();
                }

                if (string.IsNullOrWhiteSpace(request.RoomId))
                {
                    return BadRequest("RoomId is required.");
                }

                var room = client.GetGrain<IRoomGrain>(request.RoomId);
                var snapshot = await room.GetSnapshotAsync();
                return Results.Ok(snapshot);
            }))
        .WithName("Gateway.GetRoomSnapshot")
        .Accepts<WebRoomRequest>("application/json")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status500InternalServerError);

        group.MapPost("/restore-current", (SessionTokenRequest request, IClusterClient client) =>
            GatewayEndpointHelpers.ExecuteRoomOperationAsync(async () =>
            {
                var accountId = await ValidateAccountAsync(client, request.SessionToken);
                if (string.IsNullOrWhiteSpace(accountId))
                {
                    return InvalidSession();
                }

                var mapping = client.GetGrain<IRoomIdMappingGrain>("global");
                var roomId = await mapping.TryGetAccountRoomAsync(accountId);
                if (string.IsNullOrWhiteSpace(roomId))
                {
                    return GatewayEndpointHelpers.ToRoomHttpError(
                        RoomGatewayErrorCodes.AccountNotInRoom,
                        "No current room for account.",
                        StatusCodes.Status404NotFound,
                        GatewayStatusCode.NotFound);
                }

                var room = client.GetGrain<IRoomGrain>(roomId);
                var restore = await room.RestoreAsync(accountId);
                return Results.Ok(restore);
            }))
        .WithName("Gateway.RestoreCurrentRoom")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status500InternalServerError);

        group.MapPost("/leave", (WebRoomRequest request, IClusterClient client) =>
            GatewayEndpointHelpers.ExecuteRoomOperationAsync(async () =>
            {
                var accountId = await ValidateAccountAsync(client, request.SessionToken);
                if (string.IsNullOrWhiteSpace(accountId))
                {
                    return InvalidSession();
                }

                if (string.IsNullOrWhiteSpace(request.RoomId))
                {
                    return BadRequest("RoomId is required.");
                }

                var room = client.GetGrain<IRoomGrain>(request.RoomId);
                await room.LeaveAsync(accountId);
                var mapping = client.GetGrain<IRoomIdMappingGrain>("global");
                await mapping.ClearAccountRoomAsync(accountId, request.RoomId);
                return Results.Ok(new { Success = true });
            }))
        .WithName("Gateway.LeaveRoom")
        .Accepts<WebRoomRequest>("application/json")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status500InternalServerError);

        group.MapPost("/mark-offline", (WebRoomRequest request, IClusterClient client) =>
            GatewayEndpointHelpers.ExecuteRoomOperationAsync(async () =>
            {
                var accountId = await ValidateAccountAsync(client, request.SessionToken);
                if (string.IsNullOrWhiteSpace(accountId))
                {
                    return InvalidSession();
                }

                if (string.IsNullOrWhiteSpace(request.RoomId))
                {
                    return BadRequest("RoomId is required.");
                }

                var room = client.GetGrain<IRoomGrain>(request.RoomId);
                await room.MarkOfflineAsync(accountId);
                var state = await room.GetRuntimeStateAsync();
                return Results.Ok(state);
            }))
        .WithName("Gateway.MarkRoomMemberOffline")
        .Accepts<WebRoomRequest>("application/json")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status500InternalServerError);

        group.MapPost("/close", (WebRoomRequest request, IClusterClient client) =>
            GatewayEndpointHelpers.ExecuteRoomOperationAsync(async () =>
            {
                var accountId = await ValidateAccountAsync(client, request.SessionToken);
                if (string.IsNullOrWhiteSpace(accountId))
                {
                    return InvalidSession();
                }

                if (string.IsNullOrWhiteSpace(request.RoomId))
                {
                    return BadRequest("RoomId is required.");
                }

                var room = client.GetGrain<IRoomGrain>(request.RoomId);
                await room.CloseAsync(accountId);
                var mapping = client.GetGrain<IRoomIdMappingGrain>("global");
                await mapping.ClearAccountRoomAsync(accountId, request.RoomId);
                return Results.Ok(new { Success = true });
            }))
        .WithName("Gateway.CloseRoom")
        .Accepts<WebRoomRequest>("application/json")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status500InternalServerError);

        group.MapPost("/runtime-state", (WebRoomRequest request, IClusterClient client) =>
            GatewayEndpointHelpers.ExecuteRoomOperationAsync(async () =>
            {
                var accountId = await ValidateAccountAsync(client, request.SessionToken);
                if (string.IsNullOrWhiteSpace(accountId))
                {
                    return InvalidSession();
                }

                if (string.IsNullOrWhiteSpace(request.RoomId))
                {
                    return BadRequest("RoomId is required.");
                }

                var room = client.GetGrain<IRoomGrain>(request.RoomId);
                var state = await room.GetRuntimeStateAsync();
                return Results.Ok(state);
            }))
        .WithName("Gateway.GetRoomRuntimeState")
        .Accepts<WebRoomRequest>("application/json")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status500InternalServerError);

        group.MapGet("/runtime-state/{roomId}", (string roomId, IClusterClient client) =>
            GatewayEndpointHelpers.ExecuteRoomOperationAsync(async () =>
            {
                var room = client.GetGrain<IRoomGrain>(roomId);
                var state = await room.GetRuntimeStateAsync();
                return Results.Ok(state);
            }))
        .WithName("Gateway.GetRoomRuntimeStateByRoute")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status500InternalServerError);

        group.MapPost("/ready", (WebRoomReadyRequest request, IClusterClient client) =>
            GatewayEndpointHelpers.ExecuteRoomOperationAsync(async () =>
            {
                var accountId = await ValidateAccountAsync(client, request.SessionToken);
                if (string.IsNullOrWhiteSpace(accountId))
                {
                    return InvalidSession();
                }

                if (string.IsNullOrWhiteSpace(request.RoomId))
                {
                    return BadRequest("RoomId is required.");
                }

                var room = client.GetGrain<IRoomGrain>(request.RoomId);
                await room.SetReadyAsync(new RoomReadyRequest(accountId, request.Ready));
                var snapshot = await room.GetSnapshotAsync();
                return Results.Ok(snapshot);
            }))
        .WithName("Gateway.SetRoomReady")
        .Accepts<WebRoomReadyRequest>("application/json")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status500InternalServerError);

        group.MapPost("/pick-hero", (WebRoomPickHeroRequest request, IClusterClient client) =>
            GatewayEndpointHelpers.ExecuteRoomOperationAsync(async () =>
            {
                var accountId = await ValidateAccountAsync(client, request.SessionToken);
                if (string.IsNullOrWhiteSpace(accountId))
                {
                    return InvalidSession();
                }

                if (string.IsNullOrWhiteSpace(request.RoomId))
                {
                    return BadRequest("RoomId is required.");
                }

                var room = client.GetGrain<IRoomGrain>(request.RoomId);
                await room.SubmitGameplayCommandAsync(RoomGameplayCommandRequest.CreateMobaLoadout(
                    accountId,
                    request.HeroId,
                    request.TeamId,
                    request.SpawnPointId,
                    request.Level,
                    request.AttributeTemplateId,
                    request.BasicAttackSkillId,
                    request.SkillIds));
                await room.SetReadyAsync(new RoomReadyRequest(accountId, true));
                var snapshot = await room.GetSnapshotAsync();
                return Results.Ok(snapshot);
            }))
        .WithName("Gateway.PickRoomHero")
        .Accepts<WebRoomPickHeroRequest>("application/json")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status500InternalServerError);

        group.MapPost("/start-battle", (WebStartRoomBattleRequest request, IClusterClient client) =>
            GatewayEndpointHelpers.ExecuteRoomOperationAsync(async () =>
            {
                var accountId = await ValidateAccountAsync(client, request.SessionToken);
                if (string.IsNullOrWhiteSpace(accountId))
                {
                    return InvalidSession();
                }

                if (string.IsNullOrWhiteSpace(request.RoomId))
                {
                    return BadRequest("RoomId is required.");
                }

                var room = client.GetGrain<IRoomGrain>(request.RoomId);
                var response = await room.StartBattleAsync(new StartRoomBattleRequest(
                    accountId,
                    request.GameplayId,
                    request.RuleSetId,
                    request.ConfigVersion,
                    request.ProtocolVersion,
                    request.WorldType,
                    request.ClientId,
                    new BattleSyncStartOptions(
                        request.SyncTemplateId,
                        request.SyncModel ?? 0,
                        request.NetworkEnvironmentId,
                        request.CarrierName,
                        request.EnableAuthoritativeWorld,
                        request.InterpolationEnabled,
                        request.InputDelayFrames)));
                return Results.Ok(response);
            }))
        .WithName("Gateway.StartRoomBattle")
        .Accepts<WebStartRoomBattleRequest>("application/json")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status500InternalServerError);

        return group;
    }

    private static async Task<string?> ValidateAccountAsync(IClusterClient client, string? sessionToken)
    {
        if (string.IsNullOrWhiteSpace(sessionToken))
        {
            return null;
        }

        var session = client.GetGrain<ISessionGrain>("global");
        var result = await session.ValidateAsync(new ValidateSessionRequest(sessionToken));
        return result.IsValid ? result.AccountId : null;
    }

    private static Task BindCurrentRoomAsync(IClusterClient client, string accountId, string roomId)
    {
        var mapping = client.GetGrain<IRoomIdMappingGrain>("global");
        return mapping.BindAccountRoomAsync(accountId, roomId);
    }

    private static string CreateDirectoryKey(string? region, string? serverId)
    {
        return $"{NormalizeRegion(region)}:{NormalizeServerId(serverId)}";
    }

    private static string NormalizeRegion(string? region)
    {
        return string.IsNullOrWhiteSpace(region) ? "dev" : region;
    }

    private static string NormalizeServerId(string? serverId)
    {
        return string.IsNullOrWhiteSpace(serverId) ? ShooterServerProtocol.DefaultServerId : serverId;
    }

    private static IResult InvalidSession()
    {
        return GatewayEndpointHelpers.ToRoomHttpError(
            RoomGatewayErrorCodes.BadRequest,
            "Invalid session",
            StatusCodes.Status400BadRequest,
            GatewayStatusCode.BadRequest);
    }

    private static IResult BadRequest(string message)
    {
        return GatewayEndpointHelpers.ToRoomHttpError(
            RoomGatewayErrorCodes.BadRequest,
            message,
            StatusCodes.Status400BadRequest,
            GatewayStatusCode.BadRequest);
    }
}
