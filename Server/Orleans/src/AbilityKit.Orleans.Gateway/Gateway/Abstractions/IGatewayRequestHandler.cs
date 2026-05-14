namespace AbilityKit.Orleans.Gateway.Abstractions;

/// <summary>
/// Gateway 请求处理器接口
/// </summary>
public interface IGatewayRequestHandler
{
    /// <summary>
    /// 处理的 OpCode
    /// </summary>
    uint OpCode { get; }

    /// <summary>
    /// 处理请求
    /// </summary>
    ValueTask<GatewayResponse> HandleAsync(
        GatewayRequest request,
        GatewaySessionContext context,
        CancellationToken cancellationToken);
}
