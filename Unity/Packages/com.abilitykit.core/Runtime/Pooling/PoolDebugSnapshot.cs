using System;

namespace AbilityKit.Core.Pooling
{
    public readonly struct PoolDebugSnapshot
    {
        public readonly Type ElementType;
        public readonly PoolKey Key;
        public readonly PoolStats Stats;
        public readonly int MaxSize;

        public PoolDebugSnapshot(Type elementType, PoolKey key, PoolStats stats, int maxSize)
        {
            ElementType = elementType;
            Key = key;
            Stats = stats;
            MaxSize = maxSize;
        }
    }
}
