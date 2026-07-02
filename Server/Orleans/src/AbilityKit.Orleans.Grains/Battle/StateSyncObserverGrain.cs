using AbilityKit.Orleans.Contracts.Battle;
using AbilityKit.Protocol.Room;
using Microsoft.Extensions.Logging;
using Orleans;

namespace AbilityKit.Orleans.Grains.Battle;

/// <summary>
/// 状态同步 Observer Grain
/// 桥接 BattleLogicHostGrain 和 Gateway，负责向客户端推送状态快照
/// </summary>
public sealed class StateSyncObserverGrain : Grain, IStateSyncObserverGrain
{
    private readonly ILogger<StateSyncObserverGrain> _logger;

    private readonly StateSyncObserverSubscriptionState _subscriptionState = new();
    private string _accountId = string.Empty;
    private string _roomId = string.Empty;

    public StateSyncObserverGrain(ILogger<StateSyncObserverGrain> logger)
    {
        _logger = logger;
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
        var battleGrain = GrainFactory.GetGrain<IBattleLogicHostGrain>(battleGrainKey);
        var decision = _subscriptionState.DecideSubscribe(battleGrainKey);
        if (decision.Action == StateSyncObserverSubscriptionAction.RefreshFullSnapshot)
        {
            _logger.LogInformation(
                "[StateSyncObserver] Duplicate subscription refreshed full snapshot. Battle: {BattleKey}, Account: {AccountId}",
                battleGrainKey,
                _accountId);
            await battleGrain.RequestFullSnapshotAsync(this);
            return;
        }

        if (decision.Action == StateSyncObserverSubscriptionAction.SwitchBattle)
        {
            await UnsubscribeBattleAsync(decision.PreviousBattleKey, "switch battle subscription", logFailureAsWarning: false);
            battleGrain = GrainFactory.GetGrain<IBattleLogicHostGrain>(battleGrainKey);
        }

        await battleGrain.SubscribeAsync(this);
        _subscriptionState.MarkSubscribed(battleGrainKey);

        _logger.LogInformation(
            "[StateSyncObserver] Subscribed to battle: {BattleKey}, Account: {AccountId}",
            battleGrainKey, _accountId);
    }

    /// <summary>
    /// 取消订阅
    /// </summary>
    public Task UnsubscribeAsync(string battleGrainKey)
    {
        return UnsubscribeBattleAsync(battleGrainKey, "explicit unsubscribe", logFailureAsWarning: true);
    }

    /// <summary>
    /// 接收 BattleLogicHostGrain 的状态快照推送。
    /// </summary>
    public async Task OnSnapshotPushedAsync(StateSyncPush push)
    {
        try
        {
            var wire = ToWireSnapshotPush(push);
            var payload = WireRoomGatewayBinary.Serialize(in wire);

            var gatewayPush = GrainFactory.GetGrain<IGatewayPushTargetGrain>(0);
            var opCode = push.IsFullSnapshot ? RoomGatewayOpCodes.SnapshotPushed : RoomGatewayOpCodes.DeltaSnapshotPushed;
            var success = await gatewayPush.PushToAccountAsync(_accountId, opCode, payload.ToArray());

            if (!success)
            {
                _logger.LogWarning(
                    "[StateSyncObserver] Snapshot push target is offline. Account: {AccountId}, Frame: {Frame}, FullSnapshot: {FullSnapshot}",
                    _accountId,
                    push.Frame,
                    push.IsFullSnapshot);
                await UnsubscribeCurrentBattleAsync("gateway push target offline");
                throw new InvalidOperationException($"Snapshot push target is offline. Account={_accountId}, Frame={push.Frame}, FullSnapshot={push.IsFullSnapshot}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[StateSyncObserver] Error pushing snapshot to account: {AccountId}", _accountId);
            throw;
        }
    }

    private static WireStateSyncSnapshotPush ToWireSnapshotPush(StateSyncPush push)
    {
        var source = push.Actors;
        var actors = source == null || source.Count == 0
            ? null
            : new List<WireStateSyncActorSnapshot>(source.Count);

        if (actors != null && source != null)
        {
            foreach (var actor in source)
            {
                actors.Add(new WireStateSyncActorSnapshot
                {
                    ActorId = actor.ActorId,
                    X = actor.X,
                    Y = actor.Y,
                    Z = actor.Z,
                    Rotation = actor.Rotation,
                    VelocityX = actor.VelocityX,
                    VelocityZ = actor.VelocityZ,
                    Hp = actor.Hp,
                    HpMax = actor.HpMax,
                    TeamId = actor.TeamId
                });
            }
        }

        return new WireStateSyncSnapshotPush
        {
            WorldId = push.WorldId,
            Frame = push.Frame,
            Timestamp = push.Timestamp,
            IsFullSnapshot = push.IsFullSnapshot,
            Actors = actors,
            PayloadOpCode = push.PayloadOpCode,
            Payload = push.Payload,
            ServerTicks = push.ServerTicks
        };
    }

    private Task UnsubscribeCurrentBattleAsync(string reason)
    {
        return UnsubscribeBattleAsync(_subscriptionState.CurrentBattleKey, reason, logFailureAsWarning: false);
    }

    private async Task UnsubscribeBattleAsync(string battleGrainKey, string reason, bool logFailureAsWarning)
    {
        if (!_subscriptionState.IsSubscribed || string.IsNullOrWhiteSpace(battleGrainKey))
        {
            return;
        }

        try
        {
            var battleGrain = GrainFactory.GetGrain<IBattleLogicHostGrain>(battleGrainKey);
            await battleGrain.UnsubscribeAsync(this);
            _logger.LogInformation(
                "[StateSyncObserver] Unsubscribed from battle: {BattleKey}, Reason: {Reason}",
                battleGrainKey,
                reason);
        }
        catch (Exception ex) when (!logFailureAsWarning)
        {
            _logger.LogDebug(
                ex,
                "[StateSyncObserver] Ignored unsubscribe failure. BattleKey: {BattleKey}, Reason: {Reason}",
                battleGrainKey,
                reason);
        }
        finally
        {
            _subscriptionState.Clear();
        }
    }

    public override async Task OnDeactivateAsync(DeactivationReason reason, CancellationToken cancellationToken)
    {
        await UnsubscribeCurrentBattleAsync($"deactivate: {reason}");

        _logger.LogInformation("[StateSyncObserver] Deactivating: {Reason}", reason);
        await base.OnDeactivateAsync(reason, cancellationToken);
    }
}
