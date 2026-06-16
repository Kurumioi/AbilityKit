using System;
using System.Collections.Generic;
using AbilityKit.Core.Pooling;
using AbilityKit.Game.View.Presentation;

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

    public interface IViewRenderBackend
    {
        ViewRenderBackend Backend { get; }
        IViewBinder CreateBinder(IViewShellLoader shellLoader);
    }

    public static class ViewRenderBackendFactory
    {
        public static IViewBinder CreateBinder(
            IViewShellLoader shellLoader,
            ViewRenderBackend backend,
            Func<IViewShellLoader, IViewBinder> gameObjectFactory,
            Func<IViewShellLoader, IViewBinder> dotsFactory = null)
        {
            if (shellLoader == null) throw new ArgumentNullException(nameof(shellLoader));
            if (gameObjectFactory == null) throw new ArgumentNullException(nameof(gameObjectFactory));

            switch (backend)
            {
                case ViewRenderBackend.GameObject:
                    return new GameObjectViewRenderBackend(gameObjectFactory).CreateBinder(shellLoader);
                case ViewRenderBackend.Dots:
                    return new DotsViewRenderBackend(dotsFactory).CreateBinder(shellLoader);
                default:
                    throw new ArgumentOutOfRangeException(nameof(backend), backend, "Unsupported view render backend.");
            }
        }
    }

    public sealed class GameObjectViewRenderBackend : IViewRenderBackend
    {
        private readonly Func<IViewShellLoader, IViewBinder> _binderFactory;

        public GameObjectViewRenderBackend(Func<IViewShellLoader, IViewBinder> binderFactory)
        {
            _binderFactory = binderFactory ?? throw new ArgumentNullException(nameof(binderFactory));
        }

        public ViewRenderBackend Backend => ViewRenderBackend.GameObject;

        public IViewBinder CreateBinder(IViewShellLoader shellLoader)
        {
            if (shellLoader == null) throw new ArgumentNullException(nameof(shellLoader));
            return _binderFactory(shellLoader);
        }
    }

    public sealed class DotsViewRenderBackend : IViewRenderBackend
    {
        private readonly Func<IViewShellLoader, IViewBinder> _binderFactory;

        public DotsViewRenderBackend(Func<IViewShellLoader, IViewBinder> binderFactory = null)
        {
            _binderFactory = binderFactory;
        }

        public ViewRenderBackend Backend => ViewRenderBackend.Dots;

        public IViewBinder CreateBinder(IViewShellLoader shellLoader)
        {
            if (shellLoader == null) throw new ArgumentNullException(nameof(shellLoader));
            return _binderFactory != null ? _binderFactory(shellLoader) : new DotsViewBinder();
        }
    }

    public sealed class DotsViewBinder : IViewBinder
    {
        public void Sync(object entity)
        {
        }

        public void TickInterpolation(float deltaTime)
        {
        }

        public void Clear()
        {
        }

        public void RebindAll()
        {
        }
    }
}