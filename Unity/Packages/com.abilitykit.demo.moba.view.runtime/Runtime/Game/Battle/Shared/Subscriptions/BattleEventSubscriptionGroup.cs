using System;
using System.Collections.Generic;
using AbilityKit.Ability.Triggering;
using AbilityKit.Core.Common.Log;

namespace AbilityKit.Game.Flow
{
    internal sealed class BattleEventSubscriptionGroup : IDisposable
    {
        private readonly List<IEventSubscription> _items;
        private bool _isClearing;

        public BattleEventSubscriptionGroup(int capacity = 4)
        {
            _items = new List<IEventSubscription>(capacity);
        }

        public IEventSubscription Add(IEventSubscription subscription)
        {
            if (subscription != null)
            {
                _items.Add(subscription);
            }

            return subscription;
        }

        public void Clear()
        {
            if (_isClearing) return;
            _isClearing = true;

            try
            {
                for (int i = _items.Count - 1; i >= 0; i--)
                {
                    try
                    {
                        _items[i]?.Unsubscribe();
                    }
                    catch (Exception ex)
                    {
                        Log.Exception(ex);
                    }
                }

                _items.Clear();
            }
            finally
            {
                _isClearing = false;
            }
        }

        public void Dispose()
        {
            Clear();
        }
    }
}
