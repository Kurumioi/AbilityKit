namespace AbilityKit.Orleans.Gateway.Abstractions;

/// <summary>
/// Gateway 传输层会话接口
/// </summary>
public interface IGatewayTransportSession
{
    /// <summary>
    /// 会话 ID
    /// </summary>
    long ConnectionId { get; }

    /// <summary>
    /// 是否已连接
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    /// 发送服务器推送
    /// </summary>
    Task SendServerPushAsync(uint opCode, byte[] payload, CancellationToken cancellationToken = default);
}
