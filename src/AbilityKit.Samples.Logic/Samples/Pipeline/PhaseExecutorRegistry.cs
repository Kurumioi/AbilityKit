using System;
using System.Collections.Generic;
using AbilityKit.Core.Pooling;
using AbilityKit.Samples.Logic.Infrastructure.Config.Attributes;

namespace AbilityKit.Samples.Logic.Samples.Pipeline
{
    /// <summary>
    /// 闃舵鎵ц鍣ㄦ敞鍐岃〃
    /// 浣跨敤瀵硅薄姹犵鐞嗘墽琛屽櫒瀹炰緥鐨勫鐢?    /// 閫氳繃 ExecutorForAttribute 鑷姩鍙戠幇閰嶇疆绫诲瀷涓庢墽琛屽櫒鐨勬槧灏?    /// </summary>
    public sealed class PhaseExecutorRegistry
    {
        private static readonly Lazy<PhaseExecutorRegistry> _instance = new(() => new PhaseExecutorRegistry());
        public static PhaseExecutorRegistry Instance => _instance.Value;

        private readonly Dictionary<Type, ObjectPool<IPhaseExecutor>> _pools = new();
        private readonly Dictionary<Type, int> _poolMaxSizes = new();
        private readonly ExecutorForRegistry _executorForRegistry = ExecutorForRegistry.Instance;

        private PhaseExecutorRegistry()
        {
            RegisterBuiltins();
        }

        private void RegisterBuiltins()
        {
            RegisterPooled<PreCheckExecutor>(maxSize: 10);
            RegisterPooled<ValidationExecutor>(maxSize: 10);
            RegisterPooled<CastingExecutor>(maxSize: 5);
            RegisterPooled<ExecuteExecutor>(maxSize: 10);
            RegisterPooled<CooldownExecutor>(maxSize: 10);
        }

        /// <summary>
        /// 娉ㄥ唽甯﹀璞℃睜鐨勬墽琛屽櫒
        /// </summary>
        public void RegisterPooled<TExecutor>(int maxSize = 10) where TExecutor : class, IPhaseExecutor, new()
        {
            var options = new ObjectPoolOptions<IPhaseExecutor>(() => new TExecutor())
            {
                MaxSize = maxSize,
                DefaultCapacity = 2,
                OnRelease = obj => obj.OnPoolRelease(),
                OnGet = obj => obj.OnPoolGet()
            };

            var pool = new ObjectPool<IPhaseExecutor>(options);
            _pools[typeof(TExecutor)] = pool;
            _poolMaxSizes[typeof(TExecutor)] = maxSize;
        }

        /// <summary>
        /// 鑾峰彇鎵ц鍣ㄥ疄渚嬶紙浠庢睜涓幏鍙栵級
        /// </summary>
        public IPhaseExecutor Rent<TExecutor>() where TExecutor : class, IPhaseExecutor, new()
        {
            if (_pools.TryGetValue(typeof(TExecutor), out var pool))
            {
                return pool.Get();
            }
            return new TExecutor();
        }

        /// <summary>
        /// 褰掕繕鎵ц鍣ㄥ疄渚嬶紙褰掕繕鍒版睜涓級
        /// </summary>
        public void Return(IPhaseExecutor executor)
        {
            if (executor == null) return;

            if (_pools.TryGetValue(executor.GetType(), out var pool))
            {
                pool.Release(executor);
            }
        }

        /// <summary>
        /// 鏍规嵁閰嶇疆绫诲瀷鑾峰彇瀵瑰簲鐨勬墽琛屽櫒锛堜粠姹犱腑鑾峰彇锛?        /// </summary>
        public IPhaseExecutor Rent(Type executorType)
        {
            if (executorType == null) return null;

            if (_pools.TryGetValue(executorType, out var pool))
            {
                return pool.Get();
            }

            // 濡傛灉娌℃湁姹狅紝鐩存帴鍒涘缓
            return Activator.CreateInstance(executorType) as IPhaseExecutor;
        }

        /// <summary>
        /// 鏍规嵁閰嶇疆鑾峰彇鎵ц鍣紙鑷姩鎺ㄦ柇绫诲瀷锛?        /// </summary>
        public IPhaseExecutor Rent(object config)
        {
            if (config == null) return null;

            var configType = config.GetType();
            var executorType = _executorForRegistry.GetExecutorType(configType);
            if (executorType != null)
            {
                return Rent(executorType);
            }

            return null;
        }

        /// <summary>
        /// 鏍规嵁閰嶇疆绫诲瀷鑾峰彇瀵瑰簲鐨勬墽琛屽櫒绫诲瀷
        /// </summary>
        public Type GetExecutorType(Type configType)
        {
            return _executorForRegistry.GetExecutorType(configType);
        }

        /// <summary>
        /// 棰勭儹姹?        /// </summary>
        public void Prewarm(int count = 5)
        {
            foreach (var pool in _pools.Values)
            {
                pool.Prewarm(count);
            }
        }

        /// <summary>
        /// 鑾峰彇鎵€鏈夊凡娉ㄥ唽鐨勬墽琛屽櫒淇℃伅
        /// </summary>
        public IEnumerable<(string ExecutorName, int PoolSize, int InactiveCount)> GetPoolStats()
        {
            foreach (var kvp in _pools)
            {
                var maxSize = _poolMaxSizes.GetValueOrDefault(kvp.Key, 0);
                yield return (kvp.Key.Name, maxSize, kvp.Value.InactiveCount);
            }
        }

        /// <summary>
        /// 鑾峰彇鎵€鏈夐厤缃啋鎵ц鍣ㄧ殑鏄犲皠
        /// </summary>
        public IEnumerable<(Type ConfigType, Type ExecutorType)> GetAllMappings()
        {
            return _executorForRegistry.GetAllMappings();
        }
    }

    /// <summary>
    /// 姹犲寲鎵ц鍣ㄧ殑鍖呰鍣?    /// 鏋愭瀯鏃惰嚜鍔ㄥ綊杩樺埌姹?    /// </summary>
    public sealed class PooledExecutor : IDisposable
    {
        private readonly PhaseExecutorRegistry _registry;
        public IPhaseExecutor Executor { get; }

        internal PooledExecutor(IPhaseExecutor executor, PhaseExecutorRegistry registry)
        {
            Executor = executor;
            _registry = registry;
        }

        public void Dispose()
        {
            _registry?.Return(Executor);
        }
    }

    /// <summary>
    /// 鎵ц鍣ㄤ笂涓嬫枃鎵╁睍
    /// </summary>
    public static class PhaseExecutorContextExtensions
    {
        /// <summary>
        /// 绉熻祦鎵ц鍣紙浣跨敤姹狅級
        /// </summary>
        public static PooledExecutor RentExecutor(this PhaseExecutorRegistry registry, object config)
        {
            var executor = registry.Rent(config);
            return new PooledExecutor(executor, registry);
        }
    }
}
