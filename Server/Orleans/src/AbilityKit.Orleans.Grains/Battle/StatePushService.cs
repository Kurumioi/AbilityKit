using AbilityKit.Orleans.Contracts.Battle;
using Microsoft.Extensions.Logging;
using Orleans;

namespace AbilityKit.Orleans.Grains.Battle;

/// <summary>
/// 状态推送服务实现
/// 负责向客户端推送消息
/// </summary>
public sealed class StatePushService : Grain, IStatePushService
{
    private readonly ILogger<StatePushService> _logger;

    public StatePushService(ILogger<StatePushService> logger)
    {
        _logger = logger;
    }

    public Task<bool> PushToAccountAsync(string accountId, uint opCode, byte[] payload)
    {
        // 推送逻辑由 Gateway 通过 IGatewayPushTargetGrain 实现
        // 这里只是一个占位符，实际推送通过 Gateway 进行
        _logger.LogDebug(
            "[StatePushService] PushToAccountAsync called - Account: {AccountId}, OpCode: {OpCode}, Size: {Size}",
            accountId, opCode, payload?.Length ?? 0);

        // 记录推送请求，实际推送由订阅机制处理
        return Task.FromResult(true);
    }

    public Task<bool> PushToTokenAsync(string token, uint opCode, byte[] payload)
    {
        _logger.LogDebug(
            "[StatePushService] PushToTokenAsync called - Token: {Token}, OpCode: {OpCode}, Size: {Size}",
            token, opCode, payload?.Length ?? 0);

        return Task.FromResult(true);
    }
}
