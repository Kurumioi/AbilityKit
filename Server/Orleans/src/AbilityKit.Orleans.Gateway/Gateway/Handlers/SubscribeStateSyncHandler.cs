using AbilityKit.Orleans.Contracts.Battle;
using AbilityKit.Orleans.Gateway.Abstractions;
using MemoryPack;
using Orleans;
using Orleans.Serialization;

namespace AbilityKit.Orleans.Gateway.Handlers;

/// <summary>
/// 订阅状态同步 Handler
/// </summary>
[Core.GatewayHandler(103)]
public sealed class SubscribeStateSyncHandler : GatewayRequestHandlerBase
{
    private readonly IClusterClient _clusterClient;
    private readonly IGatewaySessionRegistry _sessionRegistry;
    private readonly Serializer _serializer;

    public SubscribeStateSyncHandler(
        IClusterClient clusterClient,
        IGatewaySessionRegistry sessionRegistry,
        Serializer serializer)
    {
        _clusterClient = clusterClient;
        _sessionRegistry = sessionRegistry;
        _serializer = serializer;
    }

    public override async ValueTask<GatewayResponse> HandleAsync(
        GatewayRequest request,
        GatewaySessionContext context,
        CancellationToken cancellationToken)
    {
        if (request.Payload == null || request.Payload.Length == 0)
            return GatewayResponse.Error(request.Seq, GatewayStatusCode.BadRequest);

        var req = _serializer.Deserialize<SubscribeStateSyncReq>(request.Payload);
        if (string.IsNullOrEmpty(req.BattleGrainKey))
            return GatewayResponse.Error(request.Seq, GatewayStatusCode.BadRequest);

        try
        {
            // 创建或获取状态同步观察者
            var observerKey = $"{context.AccountId}:{req.RoomId}";
            var observerGrain = _clusterClient.GetGrain<IStateSyncObserverGrain>(observerKey);

            // 订阅战斗状态同步
            await observerGrain.SubscribeAsync(req.BattleGrainKey);

            // 绑定账号到会话（如果没有绑定的话）
            if (!string.IsNullOrEmpty(context.AccountId) && context.ConnectionId > 0)
            {
                _sessionRegistry.BindAccount(context.AccountId, context.ConnectionId);
            }

            var responsePayload = _serializer.SerializeToArray(new SubscribeStateSyncRes
            {
                Success = true,
                Message = "Subscribed to state sync"
            });

            return GatewayResponse.Ok(request.Seq, responsePayload);
        }
        catch (Exception)
        {
            return GatewayResponse.Error(request.Seq, GatewayStatusCode.InternalError);
        }
    }
}

[MemoryPackable]
public readonly partial struct SubscribeStateSyncReq
{
    [MemoryPackOrder(0)] public string BattleGrainKey { get; init; }
    [MemoryPackOrder(1)] public string RoomId { get; init; }
}

[MemoryPackable]
public readonly partial struct SubscribeStateSyncRes
{
    [MemoryPackOrder(0)] public bool Success { get; init; }
    [MemoryPackOrder(1)] public string Message { get; init; }
}
