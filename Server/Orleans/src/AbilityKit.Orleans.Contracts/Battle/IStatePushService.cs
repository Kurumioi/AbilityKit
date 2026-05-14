namespace AbilityKit.Orleans.Contracts.Battle;

/// <summary>
/// 状态推送服务接口
/// 允许 Grains 向客户端推送消息
/// </summary>
public interface IStatePushService
{
    /// <summary>
    /// 向指定账号推送消息
    /// </summary>
    Task<bool> PushToAccountAsync(string accountId, uint opCode, byte[] payload);

    /// <summary>
    /// 向指定 Token 推送消息
    /// </summary>
    Task<bool> PushToTokenAsync(string token, uint opCode, byte[] payload);
}
