using System.Diagnostics;
using AbilityKit.Orleans.Gateway.TcpGateway.Handler;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AbilityKit.Orleans.Gateway.TcpGateway;

/// <summary>
/// 重构后的请求路由器 - 使用 HandlerRegistry
/// </summary>
public sealed class TcpGatewayRequestRouter
{
    private readonly HandlerRegistry _registry;
    private readonly IOptions<TcpGatewayOptions> _options;
    private readonly ILogger<TcpGatewayRequestRouter> _logger;

    public TcpGatewayRequestRouter(
        HandlerRegistry registry,
        IOptions<TcpGatewayOptions> options,
        ILogger<TcpGatewayRequestRouter> logger)
    {
        _registry = registry;
        _options = options;
        _logger = logger;
    }

    public async Task<Messages.GatewayResponse> RouteAsync(
        TcpClientSessionContext context,
        NetworkPacketHeader header,
        ReadOnlyMemory<byte> payload,
        CancellationToken cancellationToken)
    {
        var handler = _registry.GetHandler(header.OpCode);
        if (handler == null)
        {
            _logger.LogWarning("Unhandled opcode: {OpCode}", header.OpCode);
            return Messages.GatewayResponse.Error(header.Seq, Messages.TcpGatewayStatusCode.UnhandledOpCode);
        }

        var timeoutMs = _options.Value.RequestTimeoutMs;
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        if (timeoutMs > 0) cts.CancelAfter(timeoutMs);

        var request = new Messages.GatewayRequest(header.Seq, payload.ToArray());

        try
        {
            return await handler.HandleAsync(request, context, cts.Token);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning("Request timeout. OpCode={OpCode} Seq={Seq}", header.OpCode, header.Seq);
            return Messages.GatewayResponse.Error(header.Seq, Messages.TcpGatewayStatusCode.Timeout);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Request handler exception. OpCode={OpCode} Seq={Seq}", header.OpCode, header.Seq);
            var errorPayload = System.Text.Encoding.UTF8.GetBytes(ex.ToString());
            return Messages.GatewayResponse.Error(header.Seq, Messages.TcpGatewayStatusCode.Exception, errorPayload);
        }
    }
}
