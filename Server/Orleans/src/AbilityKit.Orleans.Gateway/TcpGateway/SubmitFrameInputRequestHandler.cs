using AbilityKit.Orleans.Contracts.FrameSync;
using AbilityKit.Orleans.Gateway.TcpGateway.Handler;
using AbilityKit.Protocol.Moba.Generated.GatewayFrameSync;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans;

namespace AbilityKit.Orleans.Gateway.TcpGateway;

[GatewayHandler(200)]
public sealed class SubmitFrameInputRequestHandler : RequestHandlerBase
{
    private static readonly bool EnableVerboseFrameInputLog = false;

    private readonly IOptions<TcpGatewayOptions> _options;
    private readonly ITcpGatewaySessionRegistry _registry;
    private readonly IClusterClient _clusterClient;
    private readonly FrameSyncObserverHub _observerHub;
    private readonly ILogger<SubmitFrameInputRequestHandler> _logger;

    public override uint OpCode => _options.Value.SubmitFrameInputOpCode;

    public SubmitFrameInputRequestHandler(
        IOptions<TcpGatewayOptions> options,
        ITcpGatewaySessionRegistry registry,
        IClusterClient clusterClient,
        FrameSyncObserverHub observerHub,
        ILogger<SubmitFrameInputRequestHandler> logger)
    {
        _options = options;
        _registry = registry;
        _clusterClient = clusterClient;
        _observerHub = observerHub;
        _logger = logger;
    }

    public override async ValueTask<Messages.GatewayResponse> HandleAsync(
        Messages.GatewayRequest request,
        TcpClientSessionContext context,
        CancellationToken cancellationToken)
    {
        try
        {
            var req = WireCustomBinary.DeserializeSubmitFrameInputReq(request.Payload);

            if (EnableVerboseFrameInputLog)
            {
                _logger.LogDebug(
                    "SubmitFrameInput received. Conn={ConnectionId} OpCode={OpCode} Seq={Seq} RoomId={RoomId} WorldId={WorldId} PlayerId={PlayerId} Frame={Frame} InputOpCode={InputOpCode} PayloadLen={PayloadLen}",
                    context.ConnectionId,
                    request.Seq,
                    request.Seq,
                    req.RoomId,
                    req.WorldId,
                    req.PlayerId,
                    req.Frame,
                    req.InputOpCode,
                    req.InputPayload.Length);
            }

            if (_clusterClient != null && _observerHub != null)
            {
                await _observerHub.EnsureSubscribedAsync(req.RoomId, cancellationToken);

                var grain = _clusterClient.GetGrain<IBattleFrameSyncGrain>(req.RoomId.ToString());
                await grain.SubmitInputAsync(
                    worldId: req.WorldId,
                    frame: req.Frame,
                    input: new FrameInputItem(req.PlayerId, req.InputOpCode, req.InputPayload));
            }

            var resp = new WireSubmitFrameInputRes(accepted: true, serverFrame: 0, reasonCode: 0);
            var respPayload = WireCustomBinary.Serialize(in resp);
            return Messages.GatewayResponse.Ok(request.Seq, respPayload);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SubmitFrameInput handler exception. Conn={ConnectionId} OpCode={OpCode} Seq={Seq}",
                context.ConnectionId, request.Seq, request.Seq);
            var errorPayload = System.Text.Encoding.UTF8.GetBytes(ex.ToString());
            return Messages.GatewayResponse.Error(request.Seq, Messages.TcpGatewayStatusCode.Exception, errorPayload);
        }
    }
}
