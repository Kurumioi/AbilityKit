namespace AbilityKit.Orleans.Gateway.Abstractions;

/// <summary>
/// Gateway 请求处理器基类，OpCode 由生成期注入的派生类型属性提供
/// </summary>
public abstract class GatewayRequestHandlerBase : IGatewayRequestHandler
{
    public abstract uint OpCode { get; }

    public abstract ValueTask<GatewayResponse> HandleAsync(
        GatewayRequest request,
        GatewaySessionContext context,
        CancellationToken cancellationToken);
}
