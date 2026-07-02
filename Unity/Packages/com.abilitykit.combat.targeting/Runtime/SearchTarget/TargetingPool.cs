using System.Collections.Generic;
using AbilityKit.Core.Pooling;

namespace AbilityKit.Battle.SearchTarget
{
    /// <summary>
    /// 目标查找模块的统一对象池入口，用于高频目标查询场景。
    /// </summary>
    public static class TargetingPool
    {
        private static readonly ObjectPool<List<SearchHit>> HitListPool = Pools.GetPool(
            createFunc: () => new List<SearchHit>(128),
            onRelease: l => l.Clear(),
            defaultCapacity: 32,
            maxSize: 1024,
            collectionCheck: false);

        private static readonly ObjectPool<List<IEntityId>> EntityIdListPool = Pools.GetPool(
            createFunc: () => new List<IEntityId>(64),
            onRelease: l => l.Clear(),
            defaultCapacity: 32,
            maxSize: 1024,
            collectionCheck: false);

        private static readonly ObjectPool<List<ITargetRule>> RuleListPool = Pools.GetPool(
            createFunc: () => new List<ITargetRule>(4),
            onRelease: l => l.Clear(),
            defaultCapacity: 32,
            maxSize: 1024,
            collectionCheck: false);

        private static readonly ObjectPool<SearchContext> ContextPool = Pools.GetPool(
            createFunc: () => new SearchContext(),
            onGet: c => c.ResetForRent(),
            onRelease: c => c.ResetForRelease(),
            defaultCapacity: 16,
            maxSize: 512,
            collectionCheck: false);

        private static readonly ObjectPool<SearchResult> ResultPool = Pools.GetPool(
            createFunc: () => new SearchResult(),
            onGet: r => r.ResetForRent(),
            onRelease: r => r.ResetForRelease(),
            defaultCapacity: 32,
            maxSize: 1024,
            collectionCheck: false);

        private static readonly ObjectPool<SearchHitBuffer> HitBufferPool = Pools.GetPool(
            createFunc: () => new SearchHitBuffer(),
            onRelease: b => b.Reset(),
            defaultCapacity: 16,
            maxSize: 512,
            collectionCheck: false);

        public static SearchContext RentContext()
        {
            return ContextPool.Get();
        }

        public static void Release(SearchContext context)
        {
            if (context == null) return;
            ContextPool.Release(context);
        }

        public static SearchResult RentResult()
        {
            return ResultPool.Get();
        }

        public static void Release(SearchResult result)
        {
            if (result == null) return;
            ResultPool.Release(result);
        }

        internal static List<SearchHit> RentHitList()
        {
            return HitListPool.Get();
        }

        internal static void ReleaseHitList(List<SearchHit> list)
        {
            if (list == null) return;
            HitListPool.Release(list);
        }

        internal static List<IEntityId> RentEntityIdList()
        {
            return EntityIdListPool.Get();
        }

        internal static void ReleaseEntityIdList(List<IEntityId> list)
        {
            if (list == null) return;
            EntityIdListPool.Release(list);
        }

        internal static List<ITargetRule> RentRuleList()
        {
            return RuleListPool.Get();
        }

        internal static void ReleaseRuleList(List<ITargetRule> list)
        {
            if (list == null) return;
            RuleListPool.Release(list);
        }

        internal static SearchHitBuffer RentHitBuffer(int capacity)
        {
            var buffer = HitBufferPool.Get();
            buffer.EnsureCapacity(capacity);
            return buffer;
        }

        internal static void ReleaseHitBuffer(SearchHitBuffer buffer)
        {
            if (buffer == null) return;
            HitBufferPool.Release(buffer);
        }
    }
}
