using System;
using System.Collections.Generic;
using AbilityKit.Demo.Moba.Console.Platform;

namespace AbilityKit.Demo.Moba.Console.Events
{
    /// <summary>
    /// 静态事件总线
    /// 用于模块间的解耦通信
    /// </summary>
    public static class BattleEventBus
    {
        private static readonly Dictionary<Type, List<Delegate>> _handlers = new();
        private static readonly object _lock = new();

        /// <summary>
        /// 订阅事件
        /// </summary>
        public static void Subscribe<T>(Action<T> handler) where T : struct
        {
            lock (_lock)
            {
                var type = typeof(T);
                if (!_handlers.TryGetValue(type, out var list))
                {
                    list = new List<Delegate>();
                    _handlers[type] = list;
                }
                list.Add(handler);
            }
        }

        /// <summary>
        /// 取消订阅
        /// </summary>
        public static void Unsubscribe<T>(Action<T> handler) where T : struct
        {
            lock (_lock)
            {
                var type = typeof(T);
                if (_handlers.TryGetValue(type, out var list))
                {
                    list.Remove(handler);
                    if (list.Count == 0)
                    {
                        _handlers.Remove(type);
                    }
                }
            }
        }

        /// <summary>
        /// 发布事件
        /// </summary>
        public static void Publish<T>(in T evt) where T : struct
        {
            List<Delegate> handlersCopy;
            lock (_lock)
            {
                if (!_handlers.TryGetValue(typeof(T), out var list))
                {
                    return;
                }
                handlersCopy = new List<Delegate>(list);
            }

            foreach (var handler in handlersCopy)
            {
                try
                {
                    if (handler is Action<T> action)
                    {
                        action(evt);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Event handler exception: {ex}");
                    Log.Error($"[EventBus] Handler exception: {ex.Message}\n{ex.StackTrace}");
                }
            }
        }

        /// <summary>
        /// 清除所有事件订阅
        /// </summary>
        public static void Clear()
        {
            lock (_lock)
            {
                _handlers.Clear();
            }
        }

        /// <summary>
        /// 获取指定事件的订阅数量
        /// </summary>
        public static int GetHandlerCount<T>() where T : struct
        {
            lock (_lock)
            {
                if (_handlers.TryGetValue(typeof(T), out var list))
                {
                    return list.Count;
                }
                return 0;
            }
        }
    }
}
