using System.Diagnostics;
using AbilityKit.Orleans.Gateway.TcpGateway.Handler;
using AbilityKit.Protocol.Moba.GatewayTimeSync;
using Microsoft.Extensions.Options;

namespace AbilityKit.Orleans.Gateway.TcpGateway;

[GatewayHandler(50)]
public sealed class TimeSyncRequestHandler : RequestHandlerBase
{
    private readonly IOptions<TcpGatewayOptions> _options;

    public override uint OpCode => _options.Value.TimeSyncOpCode;

    public TimeSyncRequestHandler(IOptions<TcpGatewayOptions> options)
    {
        _options = options;
    }

    public override ValueTask<Messages.GatewayResponse> HandleAsync(
        Messages.GatewayRequest request,
        TcpClientSessionContext context,
        CancellationToken cancellationToken)
    {
        var req = WireTimeSyncBinary.DeserializeTimeSyncReq(request.Payload);

        var serverNowTicks = Stopwatch.GetTimestamp();
        var serverFreq = Stopwatch.Frequency;

        var res = new WireTimeSyncRes(req.ClientSendTicks, serverNowTicks, serverFreq);
        var respPayload = WireTimeSyncBinary.Serialize(in res);
        return ValueTask.FromResult(Messages.GatewayResponse.Ok(request.Seq, respPayload));
    }
}
