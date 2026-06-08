using System;
using System.Collections.Generic;
using AbilityKit.Core.Common.Log;

namespace AbilityKit.Game.Flow
{
    internal sealed class BattleSubscriptionGroup : IDisposable
    {
        private readonly List<IDisposable> _items;
        private bool _isClearing;

        public BattleSubscriptionGroup(int capacity = 4)
        {
            _items = new List<IDisposable>(capacity);
        }

        public T Add<T>(T subscription) where T : class, IDisposable
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
                        _items[i]?.Dispose();
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
