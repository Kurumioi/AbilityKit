using AbilityKit.Orleans.Contracts.Rooms;
using AbilityKit.Orleans.Gateway.Abstractions;
using AbilityKit.Protocol.Room;
using Orleans;

namespace AbilityKit.Orleans.Gateway.Handlers;

/// <summary>
/// 房间启动战斗 Handler
/// </summary>
[Core.GatewayHandler(RoomGatewayOpCodes.StartBattle)]
public sealed class StartRoomBattleHandler : GatewayRequestHandlerBase
{
    private readonly IClusterClient _clusterClient;

    public StartRoomBattleHandler(IClusterClient clusterClient)
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

        var req = WireRoomGatewayBinary.Deserialize<WireStartRoomBattleReq>(request.Payload);
        var roomId = string.IsNullOrWhiteSpace(req.RoomId) ? context.RoomId : req.RoomId;
        if (string.IsNullOrWhiteSpace(req.SessionToken) || string.IsNullOrWhiteSpace(roomId))
            return GatewayResponse.Error(request.Seq, GatewayStatusCode.BadRequest);

        try
        {
            var accountId = await RoomGatewayWireMapper.ValidateAccountAsync(_clusterClient, req.SessionToken);
            if (string.IsNullOrWhiteSpace(accountId))
                return GatewayResponse.Error(request.Seq, GatewayStatusCode.BadRequest);

            var room = _clusterClient.GetGrain<IRoomGrain>(roomId);
            var resp = await room.StartBattleAsync(new StartRoomBattleRequest(
                accountId,
                req.GameplayId,
                req.RuleSetId,
                req.ConfigVersion,
                req.ProtocolVersion,
                req.WorldType,
                req.ClientId));

            var wire = new WireStartRoomBattleRes
            {
                Success = true,
                BattleId = resp.BattleId ?? string.Empty,
                WorldId = resp.WorldId,
                Started = resp.Started,
                Message = string.Empty,
                WorldStartAnchor = RoomGatewayWireMapper.ToWireAnchor(resp.WorldStartAnchor),
                ServerNowTicks = resp.ServerNowTicks
            };
            var responsePayload = WireRoomGatewayBinary.Serialize(in wire);

            context.RoomId = roomId;
            context.AccountId = accountId;
            return GatewayResponse.Ok(request.Seq, responsePayload.ToArray());
        }
        catch (Exception)
        {
            return GatewayResponse.Error(request.Seq, GatewayStatusCode.InternalError);
        }
    }
}
