using AbilityKit.Orleans.Contracts.Rooms;
using AbilityKit.Orleans.Gateway.Abstractions;
using AbilityKit.Protocol.Room;
using Orleans;

namespace AbilityKit.Orleans.Gateway.Handlers;

/// <summary>
/// Lists rooms for the binary room gateway used by Unity Shooter lobby flows.
/// </summary>
[Core.GatewayHandler(RoomGatewayOpCodes.ListRooms)]
public sealed partial class ListRoomsHandler : GatewayRequestHandlerBase
{
    private readonly IClusterClient _clusterClient;

    public ListRoomsHandler(IClusterClient clusterClient)
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

        var req = WireRoomGatewayBinary.Deserialize<WireListRoomsReq>(request.Payload);
        if (string.IsNullOrWhiteSpace(req.SessionToken) || string.IsNullOrWhiteSpace(req.Region) || string.IsNullOrWhiteSpace(req.ServerId))
            return GatewayResponse.Error(request.Seq, GatewayStatusCode.BadRequest);

        try
        {
            var accountId = await RoomGatewayWireMapper.ValidateAccountAsync(_clusterClient, req.SessionToken);
            if (string.IsNullOrWhiteSpace(accountId))
                return GatewayResponse.Error(request.Seq, GatewayStatusCode.BadRequest);

            var directoryKey = $"{req.Region}:{req.ServerId}";
            var directory = _clusterClient.GetGrain<IRoomDirectoryGrain>(directoryKey);
            var response = await directory.ListRoomsAsync(new ListRoomsRequest(
                accountId,
                req.Region,
                req.ServerId,
                req.Offset,
                req.Limit,
                string.IsNullOrWhiteSpace(req.RoomType) ? null : req.RoomType));

            var wire = RoomGatewayWireMapper.ToListRoomsRes(response);
            var responsePayload = WireRoomGatewayBinary.Serialize(in wire);

            context.AccountId = accountId;
            return GatewayResponse.Ok(request.Seq, responsePayload.ToArray());
        }
        catch (Exception exception)
        {
            return RoomGatewayErrorMapper.ToResponse(request.Seq, exception);
        }
    }
}
