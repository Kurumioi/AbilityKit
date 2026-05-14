using AbilityKit.Orleans.Contracts.Battle;
using Orleans;

namespace AbilityKit.Orleans.Gateway.Core;

/// <summary>
/// Gateway 推送目标 Grain 实现
/// 由 Gateway 实现，用于接收来自其他 Grains 的推送请求
/// </summary>
public sealed class GatewayPushTargetGrain : Grain, IGatewayPushTargetGrain
{
    private readonly Abstractions.IGatewaySessionRegistry _sessionRegistry;

    public GatewayPushTargetGrain(Abstractions.IGatewaySessionRegistry sessionRegistry)
    {
        _sessionRegistry = sessionRegistry;
    }

    public Task<bool> PushToAccountAsync(string accountId, uint opCode, byte[] payload)
    {
        return _sessionRegistry.TrySendPushToAccountAsync(accountId, opCode, payload);
    }

    public Task<bool> PushToTokenAsync(string token, uint opCode, byte[] payload)
    {
        return _sessionRegistry.TrySendPushToTokenAsync(token, opCode, payload);
    }
}
