using AbilityKit.Orleans.Contracts.Battle;
using AbilityKit.Orleans.Gateway.Abstractions;
using AbilityKit.Protocol.Room;
using Orleans;

namespace AbilityKit.Orleans.Gateway.Handlers;

/// <summary>
/// 閫氱敤鎴樻枟杈撳叆鎻愪氦 Handler銆俻ayload 鐢卞叿浣撶帺娉曞崗璁В閲婏紝Gateway 鍙礋璐ｉ壌鏉冧笌杞彂銆?
/// </summary>
[Core.GatewayHandler(RoomGatewayOpCodes.SubmitBattleInput)]
public sealed partial class SubmitBattleInputHandler : GatewayRequestHandlerBase
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
            var submit = await battle.SubmitInputAsync(req.WorldId, req.Frame, new BattleInputItem
            {
                PlayerId = req.PlayerId,
                OpCode = req.InputOpCode,
                Payload = req.Payload ?? Array.Empty<byte>()
            });

            context.AccountId = accountId;
            var wire = new WireSubmitBattleInputRes
            {
                Success = submit.Accepted,
                AcceptedFrame = submit.AcceptedFrame,
                Message = submit.Message,
                CurrentFrame = submit.CurrentFrame,
                Status = submit.Status,
                ShouldResync = !submit.Accepted,
                ServerTicks = DateTime.UtcNow.Ticks
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

