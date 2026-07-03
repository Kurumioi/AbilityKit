using System;

namespace AbilityKit.Context
{
    /// <summary>
    /// 上下文值解析器。
    /// 为触发模块提供按上下文标识读取实时值或快照值的统一入口。
    /// </summary>
    public sealed class ContextValueResolver
    {
        private readonly ContextRegistry _registry;
        private readonly SnapshotStorage _snapshots;
        private readonly ContextRealtimeProviderRegistry _realtimeProviders;

        public ContextValueResolver(ContextRegistry registry, SnapshotStorage snapshots = null, ContextRealtimeProviderRegistry realtimeProviders = null)
        {
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
            _snapshots = snapshots;
            _realtimeProviders = realtimeProviders;
        }

        public ContextRegistry Registry => _registry;
        public SnapshotStorage Snapshots => _snapshots;
        public ContextRealtimeProviderRegistry RealtimeProviders => _realtimeProviders;

        public bool TryGetRealtimeProperty<TProperty>(long contextId, out TProperty property)
            where TProperty : class, IProperty
        {
            var type = PropertyTypeRegistry.Instance.Get<TProperty>() ?? PropertyTypeRegistry.Instance.Register<TProperty>();
            return TryReadRealtimeProperty(contextId, type.Id, out property);
        }

        public bool TryGetSnapshot(long contextId, out IContextSnapshot snapshot)
        {
            snapshot = _snapshots?.Get(contextId);
            return snapshot != null;
        }

        public ContextValueResult<TProperty> GetProperty<TProperty>(
            long contextId,
            ContextValueReadMode mode = ContextValueReadMode.RealtimeThenSnapshot)
            where TProperty : class, IProperty
        {
            var request = ContextValueRequest.ForProperty<TProperty>(contextId);
            return GetProperty<TProperty>(request, mode);
        }

        public ContextValueResult<TProperty> GetProperty<TProperty>(
            in ContextValueRequest request,
            ContextValueReadMode mode = ContextValueReadMode.RealtimeThenSnapshot)
            where TProperty : class, IProperty
        {
            if (!request.HasPropertyType)
                return ContextValueResult<TProperty>.Missing();

            if (ShouldReadRealtimeFirst(mode) && TryReadRealtimeProperty(request.ContextId, request.PropertyTypeId, out TProperty realtime))
                return new ContextValueResult<TProperty>(true, realtime, ContextValueSource.Realtime);

            if (ShouldReadSnapshot(mode) && TryReadSnapshotValue(request, out TProperty snapshotValue))
                return new ContextValueResult<TProperty>(true, snapshotValue, ContextValueSource.Snapshot);

            if (ShouldReadRealtimeLast(mode) && TryReadRealtimeProperty(request.ContextId, request.PropertyTypeId, out realtime))
                return new ContextValueResult<TProperty>(true, realtime, ContextValueSource.Realtime);

            return ContextValueResult<TProperty>.Missing();
        }

        public ContextValueResult<T> GetValue<T, TProperty>(
            long contextId,
            string key,
            T defaultValue = default,
            ContextValueReadMode mode = ContextValueReadMode.RealtimeThenSnapshot)
            where TProperty : class, IProperty
        {
            var request = ContextValueRequest.ForProperty<TProperty>(contextId, key);
            return GetValue<T>(request, defaultValue, mode);
        }

        public ContextValueResult<T> GetValue<T>(
            in ContextValueRequest request,
            T defaultValue = default,
            ContextValueReadMode mode = ContextValueReadMode.RealtimeThenSnapshot)
        {
            if (string.IsNullOrEmpty(request.Key))
                return ContextValueResult<T>.FromDefault(defaultValue);

            if (ShouldReadRealtimeFirst(mode) && TryReadRealtimeValue(request, out T realtime))
                return new ContextValueResult<T>(true, realtime, ContextValueSource.Realtime);

            if (ShouldReadSnapshot(mode) && TryReadSnapshotValue(request, out T snapshotValue))
                return new ContextValueResult<T>(true, snapshotValue, ContextValueSource.Snapshot);

            if (ShouldReadRealtimeLast(mode) && TryReadRealtimeValue(request, out realtime))
                return new ContextValueResult<T>(true, realtime, ContextValueSource.Realtime);

            return ContextValueResult<T>.FromDefault(defaultValue);
        }

        public bool TryGetValue<T, TProperty>(
            long contextId,
            string key,
            out T value,
            ContextValueReadMode mode = ContextValueReadMode.RealtimeThenSnapshot)
            where TProperty : class, IProperty
        {
            var request = ContextValueRequest.ForProperty<TProperty>(contextId, key);
            return TryGetValue(request, out value, mode);
        }

        public bool TryGetValue<T>(
            in ContextValueRequest request,
            out T value,
            ContextValueReadMode mode = ContextValueReadMode.RealtimeThenSnapshot)
        {
            var result = GetValue<T>(request, default, mode);
            value = result.Value;
            return result.Found && result.Source != ContextValueSource.DefaultValue;
        }

        private bool TryReadRealtimeProperty<TProperty>(long contextId, int propertyTypeId, out TProperty property)
            where TProperty : class, IProperty
        {
            if (_realtimeProviders != null && _realtimeProviders.TryGetProperty(contextId, propertyTypeId, out TProperty realtime))
            {
                property = realtime;
                return true;
            }

            var raw = _registry.GetProperty(contextId, propertyTypeId);
            if (raw is TProperty typed)
            {
                property = typed;
                return true;
            }

            property = null;
            return false;
        }

        private bool TryReadRealtimeValue<T>(in ContextValueRequest request, out T value)
        {
            value = default;
            if (!request.HasPropertyType)
                return false;

            if (_realtimeProviders != null && _realtimeProviders.TryGetValue(request, out value))
                return true;

            var property = _registry.GetProperty(request.ContextId, request.PropertyTypeId);
            if (property == null)
                return false;

            if (property is IContextValueProvider provider && provider.TryGetValue(request.Key, out value))
                return true;

            if (property is T typed && string.IsNullOrEmpty(request.Key))
            {
                value = typed;
                return true;
            }

            return false;
        }

        private bool TryReadSnapshotValue<T>(in ContextValueRequest request, out T value)
        {
            value = default;
            if (_snapshots == null)
                return false;

            var snapshot = _snapshots.Get(request.ContextId);
            if (snapshot == null)
                return false;

            if (snapshot is IContextValueProvider provider && provider.TryGetValue(request.Key, out value))
                return true;

            if (snapshot is ISnapshotAccessor accessor)
            {
                value = accessor.GetValue(request.Key, default(T));
                return true;
            }

            if (snapshot is T typed && string.IsNullOrEmpty(request.Key))
            {
                value = typed;
                return true;
            }

            return false;
        }

        private static bool ShouldReadRealtimeFirst(ContextValueReadMode mode)
        {
            return mode == ContextValueReadMode.RealtimeThenSnapshot || mode == ContextValueReadMode.RealtimeOnly;
        }

        private static bool ShouldReadRealtimeLast(ContextValueReadMode mode)
        {
            return mode == ContextValueReadMode.SnapshotThenRealtime;
        }

        private static bool ShouldReadSnapshot(ContextValueReadMode mode)
        {
            return mode == ContextValueReadMode.RealtimeThenSnapshot || mode == ContextValueReadMode.SnapshotThenRealtime || mode == ContextValueReadMode.SnapshotOnly;
        }
    }
}
