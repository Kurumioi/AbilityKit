using AbilityKit.Orleans.Contracts.Accounts;
using AbilityKit.Orleans.Contracts.Battle;
using AbilityKit.Orleans.Contracts.Rooms;
using AbilityKit.Orleans.Gateway.TcpGateway.Handler;
using AbilityKit.Orleans.Gateway.TcpGateway.StateSync;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans;
using AbilityKit.Ability.StateSync.Network;
using AbilityKit.Ability.StateSync.Snapshot;
using AbilityKit.Protocol.Moba.StateSync;

namespace AbilityKit.Orleans.Gateway.TcpGateway;

[GatewayHandler(100)]
public sealed class CreateRoomRequestHandler : RequestHandlerBase
{
    private readonly IClusterClient _clusterClient;
    private readonly IOptions<TcpGatewayOptions> _options;
    private readonly ITcpGatewaySessionRegistry _registry;
    private readonly IStateSyncHandler _stateSyncHandler;
    private readonly ILogger<CreateRoomRequestHandler> _logger;

    public override uint OpCode => _options.Value.CreateRoomOpCode;

    public CreateRoomRequestHandler(
        IClusterClient clusterClient,
        IOptions<TcpGatewayOptions> options,
        ITcpGatewaySessionRegistry registry,
        IStateSyncHandler stateSyncHandler,
        ILogger<CreateRoomRequestHandler> logger)
    {
        _clusterClient = clusterClient;
        _options = options;
        _registry = registry;
        _stateSyncHandler = stateSyncHandler;
        _logger = logger;
    }

    public override async ValueTask<Messages.GatewayResponse> HandleAsync(
        Messages.GatewayRequest request,
        TcpClientSessionContext context,
        CancellationToken cancellationToken)
    {
        CreateRoomWireRequest wire;
        try
        {
            wire = GatewaySerializer.Deserialize<CreateRoomWireRequest>(request.Payload);
        }
        catch
        {
            return Messages.GatewayResponse.Error(request.Seq, Messages.TcpGatewayStatusCode.BadRequest);
        }

        var session = _clusterClient.GetGrain<ISessionGrain>("global");
        var v = await session.ValidateAsync(new ValidateSessionRequest(wire.SessionToken));
        if (!v.IsValid || string.IsNullOrWhiteSpace(v.AccountId))
        {
            return Messages.GatewayResponse.Error(request.Seq, Messages.TcpGatewayStatusCode.BadRequest);
        }

        _registry.BindToken(wire.SessionToken, context.ConnectionId);

        var directoryKey = $"{wire.Region}:{wire.ServerId}";
        var directory = _clusterClient.GetGrain<IRoomDirectoryGrain>(directoryKey);

        var req = new CreateRoomRequest(
            v.AccountId,
            wire.Region,
            wire.ServerId,
            wire.RoomType,
            wire.Title,
            wire.IsPublic,
            wire.MaxPlayers,
            wire.Tags == null ? null : new Dictionary<string, string>(wire.Tags));

        var resp = await directory.CreateRoomAsync(req);

        await InitializeBattleForRoomAsync(resp.RoomId, wire.SessionToken);

        var mapper = _clusterClient.GetGrain<IRoomIdMappingGrain>("global");
        var numericRoomId = await mapper.GetOrCreateNumericIdAsync(resp.RoomId);

        var responsePayload = GatewaySerializer.Serialize(new { resp.RoomId, NumericRoomId = numericRoomId });
        return Messages.GatewayResponse.Ok(request.Seq, responsePayload);
    }

    private async Task InitializeBattleForRoomAsync(string roomId, string sessionToken)
    {
        try
        {
            var battleGrain = _clusterClient.GetGrain<IBattleLogicHostGrain>(roomId);

            var initParams = new BattleInitParams
            {
                WorldId = (ulong)roomId.GetHashCode(),
                TickRate = 30,
                Players = new List<PlayerInitInfo>
                {
                    new PlayerInitInfo
                    {
                        PlayerId = 1,
                        ActorId = 1,
                        HeroId = 1001,
                        PosX = 0,
                        PosY = 0,
                        PosZ = 0,
                        TeamId = 1
                    }
                }
            };

            await battleGrain.InitializeBattleAsync(initParams);

            var observer = new BattleStateSyncObserver(roomId, _stateSyncHandler, _logger);
            await battleGrain.SubscribeAsync(observer);

            _logger.LogInformation("[CreateRoom] Battle initialized for room: {RoomId}", roomId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CreateRoom] Failed to initialize battle for room: {RoomId}", roomId);
        }
    }
}

[MemoryPack.MemoryPackable]
public readonly partial struct CreateRoomWireRequest
{
    [MemoryPack.MemoryPackOrder(0)] public readonly string SessionToken;
    [MemoryPack.MemoryPackOrder(1)] public readonly string Region;
    [MemoryPack.MemoryPackOrder(2)] public readonly string ServerId;
    [MemoryPack.MemoryPackOrder(3)] public readonly string RoomType;
    [MemoryPack.MemoryPackOrder(4)] public readonly string Title;
    [MemoryPack.MemoryPackOrder(5)] public readonly bool IsPublic;
    [MemoryPack.MemoryPackOrder(6)] public readonly int MaxPlayers;
    [MemoryPack.MemoryPackOrder(7)] public readonly Dictionary<string, string>? Tags;

    [MemoryPack.MemoryPackConstructor]
    public CreateRoomWireRequest(string sessionToken, string region, string serverId, string roomType, string title, bool isPublic, int maxPlayers, Dictionary<string, string>? tags)
    {
        SessionToken = sessionToken;
        Region = region;
        ServerId = serverId;
        RoomType = roomType;
        Title = title;
        IsPublic = isPublic;
        MaxPlayers = maxPlayers;
        Tags = tags;
    }
}

/// <summary>
/// Battle StateSync 观察者实现
/// </summary>
internal sealed class BattleStateSyncObserver : IStateSyncObserver
{
    private readonly string _roomId;
    private readonly IStateSyncHandler _stateSyncHandler;
    private readonly ILogger _logger;

    public BattleStateSyncObserver(string roomId, IStateSyncHandler stateSyncHandler, ILogger logger)
    {
        _roomId = roomId;
        _stateSyncHandler = stateSyncHandler;
        _logger = logger;
    }

    public void OnSnapshotPushed(StateSyncPush push)
    {
        try
        {
            // 将 BattleSnapshot 转换为 world.statesync 的 WorldStateSnapshot
            var entities = push.Actors.Select(a => new EntityStateSnapshot(a.ActorId)
            {
                Position = new Vec3(a.X, a.Y, a.Z),
                Velocity = new Vec3(a.VelocityX, 0, a.VelocityZ),
                HealthPercent = (byte)(a.Hp / a.HpMax * 100),
                TeamId = a.TeamId
            }).ToList();

            var snapshot = new WorldStateSnapshot
            {
                Frame = push.Frame,
                Timestamp = (long)push.Timestamp,
                Entities = entities
            };

            var message = new SnapshotMessage
            {
                WorldId = push.WorldId,
                Frame = snapshot.Frame,
                Timestamp = snapshot.Timestamp,
                IsFullSnapshot = push.IsFullSnapshot,
                SnapshotData = snapshot.ToBytes()
            };

            _stateSyncHandler.HandleSnapshotPush(message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[BattleStateSyncObserver] Error pushing snapshot for room: {RoomId}", _roomId);
        }
    }
}
