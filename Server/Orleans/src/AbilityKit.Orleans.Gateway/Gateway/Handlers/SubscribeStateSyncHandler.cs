using AbilityKit.Orleans.Contracts.Battle;
using AbilityKit.Orleans.Gateway.Abstractions;
using AbilityKit.Protocol.Room;
using Orleans;

namespace AbilityKit.Orleans.Gateway.Handlers;

/// <summary>
/// 订阅状态同步 Handler
/// </summary>
[Core.GatewayHandler(RoomGatewayOpCodes.SubscribeStateSync)]
public sealed class SubscribeStateSyncHandler : GatewayRequestHandlerBase
{
    private readonly IClusterClient _clusterClient;
    private readonly IGatewaySessionRegistry _sessionRegistry;
    public SubscribeStateSyncHandler(
        IClusterClient clusterClient,
        IGatewaySessionRegistry sessionRegistry)
    {
        _clusterClient = clusterClient;
        _sessionRegistry = sessionRegistry;
    }

    public override async ValueTask<GatewayResponse> HandleAsync(
        GatewayRequest request,
        GatewaySessionContext context,
        CancellationToken cancellationToken)
    {
        if (request.Payload == null || request.Payload.Length == 0)
            return GatewayResponse.Error(request.Seq, GatewayStatusCode.BadRequest);

        var req = WireRoomGatewayBinary.Deserialize<WireSubscribeStateSyncReq>(request.Payload);
        if (string.IsNullOrWhiteSpace(req.SessionToken) || string.IsNullOrWhiteSpace(req.BattleId))
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

            var roomKey = string.IsNullOrWhiteSpace(req.RoomId) ? req.BattleId : req.RoomId;
            var observerKey = $"{accountId}:{roomKey}";
            var observerGrain = _clusterClient.GetGrain<IStateSyncObserverGrain>(observerKey);

            await observerGrain.SubscribeAsync(req.BattleId);

            context.AccountId = accountId;
            if (context.ConnectionId > 0)
            {
                _sessionRegistry.BindAccount(accountId, context.ConnectionId);
            }

            var wire = new WireSubscribeStateSyncRes
            {
                Success = true,
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

