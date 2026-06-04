using System.Diagnostics;
using AbilityKit.Orleans.Gateway.Abstractions;
using AbilityKit.Orleans.Gateway.Serialization;
using AbilityKit.Protocol.Moba.GatewayTimeSync;

namespace AbilityKit.Orleans.Gateway.Handlers;

/// <summary>
/// 时间同步 Handler
/// </summary>
[Core.GatewayHandler(OpCodes.TimeSync)]
public sealed class TimeSyncHandler : GatewayRequestHandlerBase
{
    public override ValueTask<GatewayResponse> HandleAsync(
        GatewayRequest request,
        GatewaySessionContext context,
        CancellationToken cancellationToken)
    {
        var req = WireTimeSyncBinary.DeserializeTimeSyncReq(request.Payload);

        var serverNowTicks = Stopwatch.GetTimestamp();
        var serverFreq = Stopwatch.Frequency;

        var res = new WireTimeSyncRes(req.ClientSendTicks, serverNowTicks, serverFreq);
        var respPayload = WireTimeSyncBinary.Serialize(in res);
        return ValueTask.FromResult(GatewayResponse.Ok(request.Seq, respPayload.ToArray()));
    }
}
