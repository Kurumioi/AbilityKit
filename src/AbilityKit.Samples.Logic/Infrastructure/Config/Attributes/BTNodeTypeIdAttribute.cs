using System;

namespace AbilityKit.Samples.Logic.Infrastructure.Config.Attributes
{
    /// <summary>
    /// 琛屼负鏍戣妭鐐圭被鍨嬫爣璇嗗睘鎬?
    /// 鐢ㄤ簬鏍囪瀹炵幇浜?IBTNode 鎺ュ彛鐨勮妭鐐圭被鍨?(Selector, Sequence, Condition, Action)
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class BTNodeTypeIdAttribute : AbilityKit.Core.Common.Marker.MarkerAttribute
    {
        public string NodeType { get; }

        public BTNodeTypeIdAttribute(string nodeType)
        {
            NodeType = nodeType;
        }

        public override void OnScanned(Type implType, AbilityKit.Core.Common.Marker.IMarkerRegistry registry)
        {
            if (registry is BTNodeTypeRegistry keyedRegistry)
            {
                keyedRegistry.RegisterByAttribute(this, implType);
            }
        }
    }
}
