using System;
using AbilityKit.World.ECS;

namespace AbilityKit.Game.Flow
{
    internal sealed class GameFlowRuntimeAdapter
    {
        public GameFlowRuntimeAdapter(IEntity root)
        {
            if (!root.IsValid)
            {
                throw new ArgumentNullException(nameof(root));
            }

            Root = root;
            Features = new EntityFeatureStore(root);
            FeatureBinder = new EntityFeatureBinder(root);
        }

        public IEntity Root { get; }

        public IGameFeatureStore Features { get; }

        public IFeatureBinder FeatureBinder { get; }

        private sealed class EntityFeatureStore : IGameFeatureStore
        {
            private readonly IEntity _entity;

            public EntityFeatureStore(IEntity entity)
            {
                _entity = entity;
            }

            public bool TryGet<T>(out T component) where T : class => _entity.TryGetRef(out component);
            public void Set<T>(T component) where T : class => _entity.WithRef(component);
            public void Remove<T>() where T : class => _entity.RemoveComponent(typeof(T));
            public void Remove(Type componentType) => _entity.RemoveComponent(componentType);
        }

        private sealed class EntityFeatureBinder : IFeatureBinder
        {
            private readonly IEntity _entity;

            public EntityFeatureBinder(IEntity entity)
            {
                _entity = entity;
            }

            public void AttachFeature(object feature) => _entity.WithRef((object)feature);
            public void DetachFeature(object feature) => _entity.RemoveComponent(feature.GetType());
        }
    }
}
