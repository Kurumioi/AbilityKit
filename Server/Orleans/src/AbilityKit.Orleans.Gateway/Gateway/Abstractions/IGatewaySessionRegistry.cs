namespace AbilityKit.Orleans.Gateway.Abstractions;

/// <summary>
/// Gateway 会话注册表接口
/// </summary>
public interface IGatewaySessionRegistry
{
    /// <summary>
    /// 注册会话
    /// </summary>
    void Register(long connectionId, IGatewayTransportSession session);

    /// <summary>
    /// 取消注册会话
    /// </summary>
    void Unregister(long connectionId);

    /// <summary>
    /// 根据连接 ID 获取会话
    /// </summary>
    bool TryGetSession(long connectionId, out IGatewayTransportSession? session);

    /// <summary>
    /// 根据 Token 获取连接 ID
    /// </summary>
    bool TryGetConnectionIdByToken(string token, out long connectionId);

    /// <summary>
    /// 根据账号获取连接 ID
    /// </summary>
    bool TryGetConnectionIdByAccount(string accountId, out long connectionId);

    /// <summary>
    /// 绑定 Token
    /// </summary>
    void BindToken(string token, long connectionId);

    /// <summary>
    /// 解绑 Token
    /// </summary>
    void UnbindToken(string token);

    /// <summary>
    /// 绑定账号
    /// </summary>
    void BindAccount(string accountId, long connectionId);

    /// <summary>
    /// 解绑账号
    /// </summary>
    void UnbindAccount(string accountId);

    /// <summary>
    /// 尝试发送踢出消息
    /// </summary>
    Task<bool> TrySendKickAsync(string token, string reason, CancellationToken cancellationToken);

    /// <summary>
    /// 根据账号发送 ServerPush 消息
    /// </summary>
    Task<bool> TrySendPushToAccountAsync(string accountId, uint opCode, byte[] payload, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据 Token 发送 ServerPush 消息
    /// </summary>
    Task<bool> TrySendPushToTokenAsync(string token, uint opCode, byte[] payload, CancellationToken cancellationToken = default);
}
