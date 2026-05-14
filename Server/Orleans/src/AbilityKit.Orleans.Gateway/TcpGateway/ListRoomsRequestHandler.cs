using AbilityKit.Orleans.Contracts.Accounts;
using AbilityKit.Orleans.Contracts.Rooms;
using AbilityKit.Orleans.Gateway.TcpGateway.Handler;
using Microsoft.Extensions.Options;
using Orleans;

namespace AbilityKit.Orleans.Gateway.TcpGateway;

[GatewayHandler(103)]
public sealed class ListRoomsRequestHandler : RequestHandlerBase
{
    private readonly IClusterClient _clusterClient;
    private readonly IOptions<TcpGatewayOptions> _options;
    private readonly ITcpGatewaySessionRegistry _registry;

    public override uint OpCode => _options.Value.ListRoomsOpCode;

    public ListRoomsRequestHandler(
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
        ListRoomsWireRequest wire;
        try
        {
            wire = GatewaySerializer.Deserialize<ListRoomsWireRequest>(request.Payload);
        }
        catch
        {
            return Messages.GatewayResponse.Error(request.Seq, Messages.TcpGatewayStatusCode.BadRequest);
        }

        if (string.IsNullOrWhiteSpace(wire.SessionToken))
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

        var req = new ListRoomsRequest(v.AccountId, wire.Region, wire.ServerId, wire.Offset, wire.Limit, wire.RoomType);
        var resp = await directory.ListRoomsAsync(req);

        var responsePayload = GatewaySerializer.Serialize(resp);
        return Messages.GatewayResponse.Ok(request.Seq, responsePayload);
    }
}

[MemoryPack.MemoryPackable]
public readonly partial struct ListRoomsWireRequest
{
    [MemoryPack.MemoryPackOrder(0)] public readonly string SessionToken;
    [MemoryPack.MemoryPackOrder(1)] public readonly string Region;
    [MemoryPack.MemoryPackOrder(2)] public readonly string ServerId;
    [MemoryPack.MemoryPackOrder(3)] public readonly int Offset;
    [MemoryPack.MemoryPackOrder(4)] public readonly int Limit;
    [MemoryPack.MemoryPackOrder(5)] public readonly string? RoomType;

    [MemoryPack.MemoryPackConstructor]
    public ListRoomsWireRequest(string sessionToken, string region, string serverId, int offset, int limit, string? roomType)
    {
        SessionToken = sessionToken;
        Region = region;
        ServerId = serverId;
        Offset = offset;
        Limit = limit;
        RoomType = roomType;
    }
}
