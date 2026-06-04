using AbilityKit.Orleans.Contracts.Battle;
using AbilityKit.Protocol.Moba.StateSync;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Serialization;

namespace AbilityKit.Orleans.Grains.Battle;

/// <summary>
/// 状态同步 Observer Grain
/// 桥接 BattleLogicHostGrain 和 Gateway，负责向客户端推送状态快照
/// </summary>
public sealed class StateSyncObserverGrain : Grain, IStateSyncObserverGrain, IStateSyncObserver
{
    private readonly ILogger<StateSyncObserverGrain> _logger;
    private readonly Serializer _serializer;

    private string _accountId = string.Empty;
    private string _roomId = string.Empty;
    private bool _subscribed;
    private string _currentBattleKey = string.Empty;

    public StateSyncObserverGrain(
        ILogger<StateSyncObserverGrain> logger,
        Serializer serializer)
    {
        _logger = logger;
        _serializer = serializer;
    }

    public override Task OnActivateAsync(CancellationToken cancellationToken)
    {
        var key = this.GetPrimaryKeyString();
        _logger.LogInformation("[StateSyncObserver] Activated with key: {Key}", key);

        // key 格式: "accountId:roomId"
        var parts = key.Split(':');
        if (parts.Length >= 2)
        {
            _accountId = parts[0];
            _roomId = parts[1];
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// 订阅战斗状态同步
    /// </summary>
    public async Task SubscribeAsync(string battleGrainKey)
    {
        if (_subscribed)
        {
            _logger.LogWarning("[StateSyncObserver] Already subscribed, ignoring duplicate subscription");
            return;
        }

        var battleGrain = GrainFactory.GetGrain<IBattleLogicHostGrain>(battleGrainKey);
        await battleGrain.SubscribeAsync(this);
        _subscribed = true;
        _currentBattleKey = battleGrainKey;

        _logger.LogInformation(
            "[StateSyncObserver] Subscribed to battle: {BattleKey}, Account: {AccountId}",
            battleGrainKey, _accountId);
    }

    /// <summary>
    /// 取消订阅
    /// </summary>
    public async Task UnsubscribeAsync(string battleGrainKey)
    {
        if (!_subscribed)
            return;

        var battleGrain = GrainFactory.GetGrain<IBattleLogicHostGrain>(battleGrainKey);
        await battleGrain.UnsubscribeAsync(this);
        _subscribed = false;
        _currentBattleKey = string.Empty;

        _logger.LogInformation("[StateSyncObserver] Unsubscribed from battle: {BattleKey}", battleGrainKey);
    }

    /// <summary>
    /// 实现 IStateSyncObserver.OnSnapshotPushed
    /// 将快照推送到客户端
    /// </summary>
    async void IStateSyncObserver.OnSnapshotPushed(StateSyncPush push)
    {
        await OnSnapshotPushedAsync(push);
    }

    private async Task OnSnapshotPushedAsync(StateSyncPush push)
    {
        try
        {
            // 使用 Orleans 序列化器
            var payload = _serializer.SerializeToArray(push);

            // 通过 Gateway Push Target Grain 向客户端发送快照
            var gatewayPush = GrainFactory.GetGrain<IGatewayPushTargetGrain>(0);
            var success = await gatewayPush.PushToAccountAsync(_accountId, OpCodes.SnapshotPushed, payload);

            if (!success)
            {
                _logger.LogWarning(
                    "[StateSyncObserver] Failed to push snapshot to account: {AccountId}, Frame: {Frame}",
                    _accountId, push.Frame);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[StateSyncObserver] Error pushing snapshot to account: {AccountId}", _accountId);
        }
    }

    public override async Task OnDeactivateAsync(DeactivationReason reason, CancellationToken cancellationToken)
    {
        if (_subscribed && !string.IsNullOrEmpty(_currentBattleKey))
        {
            var battleGrain = GrainFactory.GetGrain<IBattleLogicHostGrain>(_currentBattleKey);
            await battleGrain.UnsubscribeAsync(this);
        }

        _logger.LogInformation("[StateSyncObserver] Deactivating: {Reason}", reason);
        await base.OnDeactivateAsync(reason, cancellationToken);
    }
}
