namespace AbilityKit.Orleans.Contracts.Battle;

/// <summary>
/// 状态同步观察者 Grain 接口
/// 桥接 BattleLogicHostGrain 和 Gateway
/// </summary>
public interface IStateSyncObserverGrain : IGrainWithStringKey
{
    /// <summary>
    /// 获取观察者上下文元信息。
    /// </summary>
    Task<StateSyncObserverInfo> GetObserverInfoAsync();

    /// <summary>
    /// 订阅战斗状态同步
    /// </summary>
    Task SubscribeAsync(string battleGrainKey);

    /// <summary>
    /// 取消订阅
    /// </summary>
    Task UnsubscribeAsync(string battleGrainKey);

    /// <summary>
    /// 接收战斗状态快照推送。
    /// </summary>
    Task OnSnapshotPushedAsync(StateSyncPush push);
}

[GenerateSerializer]
public sealed class StateSyncObserverInfo
{
    [Id(0)] public string ObserverKey { get; set; } = string.Empty;
    [Id(1)] public string AccountId { get; set; } = string.Empty;
    [Id(2)] public string RoomId { get; set; } = string.Empty;
}
