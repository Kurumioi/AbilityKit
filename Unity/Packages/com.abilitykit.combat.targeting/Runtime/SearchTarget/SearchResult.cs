using System;
using System.Collections.Generic;

namespace AbilityKit.Battle.SearchTarget
{
    /// <summary>
    /// 池化目标查找结果。使用完成后调用释放方法，或通过目标查找对象池归还。
    /// </summary>
    public sealed class SearchResult : IDisposable
    {
        internal readonly List<IEntityId> MutableIds = TargetingPool.RentEntityIdList();

        private bool _disposed;

        public IReadOnlyList<IEntityId> Ids => MutableIds;
        public int Count => MutableIds.Count;

        public IEntityId this[int index] => MutableIds[index];

        public void CopyTo(List<IEntityId> results)
        {
            if (results == null) return;
            results.Clear();
            for (int i = 0; i < MutableIds.Count; i++)
            {
                results.Add(MutableIds[i]);
            }
        }

        public void Clear()
        {
            MutableIds.Clear();
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            TargetingPool.Release(this);
        }

        internal void ResetForRent()
        {
            _disposed = false;
            MutableIds.Clear();
        }

        internal void ResetForRelease()
        {
            _disposed = true;
            MutableIds.Clear();
        }
    }
}
