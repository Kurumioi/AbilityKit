using AbilityKit.Orleans.Contracts.Rooms;
using AbilityKit.Orleans.Gateway.Abstractions;
using Orleans;
using Orleans.Serialization;

namespace AbilityKit.Orleans.Gateway.Handlers;

/// <summary>
/// 加入房间 Handler
/// </summary>
[Core.GatewayHandler(102)]
public sealed class JoinRoomHandler : GatewayRequestHandlerBase
{
    private readonly IClusterClient _clusterClient;
    private readonly Serializer _serializer;

    public JoinRoomHandler(IClusterClient clusterClient, Serializer serializer)
    {
        _clusterClient = clusterClient;
        _serializer = serializer;
    }

    public override async ValueTask<GatewayResponse> HandleAsync(
        GatewayRequest request,
        GatewaySessionContext context,
        CancellationToken cancellationToken)
    {
        if (request.Payload == null || request.Payload.Length == 0)
            return GatewayResponse.Error(request.Seq, GatewayStatusCode.BadRequest);

        var req = _serializer.Deserialize<JoinRoomRequest>(request.Payload);
        if (req == null)
            return GatewayResponse.Error(request.Seq, GatewayStatusCode.BadRequest);

        try
        {
            var room = _clusterClient.GetGrain<IRoomGrain>(req.RoomId);
            await room.JoinAsync(req.AccountId);

            var snapshot = await room.GetSnapshotAsync();
            var responsePayload = _serializer.SerializeToArray(snapshot);

            context.RoomId = req.RoomId;
            context.AccountId = req.AccountId;

            return GatewayResponse.Ok(request.Seq, responsePayload);
        }
        catch (Exception)
        {
            return GatewayResponse.Error(request.Seq, GatewayStatusCode.InternalError);
        }
    }
}
