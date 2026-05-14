using AbilityKit.Orleans.Contracts.Accounts;
using AbilityKit.Orleans.Contracts.Rooms;
using AbilityKit.Orleans.Gateway.TcpGateway.Handler;
using System.Diagnostics;
using Microsoft.Extensions.Options;
using Orleans;

namespace AbilityKit.Orleans.Gateway.TcpGateway;

[GatewayHandler(101)]
public sealed class JoinRoomRequestHandler : RequestHandlerBase
{
    private readonly IClusterClient _clusterClient;
    private readonly IOptions<TcpGatewayOptions> _options;
    private readonly ITcpGatewaySessionRegistry _registry;

    public override uint OpCode => _options.Value.JoinRoomOpCode;

    public JoinRoomRequestHandler(
        IClusterClient clusterClient,
        IOptions<TcpGatewayOptions> options,
        ITcpGatewaySessionRegistry registry)
    {
        _clusterClient = clusterClient;
        _options = options;
        _registry = registry;
    }

    public override async ValueTask<Messages.GatewayResponse> HandleAsync(
        Messages.GatewayRequest request,
        TcpClientSessionContext context,
        CancellationToken cancellationToken)
    {
        JoinRoomWireRequest wire;
        try
        {
            wire = GatewaySerializer.Deserialize<JoinRoomWireRequest>(request.Payload);
        }
        catch
        {
            return Messages.GatewayResponse.Error(request.Seq, Messages.TcpGatewayStatusCode.BadRequest);
        }

        if (string.IsNullOrWhiteSpace(wire.SessionToken) || string.IsNullOrWhiteSpace(wire.RoomId))
        {
            return Messages.GatewayResponse.Error(request.Seq, Messages.TcpGatewayStatusCode.BadRequest);
        }

        var roomId = wire.RoomId;
        if (ulong.TryParse(roomId, out var numericRoomIdFromClient) && numericRoomIdFromClient != 0)
        {
            var mapper = _clusterClient.GetGrain<IRoomIdMappingGrain>("global");
            var mapped = await mapper.TryGetRoomIdAsync(numericRoomIdFromClient);
            if (!string.IsNullOrWhiteSpace(mapped))
            {
                roomId = mapped;
            }
        }

        var session = _clusterClient.GetGrain<ISessionGrain>("global");
        var v = await session.ValidateAsync(new ValidateSessionRequest(wire.SessionToken));
        if (!v.IsValid || string.IsNullOrWhiteSpace(v.AccountId))
        {
            return Messages.GatewayResponse.Error(request.Seq, Messages.TcpGatewayStatusCode.BadRequest);
        }

        _registry.BindToken(wire.SessionToken, context.ConnectionId);

        var room = _clusterClient.GetGrain<IRoomGrain>(roomId);
        await room.JoinAsync(v.AccountId);

        _registry.BindAccount(v.AccountId, context.ConnectionId);

        var mapper2 = _clusterClient.GetGrain<IRoomIdMappingGrain>("global");
        var numericRoomId = await mapper2.GetOrCreateNumericIdAsync(roomId);

        var snapshot = await room.GetSnapshotAsync();

        var anchor = new
        {
            StartServerTicks = Stopwatch.GetTimestamp(),
            ServerTickFrequency = Stopwatch.Frequency,
            StartFrame = 0,
            FixedDeltaSeconds = _options.Value.FixedDeltaSeconds
        };

        var responsePayload = GatewaySerializer.Serialize(new { NumericRoomId = numericRoomId, Snapshot = snapshot, WorldStartAnchor = anchor });
        return Messages.GatewayResponse.Ok(request.Seq, responsePayload);
    }
}

[MemoryPack.MemoryPackable]
public readonly partial struct JoinRoomWireRequest
{
    [MemoryPack.MemoryPackOrder(0)] public readonly string SessionToken;
    [MemoryPack.MemoryPackOrder(1)] public readonly string Region;
    [MemoryPack.MemoryPackOrder(2)] public readonly string ServerId;
    [MemoryPack.MemoryPackOrder(3)] public readonly string RoomId;

    [MemoryPack.MemoryPackConstructor]
    public JoinRoomWireRequest(string sessionToken, string region, string serverId, string roomId)
    {
        SessionToken = sessionToken;
        Region = region;
        ServerId = serverId;
        RoomId = roomId;
    }
}
