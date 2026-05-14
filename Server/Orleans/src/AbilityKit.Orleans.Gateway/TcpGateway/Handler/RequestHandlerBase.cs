namespace AbilityKit.Orleans.Gateway.TcpGateway.Handler;

/// <summary>
/// Gateway 请求处理器基类
/// </summary>
public abstract class RequestHandlerBase
{
    public abstract uint OpCode { get; }

    public abstract ValueTask<Messages.GatewayResponse> HandleAsync(
        Messages.GatewayRequest request,
        TcpClientSessionContext context,
        CancellationToken cancellationToken);
}
