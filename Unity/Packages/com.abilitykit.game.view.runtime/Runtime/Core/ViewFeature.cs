using System;
using AbilityKit.Game.View.Presentation;

namespace AbilityKit.Game.View
{
    public abstract class ViewFeature
    {
        protected IViewBinder _binder;
        protected IViewShellLoader _shellLoader;

        public virtual void Initialize(IViewShellLoader shellLoader)
        {
            Initialize(shellLoader, ViewRenderBackend.GameObject);
        }

        public virtual void Initialize(IViewShellLoader shellLoader, ViewRenderBackend backend)
        {
            if (shellLoader == null) throw new ArgumentNullException(nameof(shellLoader));

            _shellLoader = shellLoader;
            Backend = backend;
            _binder = ViewRenderBackendFactory.CreateBinder(shellLoader, backend, CreateBinder, CreateDotsBinder);
        }

        protected abstract IViewBinder CreateBinder(IViewShellLoader shellLoader);

        protected virtual IViewBinder CreateDotsBinder(IViewShellLoader shellLoader)
        {
            return new DotsViewBinder();
        }

        public virtual void Tick(float deltaTime)
        {
            _binder?.TickInterpolation(deltaTime);
        }

        public virtual void Shutdown()
        {
            _binder?.Clear();
            _binder = null;
            _shellLoader = null;
            Backend = ViewRenderBackend.GameObject;
        }

        public IViewBinder Binder => _binder;

        public ViewRenderBackend Backend { get; private set; }
    }
}