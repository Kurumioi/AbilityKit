using AbilityKit.Orleans.Contracts.Accounts;
using AbilityKit.Orleans.Gateway.Abstractions;
using AbilityKit.Orleans.Gateway.Serialization;
using MemoryPack;
using Orleans;

namespace AbilityKit.Orleans.Gateway.Handlers;

/// <summary>
/// Guest 登录 Handler
/// </summary>
[Core.GatewayHandler(100)]
public sealed class GuestLoginHandler : GatewayRequestHandlerBase
{
    private readonly IClusterClient _clusterClient;
    private readonly IGatewaySessionRegistry _registry;

    public GuestLoginHandler(
        IClusterClient clusterClient,
        IGatewaySessionRegistry registry)
    {
        _clusterClient = clusterClient;
        _registry = registry;
    }

    public override async ValueTask<GatewayResponse> HandleAsync(
        GatewayRequest request,
        GatewaySessionContext context,
        CancellationToken cancellationToken)
    {
        if (request.Payload == null || request.Payload.Length == 0)
            return GatewayResponse.Error(request.Seq, GatewayStatusCode.BadRequest);

        var req = GatewaySerializer.Deserialize<GuestLoginReq>(request.Payload);
        if (string.IsNullOrEmpty(req.GuestId))
            return GatewayResponse.Error(request.Seq, GatewayStatusCode.BadRequest);

        var session = _clusterClient.GetGrain<ISessionGrain>("global");
        var resp = await session.CreateGuestAsync();

        _registry.BindToken(resp.SessionToken, context.ConnectionId);

        var responsePayload = GatewaySerializer.Serialize(new GuestLoginRes
        {
            SessionToken = resp.SessionToken,
            AccountId = resp.AccountId,
            Success = true
        });

        return GatewayResponse.Ok(request.Seq, responsePayload.ToArray());
    }
}

[MemoryPackable]
public readonly partial struct GuestLoginReq
{
    [MemoryPackOrder(0)] public string GuestId { get; init; }
}

[MemoryPackable]
public readonly partial struct GuestLoginRes
{
    [MemoryPackOrder(0)] public bool Success { get; init; }
    [MemoryPackOrder(1)] public string SessionToken { get; init; }
    [MemoryPackOrder(2)] public string AccountId { get; init; }
    [MemoryPackOrder(3)] public string Message { get; init; }
}
