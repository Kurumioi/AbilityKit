namespace AbilityKit.Orleans.Gateway.HttpApi;

using AbilityKit.Orleans.Contracts.Battle;
using AbilityKit.Orleans.Contracts.Rooms;
using AbilityKit.Orleans.Gateway.Handlers;
using Orleans;

public static class GatewayRoomEndpoints
{
    public static RouteGroupBuilder MapGatewayRoomEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/rooms")
            .WithTags("Rooms");

        group.MapPost("/create", async (CreateRoomHttpRequest wire, IClusterClient client) =>
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

            if (string.IsNullOrWhiteSpace(wire.Region) || string.IsNullOrWhiteSpace(wire.ServerId))
            {
                return Results.BadRequest("Region and ServerId are required");
            }

            try
            {
                var directoryKey = $"{wire.Region}:{wire.ServerId}";
                var directory = client.GetGrain<IRoomDirectoryGrain>(directoryKey);

                var req = new CreateRoomRequest(
                    v.AccountId,
                    wire.Region,
                    wire.ServerId,
                    string.IsNullOrWhiteSpace(wire.RoomType) ? GameplayRoomTypes.Default : wire.RoomType,
                    wire.Title ?? string.Empty,
                    wire.IsPublic,
                    wire.MaxPlayers,
                    wire.Tags == null ? null : new Dictionary<string, string>(wire.Tags));

                var resp = await directory.CreateRoomAsync(req);
                if (!string.IsNullOrWhiteSpace(resp.RoomId))
                {
                    var mapping = client.GetGrain<IRoomIdMappingGrain>("global");
                    await mapping.BindAccountRoomAsync(v.AccountId, resp.RoomId);
                }

                return Results.Ok(resp);
            }
            catch (Exception exception)
            {
                return RoomHttpErrorMapper.ToResult(exception);
            }
        })
        .WithName("Gateway.CreateRoom")
        .Accepts<CreateRoomHttpRequest>("application/json")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status500InternalServerError);

        group.MapPost("/list", async (ListRoomsHttpRequest wire, IClusterClient client) =>
        {
            if (wire is null || string.IsNullOrWhiteSpace(wire.SessionToken) || string.IsNullOrWhiteSpace(wire.Region) || string.IsNullOrWhiteSpace(wire.ServerId))
            {
                return Results.BadRequest("SessionToken, Region and ServerId are required");
            }

            var accountId = await ValidateAccountAsync(client, wire.SessionToken);
            if (string.IsNullOrWhiteSpace(accountId))
            {
                return Results.BadRequest("Invalid session");
            }

            try
            {
                var directoryKey = $"{wire.Region}:{wire.ServerId}";
                var directory = client.GetGrain<IRoomDirectoryGrain>(directoryKey);
                var resp = await directory.ListRoomsAsync(new ListRoomsRequest(
                    accountId,
                    wire.Region,
                    wire.ServerId,
                    wire.Offset,
                    wire.Limit,
                    wire.RoomType));
                return Results.Ok(resp);
            }
            catch (Exception exception)
            {
                return RoomHttpErrorMapper.ToResult(exception);
            }
        })
        .WithName("Gateway.ListRooms")
        .Accepts<ListRoomsHttpRequest>("application/json")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status500InternalServerError);

        group.MapPost("/join", async (JoinRoomHttpRequest wire, IClusterClient client) =>
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

            try
            {
                var room = client.GetGrain<IRoomGrain>(wire.RoomId);
                await room.JoinAsync(accountId);
                var mapping = client.GetGrain<IRoomIdMappingGrain>("global");
                await mapping.BindAccountRoomAsync(accountId, wire.RoomId);

                var snapshot = await room.GetSnapshotAsync();
                return Results.Ok(snapshot);
            }
            catch (Exception exception)
            {
                return RoomHttpErrorMapper.ToResult(exception);
            }
        })
        .WithName("Gateway.JoinRoom")
        .Accepts<JoinRoomHttpRequest>("application/json")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status500InternalServerError);

        group.MapPost("/snapshot", async (RoomSnapshotHttpRequest wire, IClusterClient client) =>
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

            try
            {
                var room = client.GetGrain<IRoomGrain>(wire.RoomId);
                var snapshot = await room.GetSnapshotAsync();
                return Results.Ok(snapshot);
            }
            catch (Exception exception)
            {
                return RoomHttpErrorMapper.ToResult(exception);
            }
        })
        .WithName("Gateway.GetRoomSnapshot")
        .Accepts<RoomSnapshotHttpRequest>("application/json")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status500InternalServerError);

        group.MapPost("/restore-current", async (SessionTokenHttpRequest wire, IClusterClient client) =>
        {
            if (wire is null || string.IsNullOrWhiteSpace(wire.SessionToken))
            {
                return Results.BadRequest("SessionToken is required");
            }

            var accountId = await ValidateAccountAsync(client, wire.SessionToken);
            if (string.IsNullOrWhiteSpace(accountId))
            {
                return Results.BadRequest("Invalid session");
            }

            var mapping = client.GetGrain<IRoomIdMappingGrain>("global");
            var roomId = await mapping.TryGetAccountRoomAsync(accountId);
            if (string.IsNullOrWhiteSpace(roomId))
            {
                return Results.NotFound(new { Success = false, Reason = "NoCurrentRoom" });
            }

            try
            {
                var room = client.GetGrain<IRoomGrain>(roomId);
                var snapshot = await room.GetSnapshotAsync();
                return Results.Ok(snapshot);
            }
            catch (Exception exception)
            {
                return RoomHttpErrorMapper.ToResult(exception);
            }
        })
        .WithName("Gateway.RestoreCurrentRoom")
        .Accepts<SessionTokenHttpRequest>("application/json")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status500InternalServerError);

        group.MapPost("/leave", async (RoomLeaveHttpRequest wire, IClusterClient client) =>
        {
            if (wire is null || string.IsNullOrWhiteSpace(wire.SessionToken))
            {
                return Results.BadRequest("SessionToken is required");
            }

            var accountId = await ValidateAccountAsync(client, wire.SessionToken);
            if (string.IsNullOrWhiteSpace(accountId))
            {
                return Results.BadRequest("Invalid session");
            }

            try
            {
                var mapping = client.GetGrain<IRoomIdMappingGrain>("global");
                var roomId = await mapping.TryGetAccountRoomAsync(accountId);
                if (!string.IsNullOrWhiteSpace(roomId))
                {
                    var room = client.GetGrain<IRoomGrain>(roomId);
                    await room.LeaveAsync(accountId);
                    await mapping.ClearAccountRoomAsync(accountId, roomId);
                }

                return Results.Ok(new { Success = true });
            }
            catch (Exception exception)
            {
                return RoomHttpErrorMapper.ToResult(exception);
            }
        })
        .WithName("Gateway.LeaveRoom")
        .Accepts<RoomLeaveHttpRequest>("application/json")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status500InternalServerError);

        group.MapPost("/runtime-state", async (RoomRuntimeStateHttpRequest wire, IClusterClient client) =>
        {
            if (wire is null || string.IsNullOrWhiteSpace(wire.RoomId))
            {
                return Results.BadRequest("RoomId is required");
            }

            try
            {
                var room = client.GetGrain<IRoomGrain>(wire.RoomId);
                var state = await room.GetRuntimeStateAsync();
                return Results.Ok(state);
            }
            catch (Exception exception)
            {
                return RoomHttpErrorMapper.ToResult(exception);
            }
        })
        .WithName("Gateway.GetRoomRuntimeState")
        .Accepts<RoomRuntimeStateHttpRequest>("application/json")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status500InternalServerError);

        group.MapPost("/ready", async (RoomReadyHttpRequest wire, IClusterClient client) =>
        {
            if (wire is null || string.IsNullOrWhiteSpace(wire.SessionToken))
            {
                return Results.BadRequest("SessionToken is required");
            }

            var accountId = await ValidateAccountAsync(client, wire.SessionToken);
            if (string.IsNullOrWhiteSpace(accountId))
            {
                return Results.BadRequest("Invalid session");
            }

            try
            {
                var room = client.GetGrain<IRoomGrain>(wire.RoomId);
                await room.SetReadyAsync(accountId, wire.Ready);
                var snapshot = await room.GetSnapshotAsync();
                return Results.Ok(snapshot);
            }
            catch (Exception exception)
            {
                return RoomHttpErrorMapper.ToResult(exception);
            }
        })
        .WithName("Gateway.SetRoomReady")
        .Accepts<RoomReadyHttpRequest>("application/json")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status500InternalServerError);

        group.MapPost("/pick-hero", async (RoomPickHeroHttpRequest wire, IClusterClient client) =>
        {
            if (wire is null || string.IsNullOrWhiteSpace(wire.SessionToken))
            {
                return Results.BadRequest("SessionToken is required");
            }

            var accountId = await ValidateAccountAsync(client, wire.SessionToken);
            if (string.IsNullOrWhiteSpace(accountId))
            {
                return Results.BadRequest("Invalid session");
            }

            try
            {
                var room = client.GetGrain<IRoomGrain>(wire.RoomId);
                await room.PickHeroAsync(accountId, wire.HeroId);
                var snapshot = await room.GetSnapshotAsync();
                return Results.Ok(snapshot);
            }
            catch (Exception exception)
            {
                return RoomHttpErrorMapper.ToResult(exception);
            }
        })
        .WithName("Gateway.PickRoomHero")
        .Accepts<RoomPickHeroHttpRequest>("application/json")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status500InternalServerError);

        group.MapPost("/start-battle", async (StartRoomBattleHttpRequest wire, IClusterClient client) =>
        {
            if (wire is null || string.IsNullOrWhiteSpace(wire.SessionToken))
            {
                return Results.BadRequest("SessionToken is required");
            }

            var accountId = await ValidateAccountAsync(client, wire.SessionToken);
            if (string.IsNullOrWhiteSpace(accountId))
            {
                return Results.BadRequest("Invalid session");
            }

            try
            {
                var room = client.GetGrain<IRoomGrain>(wire.RoomId);
                var result = await room.StartBattleAsync(accountId, wire.BattleId);
                return Results.Ok(result);
            }
            catch (Exception exception)
            {
                return RoomHttpErrorMapper.ToResult(exception);
            }
        })
        .WithName("Gateway.StartRoomBattle")
        .Accepts<StartRoomBattleHttpRequest>("application/json")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status500InternalServerError);

        return group;
    }
}
