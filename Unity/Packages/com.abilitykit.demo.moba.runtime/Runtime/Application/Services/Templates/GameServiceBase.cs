using System;
using AbilityKit.Ability.World.DI;
using AbilityKit.Ability.World.Services;

namespace AbilityKit.Demo.Moba.Services.Templates
{
    /// <summary>
    /// Base class for logic-world services that need consistent logging and disposal hooks.
    /// </summary>
    public abstract class LogicWorldServiceBase<TService> : IService
        where TService : class
    {
        private bool _disposed;

        protected virtual string ServiceName => typeof(TService).Name;

        protected bool IsDisposed => _disposed;

        protected void ThrowIfDisposed()
        {
            if (_disposed) throw new ObjectDisposedException(ServiceName);
        }

        protected void LogInfo(string message) => Core.Common.Log.Log.Info($"[{ServiceName}] {message}");

        protected void LogWarning(string message) => Core.Common.Log.Log.Warning($"[{ServiceName}] {message}");

        protected void LogError(string message) => Core.Common.Log.Log.Error($"[{ServiceName}] {message}");

        protected void LogException(Exception ex, string context = "")
        {
            var msg = string.IsNullOrEmpty(context) ? ex.Message : $"{context}: {ex.Message}";
            Core.Common.Log.Log.Exception(ex, $"[{ServiceName}] {msg}");
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            OnDispose();
        }

        protected virtual void OnDispose()
        {
        }
    }

    /// <summary>
    /// Base class for services that need the world service resolver after the container is built.
    /// </summary>
    public abstract class LogicWorldInitializableServiceBase<TService> : LogicWorldServiceBase<TService>, IWorldInitializable
        where TService : class
    {
        protected IWorldResolver WorldResolver { get; private set; }

        protected bool TryResolve<T>(out T service) where T : class
        {
            service = null;
            return WorldResolver != null && WorldResolver.TryResolve(out service);
        }

        protected T Resolve<T>() where T : class
        {
            if (WorldResolver == null) throw new InvalidOperationException($"[{ServiceName}] WorldResolver is null");
            return WorldResolver.Resolve<T>();
        }

        public virtual void OnInit(IWorldResolver resolver)
        {
            WorldResolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
            OnServicesReady(resolver);
        }

        protected virtual void OnServicesReady(IWorldResolver resolver)
        {
        }
    }

    /// <summary>
    /// Base class for services that need both world initialization and world deinitialization hooks.
    /// </summary>
    public abstract class LogicWorldLifecycleServiceBase<TService> : LogicWorldInitializableServiceBase<TService>, IWorldDeinitializable
        where TService : class
    {
        private bool _deinitialized;

        public virtual void OnDeinit(IWorldResolver resolver)
        {
            if (_deinitialized) return;
            _deinitialized = true;
            OnWorldDeinit(resolver);
        }

        protected virtual void OnWorldDeinit(IWorldResolver resolver)
        {
        }
    }

    /// <summary>
    /// Base class for logic-world services that publish triggering events.
    /// </summary>
    public abstract class LogicWorldEventServiceBase<TService> : LogicWorldInitializableServiceBase<TService>
        where TService : class
    {
        protected AbilityKit.Triggering.Eventing.IEventBus EventBus { get; private set; }

        protected override void OnServicesReady(IWorldResolver resolver)
        {
            EventBus = resolver.Resolve<AbilityKit.Triggering.Eventing.IEventBus>();
        }

        protected void Publish<T>(string eventId, in T payload) where T : struct
        {
            if (EventBus == null)
            {
                LogWarning("EventBus is null, cannot publish event");
                return;
            }

            var eid = TriggeringIdUtil.GetEventEid(eventId);
            EventBus.Publish(new Core.Common.Event.EventKey<T>(eid), in payload);

            object boxed = payload;
            EventBus.Publish(new Core.Common.Event.EventKey<object>(eid), in boxed);
        }
    }

    public abstract class GameServiceBase<TService> : LogicWorldServiceBase<TService>
        where TService : class
    {
    }

    public abstract class GameInitializableServiceBase<TService> : LogicWorldInitializableServiceBase<TService>
        where TService : class
    {
    }

    public abstract class GameLifecycleServiceBase<TService> : LogicWorldLifecycleServiceBase<TService>
        where TService : class
    {
    }

    public abstract class GameEventPublisherServiceBase<TService> : LogicWorldEventServiceBase<TService>
        where TService : class
    {
    }
}
