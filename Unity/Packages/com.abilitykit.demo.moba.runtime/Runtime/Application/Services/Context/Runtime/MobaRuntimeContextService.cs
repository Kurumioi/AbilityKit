using AbilityKit.Ability.World.DI;
using AbilityKit.Ability.World.Services;
using AbilityKit.Ability.World.Services.Attributes;
using AbilityKit.Context;
using AbilityKit.Demo.Moba.Components;

namespace AbilityKit.Demo.Moba.Services
{
    [WorldService(typeof(MobaRuntimeContextService), WorldLifetime.Scoped)]
    public sealed class MobaRuntimeContextService : IService
    {
        private readonly ContextRegistry _registry = new ContextRegistry();
        private readonly SnapshotStorage _snapshots = new SnapshotStorage();
        private readonly ContextValueResolver _resolver;

        public MobaRuntimeContextService()
        {
            _resolver = new ContextValueResolver(_registry, _snapshots);
        }

        public ContextRegistry Registry => _registry;
        public SnapshotStorage Snapshots => _snapshots;
        public ContextValueResolver Resolver => _resolver;

        public long EnsureBuffContext(BuffRuntime runtime, in MobaBuffRuntimeContextData data)
        {
            if (runtime == null) return 0L;

            var contextId = runtime.RuntimeContextId;
            if (contextId == 0L || !_registry.Exists(contextId))
            {
                var createdProperty = new MobaBuffContextProperty(data);
                contextId = _registry.Create()
                    .With(createdProperty)
                    .Build();
                createdProperty.SetContextId(contextId);
                _registry.Set(contextId, createdProperty);
                runtime.RuntimeContextId = contextId;
                runtime.RuntimeContextVersion = createdProperty.Version;
                return contextId;
            }

            var property = _registry.Get<MobaBuffContextProperty>(contextId);
            if (property == null)
            {
                property = new MobaBuffContextProperty(data);
                property.SetContextId(contextId);
                _registry.Set(contextId, property);
            }
            else
            {
                property.Update(data);
                _registry.Set(contextId, property);
            }

            runtime.RuntimeContextVersion = property.Version;
            return contextId;
        }

        public bool TryGetBuffContext(long contextId, out MobaBuffContextProperty property)
        {
            property = contextId != 0L ? _registry.Get<MobaBuffContextProperty>(contextId) : null;
            return property != null;
        }

        public void SnapshotAndDestroyBuffContext(BuffRuntime runtime, MobaRuntimeContextLifecycleState finalState, int frame)
        {
            if (runtime == null) return;

            var contextId = runtime.RuntimeContextId;
            if (contextId == 0L) return;

            MobaBuffContextProperty property = null;
            if (_registry.Exists(contextId))
            {
                property = _registry.Get<MobaBuffContextProperty>(contextId);
                if (property != null)
                {
                    property.Mark(finalState, frame);
                    _registry.Set(contextId, property);
                    _snapshots.Save(new MobaBuffContextSnapshot(property));
                }

                _registry.Destroy(contextId);
            }
            else if (!_snapshots.TryGetRecord(contextId, out _))
            {
                property = MobaBuffContextProperty.FromRuntime(runtime, 0, frame, finalState);
                _snapshots.Save(new MobaBuffContextSnapshot(property));
            }

            _snapshots.MarkDestroyed(contextId);
            runtime.RuntimeContextId = 0L;
            runtime.RuntimeContextVersion = 0L;
        }

        public void Dispose()
        {
            _registry.Clear();
            _snapshots.Clear();
        }
    }
}
