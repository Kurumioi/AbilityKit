using System.Collections.Generic;
using System.Numerics;
using AbilityKit.Core.Pooling;

namespace AbilityKit.Game.View
{
    public sealed class ViewBinder : IViewBinder
    {
        private readonly Dictionary<uint, IViewHandle> _handles = new Dictionary<uint, IViewHandle>();
        private readonly IViewShellLoader _shellLoader;
        private readonly ObjectPool<ViewHandle> _handlePool;

        public ViewBinder(IViewShellLoader shellLoader)
        {
            _shellLoader = shellLoader;
            _handlePool = Pools.GetPool<ViewHandle>(
                () => new ViewHandle(),
                onGet: h => { },
                onRelease: h => { },
                defaultCapacity: 64,
                maxSize: 1024
            );
        }

        public void Sync(object entity)
        {
        }

        public void TickInterpolation(float deltaTime)
        {
        }

        public void Clear()
        {
            foreach (var handle in _handles.Values)
            {
                if (handle.Shell != null)
                {
                    _shellLoader.UnloadShell(handle.Shell);
                }
                _handlePool.Release((ViewHandle)handle);
            }
            _handles.Clear();
        }

        public void RebindAll()
        {
            Clear();
        }

        protected IViewHandle AcquireHandle(uint entityId)
        {
            var handle = _handlePool.Get();
            handle.EntityId = entityId;
            return handle;
        }

        protected void ReleaseHandle(IViewHandle handle)
        {
            _handlePool.Release((ViewHandle)handle);
        }
    }
}