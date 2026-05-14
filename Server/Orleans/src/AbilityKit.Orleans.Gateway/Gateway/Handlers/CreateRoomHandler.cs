using AbilityKit.Orleans.Contracts.Rooms;
using AbilityKit.Orleans.Gateway.Abstractions;
using Orleans;
using Orleans.Serialization;

namespace AbilityKit.Orleans.Gateway.Handlers;

/// <summary>
/// 创建房间 Handler
/// </summary>
[Core.GatewayHandler(101)]
public sealed class CreateRoomHandler : GatewayRequestHandlerBase
{
    private readonly IClusterClient _clusterClient;
    private readonly Serializer _serializer;

    public CreateRoomHandler(IClusterClient clusterClient, Serializer serializer)
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

        var req = _serializer.Deserialize<CreateRoomRequest>(request.Payload);
        if (req == null)
            return GatewayResponse.Error(request.Seq, GatewayStatusCode.BadRequest);

        try
        {
            var directoryKey = $"{req.Region}:{req.ServerId}";
            var directory = _clusterClient.GetGrain<IRoomDirectoryGrain>(directoryKey);

            var resp = await directory.CreateRoomAsync(req);

            var responsePayload = _serializer.SerializeToArray(resp);

            if (!string.IsNullOrEmpty(resp.RoomId))
            {
                context.RoomId = resp.RoomId;
            }

            return GatewayResponse.Ok(request.Seq, responsePayload);
        }
        catch (Exception)
        {
            return GatewayResponse.Error(request.Seq, GatewayStatusCode.InternalError);
        }
    }
}
