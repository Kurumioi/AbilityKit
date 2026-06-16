using System;

namespace AbilityKit.Game.View.Presentation
{
    public enum ViewRenderBackend
    {
        GameObject = 0,
        Dots = 1,
    }

    public interface IViewRenderBackend<TViewBatch>
        where TViewBatch : struct, IViewBatch
    {
        ViewRenderBackend Backend { get; }
        IViewBinder<TViewBatch> CreateBinder(IViewShellLoader shellLoader);
    }

    public static class ViewRenderBackendFactory<TViewBatch>
        where TViewBatch : struct, IViewBatch
    {
        public static IViewBinder<TViewBatch> CreateBinder(
            IViewShellLoader shellLoader,
            ViewRenderBackend backend,
            Func<IViewShellLoader, IViewBinder<TViewBatch>> gameObjectFactory,
            Func<IViewShellLoader, IViewBinder<TViewBatch>>? dotsFactory = null)
        {
            if (shellLoader == null) throw new ArgumentNullException(nameof(shellLoader));
            if (gameObjectFactory == null) throw new ArgumentNullException(nameof(gameObjectFactory));

            return backend switch
            {
                ViewRenderBackend.GameObject => new GameObjectViewRenderBackend<TViewBatch>(gameObjectFactory).CreateBinder(shellLoader),
                ViewRenderBackend.Dots => new DotsViewRenderBackend<TViewBatch>(dotsFactory).CreateBinder(shellLoader),
                _ => throw new ArgumentOutOfRangeException(nameof(backend), backend, "Unsupported view render backend."),
            };
        }
    }

    public sealed class GameObjectViewRenderBackend<TViewBatch> : IViewRenderBackend<TViewBatch>
        where TViewBatch : struct, IViewBatch
    {
        private readonly Func<IViewShellLoader, IViewBinder<TViewBatch>> _binderFactory;

        public GameObjectViewRenderBackend(Func<IViewShellLoader, IViewBinder<TViewBatch>> binderFactory)
        {
            _binderFactory = binderFactory ?? throw new ArgumentNullException(nameof(binderFactory));
        }

        public ViewRenderBackend Backend => ViewRenderBackend.GameObject;

        public IViewBinder<TViewBatch> CreateBinder(IViewShellLoader shellLoader)
        {
            return _binderFactory(shellLoader);
        }
    }

    public sealed class DotsViewRenderBackend<TViewBatch> : IViewRenderBackend<TViewBatch>
        where TViewBatch : struct, IViewBatch
    {
        private readonly Func<IViewShellLoader, IViewBinder<TViewBatch>>? _binderFactory;

        public DotsViewRenderBackend(Func<IViewShellLoader, IViewBinder<TViewBatch>>? binderFactory = null)
        {
            _binderFactory = binderFactory;
        }

        public ViewRenderBackend Backend => ViewRenderBackend.Dots;

        public IViewBinder<TViewBatch> CreateBinder(IViewShellLoader shellLoader)
        {
            if (shellLoader == null) throw new ArgumentNullException(nameof(shellLoader));
            return _binderFactory != null
                ? _binderFactory(shellLoader)
                : new DotsViewBinder<TViewBatch>();
        }
    }

    public sealed class DotsViewBinder<TViewBatch> : IViewBinder<TViewBatch>
        where TViewBatch : struct, IViewBatch
    {
        public bool InterpolationEnabled { get; set; }

        public void ApplyBatch(in TViewBatch batch)
        {
        }

        public void TickInterpolation(float deltaTime)
        {
        }

        public void RebindAll()
        {
        }

        public void Clear()
        {
        }
    }
}
