using System;
using AbilityKit.Effect;
using AbilityKit.Ability.Share.Effect;
using AbilityKit.Demo.Moba.Services;
using AbilityKit.Ability.Triggering;
using AbilityKit.Game.Flow;

namespace AbilityKit.Game.Flow.Battle.ViewEvents.Triggering
{
    public sealed class BattleTriggerEventViewBridge : IEventHandler, IDisposable
    {
        private readonly IEventBus _bus;
        private readonly IBattleViewEventSink _sink;
        private readonly BattleEventSubscriptionGroup _subscriptions = new BattleEventSubscriptionGroup(8);
        private bool _disposed;

        public BattleTriggerEventViewBridge(IEventBus bus, IBattleViewEventSink sink)
        {
            _bus = bus;
            _sink = sink;

            if (_bus == null) return;

            _subscriptions.Add(_bus.Subscribe(DamagePipelineEvents.AfterApply, this));

            _subscriptions.Add(_bus.Subscribe(MobaBuffTriggering.Events.ApplyOrRefresh, this));
            _subscriptions.Add(_bus.Subscribe(MobaBuffTriggering.Events.Remove, this));

            _subscriptions.Add(_bus.Subscribe(AreaTriggering.Events.Spawn, this));
            _subscriptions.Add(_bus.Subscribe(AreaTriggering.Events.Enter, this));
            _subscriptions.Add(_bus.Subscribe(AreaTriggering.Events.Exit, this));
            _subscriptions.Add(_bus.Subscribe(AreaTriggering.Events.Expire, this));

            _subscriptions.Add(_bus.Subscribe(ProjectileTriggering.Events.Hit, this));
        }

        public void Handle(in TriggerEvent evt)
        {
            _sink?.OnTriggerEvent(in evt);
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            _subscriptions.Clear();
        }
    }
}
