using AbilityKit.Orleans.Contracts.Accounts;
using AbilityKit.Orleans.Contracts.Automation;
using AbilityKit.Orleans.Contracts.Battle;
using AbilityKit.Orleans.Contracts.Rooms;
using AbilityKit.Orleans.Gateway.Handlers;
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

        app.MapPost("/api/accounts/login", async (AccountLoginHttpRequest wire, IClusterClient client) =>
        {
            if (wire is null || string.IsNullOrWhiteSpace(wire.AccountId))
            {
                return Results.BadRequest("AccountId is required");
            }

            var session = client.GetGrain<ISessionGrain>("global");
            try
            {
                var resp = await session.CreateSessionForAccountAsync(new CreateSessionForAccountRequest(
                    wire.AccountId,
                    wire.ExpireSeconds,
                    wire.KickExisting));
                return Results.Ok(resp);
            }
            catch (Exception exception)
            {
                return RoomHttpErrorMapper.ToResult(exception);
            }
        });

        app.MapPost("/api/session/validate", async (SessionTokenHttpRequest wire, IClusterClient client) =>
        {
            if (wire is null || string.IsNullOrWhiteSpace(wire.SessionToken))
            {
                return Results.BadRequest("SessionToken is required");
            }

            var session = client.GetGrain<ISessionGrain>("global");
            var resp = await session.ValidateAsync(new ValidateSessionRequest(wire.SessionToken));
            return Results.Ok(resp);
        });

        app.MapPost("/api/session/renew", async (RenewSessionHttpRequest wire, IClusterClient client) =>
        {
            if (wire is null || string.IsNullOrWhiteSpace(wire.SessionToken))
            {
                return Results.BadRequest("SessionToken is required");
            }

            var session = client.GetGrain<ISessionGrain>("global");
            var resp = await session.RenewAsync(new RenewSessionRequest(
                wire.SessionToken,
                wire.ExtendSeconds,
                wire.RotateToken));
            return Results.Ok(resp);
        });

        app.MapPost("/api/session/logout", async (SessionTokenHttpRequest wire, IClusterClient client) =>
        {
            if (wire is null || string.IsNullOrWhiteSpace(wire.SessionToken))
            {
                return Results.BadRequest("SessionToken is required");
            }

            var accountId = await ValidateAccountAsync(client, wire.SessionToken);
            if (!string.IsNullOrWhiteSpace(accountId))
            {
                await MarkCurrentRoomOfflineAsync(client, accountId);
            }

            var session = client.GetGrain<ISessionGrain>("global");
            var resp = await session.LogoutAsync(new LogoutRequest(wire.SessionToken));
            return Results.Ok(resp);
        });
 
        app.MapGet("/api/gameplays", () => Results.Ok(Gameplays));

        app.MapPost("/api/shooter-sandbox/start", async (StartShooterSandboxHttpRequest wire, IClusterClient client) =>
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
        });

        app.MapGet("/api/shooter-sandbox/{sandboxId?}", async (string? sandboxId, IClusterClient client) =>
        {
            var sandbox = client.GetGrain<IShooterSandboxGrain>(string.IsNullOrWhiteSpace(sandboxId) ? "default" : sandboxId);
            var resp = await sandbox.GetStateAsync();
            return Results.Ok(resp);
        });

        app.MapPost("/api/shooter-sandbox/stop", async (ShooterSandboxControlHttpRequest wire, IClusterClient client) =>
        {
            var sandbox = client.GetGrain<IShooterSandboxGrain>(string.IsNullOrWhiteSpace(wire?.SandboxId) ? "default" : wire.SandboxId);
            await sandbox.StopAsync();
            return Results.Ok(new { Success = true });
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
        });

        app.MapPost("/api/rooms/list", async (ListRoomsHttpRequest wire, IClusterClient client) =>
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
        });

        app.MapPost("/api/rooms/snapshot", async (RoomSnapshotHttpRequest wire, IClusterClient client) =>
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
        });

        app.MapPost("/api/rooms/restore-current", async (SessionTokenHttpRequest wire, IClusterClient client) =>
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
                var resp = await RestoreCurrentRoomAsync(client, accountId);
                return Results.Ok(resp);
            }
            catch (Exception exception)
            {
                return RoomHttpErrorMapper.ToResult(exception);
            }
        });

        app.MapPost("/api/rooms/leave", async (RoomLeaveHttpRequest wire, IClusterClient client) =>
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
                await room.LeaveAsync(accountId);
                return Results.Ok(new RoomLeaveHttpResponse(true, wire.RoomId, accountId));
            }
            catch (Exception exception)
            {
                return RoomHttpErrorMapper.ToResult(exception);
            }
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

            try
            {
                var room = client.GetGrain<IRoomGrain>(wire.RoomId);
                await room.SetReadyAsync(new RoomReadyRequest(accountId, wire.Ready));
                var snapshot = await room.GetSnapshotAsync();
                return Results.Ok(snapshot);
            }
            catch (Exception exception)
            {
                return RoomHttpErrorMapper.ToResult(exception);
            }
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

            try
            {
                var room = client.GetGrain<IRoomGrain>(wire.RoomId);
                await room.SubmitGameplayCommandAsync(RoomGatewayWireMapper.ToGameplayCommand(
                    accountId,
                    wire.HeroId,
                    wire.TeamId,
                    wire.SpawnPointId,
                    wire.Level,
                    wire.AttributeTemplateId,
                    wire.BasicAttackSkillId,
                    wire.SkillIds));
                var snapshot = await room.GetSnapshotAsync();
                return Results.Ok(snapshot);
            }
            catch (Exception exception)
            {
                return RoomHttpErrorMapper.ToResult(exception);
            }
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

            try
            {
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
            }
            catch (Exception exception)
            {
                return RoomHttpErrorMapper.ToResult(exception);
            }
        });
    }

    private static readonly IReadOnlyList<GameplayHttpDescriptor> Gameplays = new[]
    {
        new GameplayHttpDescriptor(
            GameplayRoomTypes.Moba,
            "MOBA Battle",
            10,
            RequiresPlayerLoadout: true,
            DefaultWorldType: GameplayRoomTypes.Moba,
            DefaultTickRate: 30,
            DefaultSyncTemplateId: "frame-sync-authority"),
        new GameplayHttpDescriptor(
            "shooter",
            "Shooter State Sync",
            16,
            RequiresPlayerLoadout: false,
            DefaultWorldType: "shooter-runtime",
            DefaultTickRate: 30,
            DefaultSyncTemplateId: "pure-state-authority")
    };

    private static async Task<string?> ValidateAccountAsync(IClusterClient client, string sessionToken)
    {
        var session = client.GetGrain<ISessionGrain>("global");
        var v = await session.ValidateAsync(new ValidateSessionRequest(sessionToken));
        return v.IsValid && !string.IsNullOrWhiteSpace(v.AccountId) ? v.AccountId : null;
    }

    private static async Task<RestoreRoomResponse> RestoreCurrentRoomAsync(IClusterClient client, string accountId)
    {
        var mapping = client.GetGrain<IRoomIdMappingGrain>("global");
        var roomId = await mapping.TryGetAccountRoomAsync(accountId);
        if (string.IsNullOrWhiteSpace(roomId))
        {
            return RestoreRoomResponse.Failed(
                RoomRestoreStatus.NoActiveRoom,
                RoomRestoreErrorCode.NoAccountRoomMapping,
                "No active room for account.");
        }

        var room = client.GetGrain<IRoomGrain>(roomId);
        var restore = await room.RestoreAsync(accountId);
        if (restore.HasActiveRoom)
        {
            await mapping.BindAccountRoomAsync(accountId, roomId);
        }
        else
        {
            await mapping.ClearAccountRoomAsync(accountId, roomId);
        }

        return restore;
    }

    private static async Task MarkCurrentRoomOfflineAsync(IClusterClient client, string accountId)
    {
        var mapping = client.GetGrain<IRoomIdMappingGrain>("global");
        var roomId = await mapping.TryGetAccountRoomAsync(accountId);
        if (string.IsNullOrWhiteSpace(roomId))
        {
            return;
        }

        try
        {
            var room = client.GetGrain<IRoomGrain>(roomId);
            await room.MarkOfflineAsync(accountId);
        }
        catch
        {
            await mapping.ClearAccountRoomAsync(accountId, roomId);
        }
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

    public sealed record AccountLoginHttpRequest(
        string AccountId,
        int ExpireSeconds,
        bool KickExisting);

    public sealed record SessionTokenHttpRequest(
        string SessionToken);

    public sealed record RenewSessionHttpRequest(
        string SessionToken,
        int ExtendSeconds,
        bool RotateToken);

    public sealed record GameplayHttpDescriptor(
        string RoomType,
        string DisplayName,
        int DefaultMaxPlayers,
        bool RequiresPlayerLoadout,
        string? DefaultWorldType,
        int DefaultTickRate,
        string? DefaultSyncTemplateId);

    public sealed record StartShooterSandboxHttpRequest(
        string? SandboxId,
        string? Region,
        string? ServerId,
        int? BotCount,
        int? MaxPlayers,
        int? TickRate,
        string? Title,
        IReadOnlyDictionary<string, string>? Tags);

    public sealed record ShooterSandboxControlHttpRequest(
        string? SandboxId);

    public sealed record CreateRoomHttpRequest(
        string SessionToken,
        string Region,
        string ServerId,
        string RoomType,
        string? Title,
        bool IsPublic,
        int MaxPlayers,
        IReadOnlyDictionary<string, string>? Tags);

    public sealed record ListRoomsHttpRequest(
        string SessionToken,
        string Region,
        string ServerId,
        int Offset,
        int Limit,
        string? RoomType);

    public sealed record JoinRoomHttpRequest(
        string SessionToken,
        string RoomId);

    public sealed record RoomSnapshotHttpRequest(
        string SessionToken,
        string RoomId);

    public sealed record RoomLeaveHttpRequest(
        string SessionToken,
        string RoomId);

    public sealed record RoomLeaveHttpResponse(
        bool Success,
        string RoomId,
        string AccountId);

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
