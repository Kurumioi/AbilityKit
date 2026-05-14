using AbilityKit.Orleans.Contracts.Accounts;
using AbilityKit.Orleans.Contracts.Rooms;
using AbilityKit.Orleans.Gateway.TcpGateway.Handler;
using Microsoft.Extensions.Options;
using Orleans;

namespace AbilityKit.Orleans.Gateway.TcpGateway;

[GatewayHandler(104)]
public sealed class CloseRoomRequestHandler : RequestHandlerBase
{
    private readonly IClusterClient _clusterClient;
    private readonly IOptions<TcpGatewayOptions> _options;
    private readonly ITcpGatewaySessionRegistry _registry;

    public override uint OpCode => _options.Value.CloseRoomOpCode;

    public CloseRoomRequestHandler(
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
        CloseRoomWireRequest wire;
        try
        {
            wire = GatewaySerializer.Deserialize<CloseRoomWireRequest>(request.Payload);
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
        await room.CloseAsync(v.AccountId);

        return Messages.GatewayResponse.Ok(request.Seq);
    }
}

[MemoryPack.MemoryPackable]
public readonly partial struct CloseRoomWireRequest
{
    [MemoryPack.MemoryPackOrder(0)] public readonly string SessionToken;
    [MemoryPack.MemoryPackOrder(1)] public readonly string RoomId;

    [MemoryPack.MemoryPackConstructor]
    public CloseRoomWireRequest(string sessionToken, string roomId)
    {
        SessionToken = sessionToken;
        RoomId = roomId;
    }
}
