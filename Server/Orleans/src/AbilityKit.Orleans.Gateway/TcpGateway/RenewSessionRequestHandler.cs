using AbilityKit.Orleans.Contracts.Accounts;
using AbilityKit.Orleans.Gateway.TcpGateway.Handler;
using Microsoft.Extensions.Options;
using Orleans;

namespace AbilityKit.Orleans.Gateway.TcpGateway;

[GatewayHandler(110)]
public sealed class RenewSessionRequestHandler : RequestHandlerBase
{
    private readonly IClusterClient _clusterClient;
    private readonly IOptions<TcpGatewayOptions> _options;
    private readonly ITcpGatewaySessionRegistry _registry;

    public override uint OpCode => _options.Value.RenewSessionOpCode;

    public RenewSessionRequestHandler(
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
        RenewSessionWireRequest wire;
        try
        {
            wire = GatewaySerializer.Deserialize<RenewSessionWireRequest>(request.Payload);
        }
        catch
        {
            return Messages.GatewayResponse.Error(request.Seq, Messages.TcpGatewayStatusCode.BadRequest);
        }

        if (string.IsNullOrWhiteSpace(wire.SessionToken))
        {
            return Messages.GatewayResponse.Error(request.Seq, Messages.TcpGatewayStatusCode.BadRequest);
        }

        var oldToken = wire.SessionToken;
        var oldBoundOther = _registry.TryGetConnectionIdByToken(oldToken, out var oldConnId) && oldConnId != context.ConnectionId;

        var session = _clusterClient.GetGrain<ISessionGrain>("global");
        var resp = await session.RenewAsync(new RenewSessionRequest(wire.SessionToken, wire.ExtendSeconds, wire.RotateToken));

        if (resp.IsValid && !string.IsNullOrWhiteSpace(resp.SessionToken))
        {
            if (wire.RotateToken)
            {
                _registry.UnbindToken(oldToken);
            }

            _registry.BindToken(resp.SessionToken, context.ConnectionId);

            var v = await session.ValidateAsync(new ValidateSessionRequest(resp.SessionToken));
            if (v.IsValid && !string.IsNullOrWhiteSpace(v.AccountId))
            {
                _registry.BindAccount(v.AccountId, context.ConnectionId);
            }

            if (oldBoundOther)
            {
                await _registry.TrySendKickAsync(oldToken, reason: "token_rotated", cancellationToken);
                _registry.UnbindToken(oldToken);
            }
        }

        var responsePayload = GatewaySerializer.Serialize(resp);
        return Messages.GatewayResponse.Ok(request.Seq, responsePayload);
    }
}

[MemoryPack.MemoryPackable]
public readonly partial struct RenewSessionWireRequest
{
    [MemoryPack.MemoryPackOrder(0)] public readonly string SessionToken;
    [MemoryPack.MemoryPackOrder(1)] public readonly int ExtendSeconds;
    [MemoryPack.MemoryPackOrder(2)] public readonly bool RotateToken;

    [MemoryPack.MemoryPackConstructor]
    public RenewSessionWireRequest(string sessionToken, int extendSeconds, bool rotateToken)
    {
        SessionToken = sessionToken;
        ExtendSeconds = extendSeconds;
        RotateToken = rotateToken;
    }
}
