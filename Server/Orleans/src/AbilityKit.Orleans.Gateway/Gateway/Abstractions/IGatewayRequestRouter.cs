namespace AbilityKit.Orleans.Gateway.Abstractions;

/// <summary>
/// Gateway 请求路由器接口
/// </summary>
public interface IGatewayRequestRouter
{
    /// <summary>
    /// 路由请求到对应的 Handler
    /// </summary>
    Task<GatewayResponse> RouteAsync(
        GatewaySessionContext context,
        uint opCode,
        uint seq,
        byte[] payload,
        CancellationToken cancellationToken);
}
