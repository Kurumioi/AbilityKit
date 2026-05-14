using AbilityKit.Orleans.Contracts.Accounts;
using AbilityKit.Orleans.Gateway.TcpGateway.Handler;
using Microsoft.Extensions.Options;
using Orleans;

namespace AbilityKit.Orleans.Gateway.TcpGateway;

[GatewayHandler(90)]
public sealed class GuestLoginRequestHandler : RequestHandlerBase
{
    private readonly IClusterClient _clusterClient;
    private readonly IOptions<TcpGatewayOptions> _options;
    private readonly ITcpGatewaySessionRegistry _registry;

    public override uint OpCode => _options.Value.GuestLoginOpCode;

    public GuestLoginRequestHandler(
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
        var session = _clusterClient.GetGrain<ISessionGrain>("global");
        var resp = await session.CreateGuestAsync();

        _registry.BindToken(resp.SessionToken, context.ConnectionId);

        var responsePayload = GatewaySerializer.Serialize(resp);
        return Messages.GatewayResponse.Ok(request.Seq, responsePayload);
    }
}
