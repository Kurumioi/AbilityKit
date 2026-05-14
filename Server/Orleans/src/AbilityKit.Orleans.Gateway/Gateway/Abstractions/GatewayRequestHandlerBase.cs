using System.Reflection;

namespace AbilityKit.Orleans.Gateway.Abstractions;

/// <summary>
/// Gateway 请求处理器基类，自动从 Attribute 获取 OpCode
/// </summary>
public abstract class GatewayRequestHandlerBase : IGatewayRequestHandler
{
    public uint OpCode { get; }

    protected GatewayRequestHandlerBase()
    {
        var attr = GetType().GetCustomAttribute<Core.GatewayHandlerAttribute>();
        OpCode = attr?.OpCode ?? throw new InvalidOperationException($"Handler {GetType().Name} missing GatewayHandlerAttribute.");
    }

    public abstract ValueTask<GatewayResponse> HandleAsync(
        GatewayRequest request,
        GatewaySessionContext context,
        CancellationToken cancellationToken);
}
