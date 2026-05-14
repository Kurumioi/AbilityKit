namespace AbilityKit.Orleans.Gateway.Abstractions;

/// <summary>
/// Handler 注册表接口
/// </summary>
public interface IGatewayHandlerRegistry
{
    /// <summary>
    /// 获取指定 OpCode 的 Handler
    /// </summary>
    IGatewayRequestHandler? GetHandler(uint opCode);

    /// <summary>
    /// 注册 Handler
    /// </summary>
    void Register(uint opCode, IGatewayRequestHandler handler);

    /// <summary>
    /// 获取所有已注册的 Handler
    /// </summary>
    IReadOnlyDictionary<uint, IGatewayRequestHandler> GetAllHandlers();
}
