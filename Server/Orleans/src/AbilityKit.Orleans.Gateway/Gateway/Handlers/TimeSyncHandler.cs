using AbilityKit.Orleans.Gateway.Abstractions;
using AbilityKit.Orleans.Gateway.Serialization;
using AbilityKit.Protocol.GatewayTimeSync;

namespace AbilityKit.Orleans.Gateway.Handlers;

/// <summary>
/// 鏃堕棿鍚屾 Handler
/// </summary>
[Core.GatewayHandler(OpCodes.TimeSync)]
public sealed partial class TimeSyncHandler : GatewayRequestHandlerBase
{
    public override ValueTask<GatewayResponse> HandleAsync(
        GatewayRequest request,
        GatewaySessionContext context,
        CancellationToken cancellationToken)
    {
        var req = WireTimeSyncBinary.DeserializeTimeSyncReq(request.Payload);

        var serverNowTicks = DateTime.UtcNow.Ticks;
        var serverFreq = TimeSpan.TicksPerSecond;

        var res = new WireTimeSyncRes(req.ClientSendTicks, serverNowTicks, serverFreq);
        var respPayload = WireTimeSyncBinary.Serialize(in res);
        return ValueTask.FromResult(GatewayResponse.Ok(request.Seq, respPayload.ToArray()));
    }
}

