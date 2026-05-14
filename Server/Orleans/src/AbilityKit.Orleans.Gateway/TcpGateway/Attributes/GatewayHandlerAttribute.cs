namespace AbilityKit.Orleans.Gateway.TcpGateway;

/// <summary>
/// 标记一个类为 Gateway 请求处理器
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class GatewayHandlerAttribute : Attribute
{
    public uint OpCode { get; }

    public GatewayHandlerAttribute(uint opCode)
    {
        OpCode = opCode;
    }
}
