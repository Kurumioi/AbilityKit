namespace AbilityKit.Orleans.Contracts.Battle;

/// <summary>
/// 状态同步观察者 Grain 接口
/// 桥接 BattleLogicHostGrain 和 Gateway
/// </summary>
public interface IStateSyncObserverGrain : IGrainWithStringKey
{
    /// <summary>
    /// 订阅战斗状态同步
    /// </summary>
    Task SubscribeAsync(string battleGrainKey);

    /// <summary>
    /// 取消订阅
    /// </summary>
    Task UnsubscribeAsync(string battleGrainKey);
}
