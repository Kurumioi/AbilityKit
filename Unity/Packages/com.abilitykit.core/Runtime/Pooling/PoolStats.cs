namespace AbilityKit.Core.Pooling
{
    public readonly struct PoolStats
    {
        public readonly int CreatedTotal;
        public readonly int GetTotal;
        public readonly int ReleaseTotal;
        public readonly int InactiveCount;
        public readonly int ActiveCount;
        public readonly int PeakActiveCount;
        public readonly int HitCount;
        public readonly int MissCount;
        public readonly int OverflowDestroyCount;
        public readonly int ClearDestroyCount;
        public readonly int DroppedInactiveCount;
        public readonly int TrimDestroyCount;

        public PoolStats(int createdTotal, int getTotal, int releaseTotal, int inactiveCount, int activeCount)
            : this(createdTotal, getTotal, releaseTotal, inactiveCount, activeCount, activeCount, 0, createdTotal, 0, 0, 0, 0)
        {
        }

        public PoolStats(
            int createdTotal,
            int getTotal,
            int releaseTotal,
            int inactiveCount,
            int activeCount,
            int peakActiveCount,
            int hitCount,
            int missCount,
            int overflowDestroyCount,
            int clearDestroyCount,
            int droppedInactiveCount,
            int trimDestroyCount)
        {
            CreatedTotal = createdTotal;
            GetTotal = getTotal;
            ReleaseTotal = releaseTotal;
            InactiveCount = inactiveCount;
            ActiveCount = activeCount;
            PeakActiveCount = peakActiveCount;
            HitCount = hitCount;
            MissCount = missCount;
            OverflowDestroyCount = overflowDestroyCount;
            ClearDestroyCount = clearDestroyCount;
            DroppedInactiveCount = droppedInactiveCount;
            TrimDestroyCount = trimDestroyCount;
        }
    }
}
