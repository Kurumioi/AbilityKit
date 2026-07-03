using System;
using System.Collections.Generic;

namespace AbilityKit.Context
{
    /// <summary>
    /// Runtime value provider registry for context ids.
    /// Providers are used before stored properties so systems can expose live data without mirroring it into ContextRegistry.
    /// </summary>
    public sealed class ContextRealtimeProviderRegistry
    {
        private readonly Dictionary<int, IContextRealtimeValueProvider> _providersByPropertyType = new Dictionary<int, IContextRealtimeValueProvider>();
        private readonly object _lock = new object();

        public void Register<TProperty>(IContextRealtimeValueProvider provider)
            where TProperty : IProperty
        {
            if (provider == null) throw new ArgumentNullException(nameof(provider));
            var type = PropertyTypeRegistry.Instance.Get<TProperty>() ?? PropertyTypeRegistry.Instance.Register<TProperty>();
            Register(type.Id, provider);
        }

        public void Register(int propertyTypeId, IContextRealtimeValueProvider provider)
        {
            if (propertyTypeId <= 0) throw new ArgumentOutOfRangeException(nameof(propertyTypeId));
            if (provider == null) throw new ArgumentNullException(nameof(provider));

            lock (_lock)
                _providersByPropertyType[propertyTypeId] = provider;
        }

        public bool Unregister<TProperty>() where TProperty : IProperty
        {
            var type = PropertyTypeRegistry.Instance.Get<TProperty>();
            return type != null && Unregister(type.Id);
        }

        public bool Unregister(int propertyTypeId)
        {
            lock (_lock)
                return _providersByPropertyType.Remove(propertyTypeId);
        }

        public bool TryGetProperty<TProperty>(long contextId, int propertyTypeId, out TProperty property)
            where TProperty : class, IProperty
        {
            property = null;
            if (TryGetProvider(propertyTypeId, out var provider) && provider.TryGetProperty(contextId, out var raw) && raw is TProperty typed)
            {
                property = typed;
                return true;
            }

            return false;
        }

        public bool TryGetValue<T>(in ContextValueRequest request, out T value)
        {
            value = default;
            return TryGetProvider(request.PropertyTypeId, out var provider) && provider.TryGetValue(request.ContextId, request.Key, out value);
        }

        public void Clear()
        {
            lock (_lock)
                _providersByPropertyType.Clear();
        }

        private bool TryGetProvider(int propertyTypeId, out IContextRealtimeValueProvider provider)
        {
            lock (_lock)
                return _providersByPropertyType.TryGetValue(propertyTypeId, out provider);
        }
    }

    public interface IContextRealtimeValueProvider
    {
        bool TryGetProperty(long contextId, out IProperty property);
        bool TryGetValue<T>(long contextId, string key, out T value);
    }
}
