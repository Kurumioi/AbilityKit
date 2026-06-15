using System;
using AbilityKit.Ability.Triggering;
using AbilityKit.Core.Logging;
using AbilityKit.Game.View.Foundation;

namespace AbilityKit.Game.Flow
{
    internal sealed class BattleEventSubscriptionGroup : IDisposable
    {
        private readonly SubscriptionGroup<IEventSubscription> _inner;

        public BattleEventSubscriptionGroup(int capacity = 4)
        {
            _inner = new SubscriptionGroup<IEventSubscription>(
                subscription => subscription.Unsubscribe(),
                ex => Log.Exception(ex),
                capacity);
        }

        public IEventSubscription Add(IEventSubscription subscription)
        {
            _inner.Add(subscription);
            return subscription;
        }

        public void Clear()
        {
            _inner.Clear();
        }

        public void Dispose()
        {
            _inner.Dispose();
        }
    }
}
