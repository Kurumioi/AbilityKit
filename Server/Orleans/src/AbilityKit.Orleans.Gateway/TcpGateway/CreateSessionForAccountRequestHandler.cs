using AbilityKit.Orleans.Contracts.Accounts;
using AbilityKit.Orleans.Gateway.TcpGateway.Handler;
using Microsoft.Extensions.Options;
using Orleans;

namespace AbilityKit.Orleans.Gateway.TcpGateway;

[GatewayHandler(112)]
public sealed class CreateSessionForAccountRequestHandler : RequestHandlerBase
{
    private readonly IClusterClient _clusterClient;
    private readonly IOptions<TcpGatewayOptions> _options;
    private readonly ITcpGatewaySessionRegistry _registry;

    public override uint OpCode => _options.Value.CreateSessionForAccountOpCode;

    public CreateSessionForAccountRequestHandler(
        IClusterClient clusterClient,
        IOptions<TcpGatewayOptions> options,
        ITcpGatewaySessionRegistry registry)
    {
        _clusterClient = clusterClient;
        _options = options;
        _registry = registry;
    }

    public override async ValueTask<Messages.GatewayResponse> HandleAsync(
        Messages.GatewayRequest request,
        TcpClientSessionContext context,
        CancellationToken cancellationToken)
    {
        CreateSessionWireRequest wire;
        try
        {
            wire = GatewaySerializer.Deserialize<CreateSessionWireRequest>(request.Payload);
        }
        catch
        {
            return Messages.GatewayResponse.Error(request.Seq, Messages.TcpGatewayStatusCode.BadRequest);
        }

        if (string.IsNullOrWhiteSpace(wire.AccountId))
        {
            return Messages.GatewayResponse.Error(request.Seq, Messages.TcpGatewayStatusCode.BadRequest);
        }

        var session = _clusterClient.GetGrain<ISessionGrain>("global");
        var resp = await session.CreateSessionForAccountAsync(new CreateSessionForAccountRequest(wire.AccountId, wire.ExpireSeconds, wire.KickExisting));

        _registry.BindToken(resp.SessionToken, context.ConnectionId);

        if (!string.IsNullOrWhiteSpace(resp.KickedSessionToken))
        {
            await _registry.TrySendKickAsync(resp.KickedSessionToken, reason: "sso_kicked", cancellationToken);
            _registry.UnbindToken(resp.KickedSessionToken);
        }

        var responsePayload = GatewaySerializer.Serialize(resp);
        return Messages.GatewayResponse.Ok(request.Seq, responsePayload);
    }
}

[MemoryPack.MemoryPackable]
public readonly partial struct CreateSessionWireRequest
{
    [MemoryPack.MemoryPackOrder(0)] public readonly string AccountId;
    [MemoryPack.MemoryPackOrder(1)] public readonly int ExpireSeconds;
    [MemoryPack.MemoryPackOrder(2)] public readonly bool KickExisting;

    [MemoryPack.MemoryPackConstructor]
    public CreateSessionWireRequest(string accountId, int expireSeconds, bool kickExisting)
    {
        AccountId = accountId;
        ExpireSeconds = expireSeconds;
        KickExisting = kickExisting;
    }
}
