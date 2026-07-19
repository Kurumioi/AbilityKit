using AbilityKit.Game.Battle.Entity;
using UnityEngine;

namespace AbilityKit.Game.Flow
{
    /// <summary>
    /// Implements <see cref="IBattleViewShellLoader"/> by delegating to a
    /// <see cref="BattleViewShellPool"/> that uses the framework
    /// <see cref="Core.Pooling.ObjectPool{T}"/> per modelId bucket.
    /// </summary>
    public sealed class PooledBattleViewShellLoader : IBattleViewShellLoader
    {
        private readonly BattleViewShellPool _pool;

        public PooledBattleViewShellLoader(BattleViewShellPool pool)
        {
            _pool = pool;
        }

        public GameObject CreateShellGameObject(int actorId, int modelId)
        {
            if (modelId <= 0) return null;
            if (_pool == null) return null;

            var instance = _pool.Get(modelId);
            if (instance != null)
            {
                instance.name = $"Shell_{modelId}_{actorId}";
            }
            return instance;
        }

        public GameObject CreateShellGameObject(int actorId, int modelId, BattleEntityKind kind)
        {
            if (modelId <= 0) return null;
            if (_pool == null) return null;

            // All entity kinds share the same shell pool keyed by modelId.
            var instance = _pool.Get(modelId);
            if (instance != null)
            {
                instance.name = $"Shell_{kind}_{modelId}_{actorId}";
            }
            return instance;
        }
    }
}
