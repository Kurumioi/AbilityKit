using System;

namespace AbilityKit.Core.Pooling
{
    public readonly struct PoolDebugSnapshot
    {
        public readonly Type ElementType;
        public readonly PoolKey Key;
        public readonly PoolStats Stats;
        public readonly int MaxSize;
        public readonly bool NeverTrim;

        public PoolDebugSnapshot(Type elementType, PoolKey key, PoolStats stats, int maxSize)
            : this(elementType, key, stats, maxSize, neverTrim: false)
        {
        }

        public PoolDebugSnapshot(Type elementType, PoolKey key, PoolStats stats, int maxSize, bool neverTrim)
        {
            ElementType = elementType;
            Key = key;
            Stats = stats;
            MaxSize = maxSize;
            NeverTrim = neverTrim;
        }
    }
}
