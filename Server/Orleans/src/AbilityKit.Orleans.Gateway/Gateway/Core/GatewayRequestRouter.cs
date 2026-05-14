using AbilityKit.Orleans.Gateway.Abstractions;
using Microsoft.Extensions.Options;

namespace AbilityKit.Orleans.Gateway.Core;

/// <summary>
/// Gateway 请求路由器
/// </summary>
public sealed class GatewayRequestRouter : IGatewayRequestRouter
{
    private readonly IGatewayHandlerRegistry _registry;
    private readonly int _requestTimeoutMs;

    public GatewayRequestRouter(
        IGatewayHandlerRegistry registry,
        IOptions<GatewayOptions> options)
    {
        _registry = registry;
        _requestTimeoutMs = options.Value.RequestTimeoutMs;
    }

    public async Task<GatewayResponse> RouteAsync(
        GatewaySessionContext context,
        uint opCode,
        uint seq,
        byte[] payload,
        CancellationToken cancellationToken)
    {
        var handler = _registry.GetHandler(opCode);
        if (handler == null)
        {
            return GatewayResponse.Error(seq, GatewayStatusCode.UnhandledOpCode);
        }

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        if (_requestTimeoutMs > 0) cts.CancelAfter(_requestTimeoutMs);

        var request = new GatewayRequest(seq, payload);

        try
        {
            return await handler.HandleAsync(request, context, cts.Token);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return GatewayResponse.Error(seq, GatewayStatusCode.Timeout);
        }
        catch (Exception)
        {
            return GatewayResponse.Error(seq, GatewayStatusCode.Exception);
        }
    }
}
