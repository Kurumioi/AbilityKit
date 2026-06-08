using AbilityKit.Orleans.Contracts.Battle;
using AbilityKit.Orleans.Gateway.Abstractions;
using AbilityKit.Protocol.Room;
using Orleans;

namespace AbilityKit.Orleans.Gateway.Handlers;

/// <summary>
/// 通用战斗输入提交 Handler。payload 由具体玩法协议解释，Gateway 只负责鉴权与转发。
/// </summary>
[Core.GatewayHandler(RoomGatewayOpCodes.SubmitBattleInput)]
public sealed class SubmitBattleInputHandler : GatewayRequestHandlerBase
{
    private readonly IClusterClient _clusterClient;

    public SubmitBattleInputHandler(IClusterClient clusterClient)
    {
        _clusterClient = clusterClient;
    }

    public override async ValueTask<GatewayResponse> HandleAsync(
        GatewayRequest request,
        GatewaySessionContext context,
        CancellationToken cancellationToken)
    {
        if (request.Payload == null || request.Payload.Length == 0)
        {
            return GatewayResponse.Error(request.Seq, GatewayStatusCode.BadRequest);
        }

        var req = WireRoomGatewayBinary.Deserialize<WireSubmitBattleInputReq>(request.Payload);
        if (string.IsNullOrWhiteSpace(req.SessionToken) || string.IsNullOrWhiteSpace(req.BattleId) || req.WorldId == 0 || req.Frame < 0 || req.PlayerId == 0)
        {
            return GatewayResponse.Error(request.Seq, GatewayStatusCode.BadRequest);
        }

        try
        {
            var accountId = await RoomGatewayWireMapper.ValidateAccountAsync(_clusterClient, req.SessionToken);
            if (string.IsNullOrWhiteSpace(accountId))
            {
                return GatewayResponse.Error(request.Seq, GatewayStatusCode.BadRequest);
            }

            var battle = _clusterClient.GetGrain<IBattleLogicHostGrain>(req.BattleId);
            await battle.SubmitInputAsync(req.WorldId, req.Frame, new BattleInputItem
            {
                PlayerId = req.PlayerId,
                OpCode = req.InputOpCode,
                Payload = req.Payload ?? Array.Empty<byte>()
            });

            context.AccountId = accountId;
            var wire = new WireSubmitBattleInputRes
            {
                Success = true,
                AcceptedFrame = req.Frame,
                Message = string.Empty
            };
            var responsePayload = WireRoomGatewayBinary.Serialize(in wire);
            return GatewayResponse.Ok(request.Seq, responsePayload.ToArray());
        }
        catch (Exception)
        {
            return GatewayResponse.Error(request.Seq, GatewayStatusCode.InternalError);
        }
    }
}
