using System;
using System.Linq;
using AbilityKit.Core.Common.Marker;

namespace AbilityKit.Samples.Logic.Samples.Pipeline
{
    /// <summary>
    /// 鏍囪閰嶇疆绫诲瀷瀵瑰簲鐨勬墽琛屽櫒
    /// 浣跨敤姝?Attribute 鏍囪閰嶇疆绫伙紝鑷姩寤虹珛鏄犲皠
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class ExecutorForAttribute : MarkerAttribute
    {
        public Type ExecutorType { get; }

        public ExecutorForAttribute(Type executorType)
        {
            ExecutorType = executorType;
        }

        public override void OnScanned(Type implType, IMarkerRegistry registry)
        {
            if (registry is ExecutorForRegistry typedRegistry && ExecutorType != null)
            {
                typedRegistry.Register(implType, ExecutorType);
            }
        }
    }

    /// <summary>
    /// 閰嶇疆鈫掓墽琛屽櫒鏄犲皠娉ㄥ唽琛?    /// Key: 閰嶇疆绫诲瀷, Value: 鎵ц鍣ㄧ被鍨?    /// </summary>
    public sealed class ExecutorForRegistry : KeyedMarkerRegistry<Type, ExecutorForAttribute>
    {
        public static ExecutorForRegistry Instance { get; } = new();

        private ExecutorForRegistry()
        {
            ScanCurrentAssembly();
        }

        private void ScanCurrentAssembly()
        {
            var assembly = typeof(ExecutorForRegistry).Assembly;
            MarkerScanner<ExecutorForAttribute>.Scan(new[] { assembly }, this);
        }

        /// <summary>
        /// 鏍规嵁閰嶇疆绫诲瀷鑾峰彇瀵瑰簲鐨勬墽琛屽櫒绫诲瀷
        /// </summary>
        public Type GetExecutorType(Type configType)
        {
            if (TryGet(configType, out var executorType))
            {
                return executorType;
            }

            // 灏濊瘯鏌ユ壘鍩虹被
            var baseType = configType.BaseType;
            while (baseType != null && baseType != typeof(object))
            {
                if (TryGet(baseType, out executorType))
                {
                    return executorType;
                }
                baseType = baseType.BaseType;
            }

            return null;
        }

        /// <summary>
        /// 鑾峰彇鎵€鏈夐厤缃啋鎵ц鍣ㄧ殑鏄犲皠
        /// </summary>
        public IEnumerable<(Type ConfigType, Type ExecutorType)> GetAllMappings()
        {
            return Keys.Select(key => (key, TryGet(key, out var executorType) ? executorType : null))
                       .Where(tuple => tuple.Item2 != null)
                       .Select(tuple => (tuple.key, tuple.Item2!));
        }
    }
}
