using System;
using System.Collections.Generic;

namespace AbilityKit.Core.Pooling
{
    public static class Pools
    {
        public static PoolScope GlobalScope => PoolRegistry.Global;

        public static PoolScope GetOrCreateScope(string name, bool destroyOnDispose = true)
        {
            return PoolRegistry.GetOrCreateScope(name, destroyOnDispose);
        }

        public static bool TryGetScope(string name, out PoolScope scope)
        {
            return PoolRegistry.TryGetScope(name, out scope);
        }

        public static bool DestroyScope(string name, bool destroy = true)
        {
            return PoolRegistry.DestroyScope(name, destroy);
        }

        public static ObjectPool<T> GetPool<T>(Func<T> createFunc, Action<T> onGet = null, Action<T> onRelease = null, Action<T> onDestroy = null, int defaultCapacity = 0, int maxSize = 1024, bool collectionCheck = true, PoolTrimPolicy trimPolicy = default) where T : class
        {
            return GetPool(PoolKey.Default, createFunc, onGet, onRelease, onDestroy, defaultCapacity, maxSize, collectionCheck, trimPolicy);
        }

        public static ObjectPool<T> GetPool<T>(PoolKey key, Func<T> createFunc, Action<T> onGet = null, Action<T> onRelease = null, Action<T> onDestroy = null, int defaultCapacity = 0, int maxSize = 1024, bool collectionCheck = true, PoolTrimPolicy trimPolicy = default) where T : class
        {
            return PoolRegistry.Global.GetPool(key, createFunc, onGet, onRelease, onDestroy, defaultCapacity, maxSize, collectionCheck, trimPolicy);
        }

        public static T Get<T>(Func<T> createFunc, Action<T> onGet = null, Action<T> onRelease = null, Action<T> onDestroy = null, int defaultCapacity = 0, int maxSize = 1024, bool collectionCheck = true, PoolTrimPolicy trimPolicy = default) where T : class
        {
            return Get(PoolKey.Default, createFunc, onGet, onRelease, onDestroy, defaultCapacity, maxSize, collectionCheck, trimPolicy);
        }

        public static T Get<T>(PoolKey key, Func<T> createFunc, Action<T> onGet = null, Action<T> onRelease = null, Action<T> onDestroy = null, int defaultCapacity = 0, int maxSize = 1024, bool collectionCheck = true, PoolTrimPolicy trimPolicy = default) where T : class
        {
            return GetPool(key, createFunc, onGet, onRelease, onDestroy, defaultCapacity, maxSize, collectionCheck, trimPolicy).Get();
        }

        public static PooledObject<T> GetPooled<T>(Func<T> createFunc, Action<T> onGet = null, Action<T> onRelease = null, Action<T> onDestroy = null, int defaultCapacity = 0, int maxSize = 1024, bool collectionCheck = true, PoolTrimPolicy trimPolicy = default) where T : class
        {
            return GetPooled(PoolKey.Default, createFunc, onGet, onRelease, onDestroy, defaultCapacity, maxSize, collectionCheck, trimPolicy);
        }

        public static PooledObject<T> GetPooled<T>(PoolKey key, Func<T> createFunc, Action<T> onGet = null, Action<T> onRelease = null, Action<T> onDestroy = null, int defaultCapacity = 0, int maxSize = 1024, bool collectionCheck = true, PoolTrimPolicy trimPolicy = default) where T : class
        {
            return GetPool(key, createFunc, onGet, onRelease, onDestroy, defaultCapacity, maxSize, collectionCheck, trimPolicy).GetPooled();
        }

        public static void Release<T>(T element) where T : class
        {
            Release(PoolKey.Default, element);
        }

        public static void Release<T>(PoolKey key, T element) where T : class
        {
            PoolRegistry.Global.Release(key, element);
        }

        public static bool TryRelease<T>(T element) where T : class
        {
            return TryRelease(PoolKey.Default, element);
        }

        public static bool TryRelease<T>(PoolKey key, T element) where T : class
        {
            return PoolRegistry.Global.TryRelease(key, element);
        }

        public static void Release(object element)
        {
            PoolRegistry.Global.Release(element);
        }

        public static bool TryRelease(object element)
        {
            return PoolRegistry.Global.TryRelease(element);
        }

        public static bool DestroyPool<T>(bool destroy = true) where T : class
        {
            return DestroyPool<T>(PoolKey.Default, destroy);
        }

        public static bool DestroyPool<T>(PoolKey key, bool destroy = true) where T : class
        {
            return PoolRegistry.Global.DestroyPool<T>(key, destroy);
        }

        public static int TrimAll()
        {
            return PoolRegistry.Global.TrimAll();
        }

        public static int TrimAll(PoolTrimPolicy policy)
        {
            return PoolRegistry.Global.TrimAll(policy);
        }

        public static void ClearAll(bool destroy = false)
        {
            PoolRegistry.Global.Clear(destroy);
        }

#if UNITY_EDITOR
        public static IReadOnlyList<PoolDebugSnapshot> GetDebugSnapshots()
        {
            return PoolRegistry.Global.GetDebugSnapshots();
        }
#endif
    }
}
