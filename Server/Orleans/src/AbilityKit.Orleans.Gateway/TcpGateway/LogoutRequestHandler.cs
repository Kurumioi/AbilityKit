using AbilityKit.Orleans.Contracts.Accounts;
using AbilityKit.Orleans.Gateway.TcpGateway.Handler;
using Microsoft.Extensions.Options;
using Orleans;

namespace AbilityKit.Orleans.Gateway.TcpGateway;

[GatewayHandler(111)]
public sealed class LogoutRequestHandler : RequestHandlerBase
{
    private readonly IClusterClient _clusterClient;
    private readonly IOptions<TcpGatewayOptions> _options;
    private readonly ITcpGatewaySessionRegistry _registry;

    public override uint OpCode => _options.Value.LogoutOpCode;

    public LogoutRequestHandler(
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
        LogoutWireRequest wire;
        try
        {
            wire = GatewaySerializer.Deserialize<LogoutWireRequest>(request.Payload);
        }
        catch
        {
            return Messages.GatewayResponse.Error(request.Seq, Messages.TcpGatewayStatusCode.BadRequest);
        }

        if (string.IsNullOrWhiteSpace(wire.SessionToken))
        {
            return Messages.GatewayResponse.Error(request.Seq, Messages.TcpGatewayStatusCode.BadRequest);
        }

        var session = _clusterClient.GetGrain<ISessionGrain>("global");
        var resp = await session.LogoutAsync(new LogoutRequest(wire.SessionToken));

        _registry.UnbindToken(wire.SessionToken);

        var responsePayload = GatewaySerializer.Serialize(resp);
        return Messages.GatewayResponse.Ok(request.Seq, responsePayload);
    }
}

[MemoryPack.MemoryPackable]
public readonly partial struct LogoutWireRequest
{
    [MemoryPack.MemoryPackOrder(0)] public readonly string SessionToken;

    [MemoryPack.MemoryPackConstructor]
    public LogoutWireRequest(string sessionToken)
    {
        SessionToken = sessionToken;
    }
}
