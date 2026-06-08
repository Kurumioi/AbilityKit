using AbilityKit.Orleans.Contracts.Rooms;
using AbilityKit.Orleans.Gateway.Abstractions;
using AbilityKit.Protocol.Room;
using Orleans;

namespace AbilityKit.Orleans.Gateway.Handlers;

/// <summary>
/// 加入房间 Handler
/// </summary>
[Core.GatewayHandler(RoomGatewayOpCodes.JoinRoom)]
public sealed class JoinRoomHandler : GatewayRequestHandlerBase
{
    private readonly IClusterClient _clusterClient;

    public JoinRoomHandler(IClusterClient clusterClient)
    {
        _clusterClient = clusterClient;
    }

    public override async ValueTask<GatewayResponse> HandleAsync(
        GatewayRequest request,
        GatewaySessionContext context,
        CancellationToken cancellationToken)
    {
        if (request.Payload == null || request.Payload.Length == 0)
            return GatewayResponse.Error(request.Seq, GatewayStatusCode.BadRequest);

        var req = WireRoomGatewayBinary.Deserialize<WireJoinRoomReq>(request.Payload);
        if (string.IsNullOrWhiteSpace(req.SessionToken) || string.IsNullOrWhiteSpace(req.RoomId))
            return GatewayResponse.Error(request.Seq, GatewayStatusCode.BadRequest);

        try
        {
            var accountId = await RoomGatewayWireMapper.ValidateAccountAsync(_clusterClient, req.SessionToken);
            if (string.IsNullOrWhiteSpace(accountId))
                return GatewayResponse.Error(request.Seq, GatewayStatusCode.BadRequest);

            var room = _clusterClient.GetGrain<IRoomGrain>(req.RoomId);
            await room.JoinAsync(accountId);

            var snapshot = await room.GetSnapshotAsync();
            var wire = RoomGatewayWireMapper.ToJoinRoomRes(snapshot);
            var responsePayload = WireRoomGatewayBinary.Serialize(in wire);

            context.RoomId = req.RoomId;
            context.AccountId = accountId;

            return GatewayResponse.Ok(request.Seq, responsePayload.ToArray());
        }
        catch (Exception)
        {
            return GatewayResponse.Error(request.Seq, GatewayStatusCode.InternalError);
        }
    }
}
