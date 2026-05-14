using Orleans;

namespace AbilityKit.Orleans.Contracts.Battle;

/// <summary>
/// Gateway 推送目标 Grain 接口
/// 由 Gateway 实现，用于接收来自其他 Grains 的推送请求
/// </summary>
public interface IGatewayPushTargetGrain : IGrainWithIntegerKey
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
