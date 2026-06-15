using System;
using AbilityKit.Core.Markers;
using AbilityKit.Samples.Logic.Infrastructure.Config.Attributes;

namespace AbilityKit.Samples.Logic.Infrastructure.Config
{
    /// <summary>
    /// 琛屼负鏍戣妭鐐圭被鍨嬫敞鍐岃〃
    /// 閫氳繃 BTNodeTypeIdAttribute 鑷姩鍙戠幇鍜屾敞鍐岃涓烘爲鑺傜偣绫诲瀷 (Selector, Sequence, Condition, Action)
    /// </summary>
    public sealed class BTNodeTypeRegistry : KeyedMarkerRegistry<string, BTNodeTypeIdAttribute>
    {
        private static readonly Lazy<BTNodeTypeRegistry> _instance = new(() => new BTNodeTypeRegistry());
        public static BTNodeTypeRegistry Instance => _instance.Value;

        private BTNodeTypeRegistry()
        {
            ScanCurrentAssembly();
        }

        private void ScanCurrentAssembly()
        {
            var assembly = typeof(BTNodeTypeRegistry).Assembly;
            MarkerScanner<BTNodeTypeIdAttribute>.Scan(new[] { assembly }, this);
        }

        internal void RegisterByAttribute(BTNodeTypeIdAttribute attr, Type implType)
        {
            if (attr == null || implType == null) return;
            Register(attr.NodeType, implType);
        }

        /// <summary>
        /// 鏍规嵁鑺傜偣绫诲瀷鍚嶇О鍒涘缓鑺傜偣瀹炰緥
        /// </summary>
        public object CreateNode(string nodeType)
        {
            return GetOrCreateInstance(nodeType);
        }
    }
}
