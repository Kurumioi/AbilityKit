using System;
using AbilityKit.Core.Common.Marker;
using AbilityKit.Samples.Logic.Infrastructure.Config.Attributes;

namespace AbilityKit.Samples.Logic.Infrastructure.Config
{
    /// <summary>
    /// 鐘舵€佽浆鎹㈡潯浠剁被鍨嬫敞鍐岃〃
    /// 閫氳繃 TransitionConditionTypeIdAttribute 鑷姩鍙戠幇鍜屾敞鍐屾潯浠剁被鍨?
    /// </summary>
    public sealed class TransitionConditionTypeRegistry : KeyedMarkerRegistry<string, TransitionConditionTypeIdAttribute>
    {
        private static readonly Lazy<TransitionConditionTypeRegistry> _instance = new(() => new TransitionConditionTypeRegistry());
        public static TransitionConditionTypeRegistry Instance => _instance.Value;

        private TransitionConditionTypeRegistry()
        {
            ScanCurrentAssembly();
        }

        private void ScanCurrentAssembly()
        {
            var assembly = typeof(TransitionConditionTypeRegistry).Assembly;
            MarkerScanner<TransitionConditionTypeIdAttribute>.Scan(new[] { assembly }, this);
        }

        internal void RegisterByAttribute(TransitionConditionTypeIdAttribute attr, Type implType)
        {
            if (attr == null || implType == null) return;
            Register(attr.ConditionName, implType);
        }

        /// <summary>
        /// 鏍规嵁鍚嶇О鍒涘缓鏉′欢瀹炰緥
        /// </summary>
        public object CreateCondition(string conditionName)
        {
            return GetOrCreateInstance(conditionName);
        }
    }
}
