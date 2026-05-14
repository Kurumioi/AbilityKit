using AbilityKit.Orleans.Contracts.Accounts;
using AbilityKit.Orleans.Contracts.Rooms;
using AbilityKit.Orleans.Gateway.TcpGateway.Handler;
using Microsoft.Extensions.Options;
using Orleans;

namespace AbilityKit.Orleans.Gateway.TcpGateway;

[GatewayHandler(102)]
public sealed class LeaveRoomRequestHandler : RequestHandlerBase
{
    private readonly IClusterClient _clusterClient;
    private readonly IOptions<TcpGatewayOptions> _options;
    private readonly ITcpGatewaySessionRegistry _registry;

    public override uint OpCode => _options.Value.LeaveRoomOpCode;

    public LeaveRoomRequestHandler(
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
        LeaveRoomWireRequest wire;
        try
        {
            wire = GatewaySerializer.Deserialize<LeaveRoomWireRequest>(request.Payload);
        }
        catch
        {
            return Messages.GatewayResponse.Error(request.Seq, Messages.TcpGatewayStatusCode.BadRequest);
        }

        if (string.IsNullOrWhiteSpace(wire.SessionToken) || string.IsNullOrWhiteSpace(wire.RoomId))
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

        var room = _clusterClient.GetGrain<IRoomGrain>(wire.RoomId);
        await room.LeaveAsync(v.AccountId);

        _registry.UnbindAccount(v.AccountId);

        var snapshot = await room.GetSnapshotAsync();
        var responsePayload = GatewaySerializer.Serialize(snapshot);
        return Messages.GatewayResponse.Ok(request.Seq, responsePayload);
    }
}

[MemoryPack.MemoryPackable]
public readonly partial struct LeaveRoomWireRequest
{
    [MemoryPack.MemoryPackOrder(0)] public readonly string SessionToken;
    [MemoryPack.MemoryPackOrder(1)] public readonly string Region;
    [MemoryPack.MemoryPackOrder(2)] public readonly string ServerId;
    [MemoryPack.MemoryPackOrder(3)] public readonly string RoomId;

    [MemoryPack.MemoryPackConstructor]
    public LeaveRoomWireRequest(string sessionToken, string region, string serverId, string roomId)
    {
        SessionToken = sessionToken;
        Region = region;
        ServerId = serverId;
        RoomId = roomId;
    }
}
