using System;

namespace AbilityKit.Samples.Logic.Infrastructure.Config.Attributes
{
    /// <summary>
    /// 绠＄嚎闃舵绫诲瀷鏍囪瘑灞炴€?
    /// 鐢ㄤ簬鏍囪瀹炵幇浜?IPipelinePhase 鎺ュ彛鐨勯樁娈电被鍨?
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class PipelinePhaseTypeIdAttribute : AbilityKit.Core.Common.Marker.MarkerAttribute
    {
        public string TypeName { get; }
        public bool IsTimed { get; }

        public PipelinePhaseTypeIdAttribute(string typeName, bool isTimed = false)
        {
            TypeName = typeName;
            IsTimed = isTimed;
        }

        public override void OnScanned(Type implType, AbilityKit.Core.Common.Marker.IMarkerRegistry registry)
        {
            if (registry is PipelinePhaseRegistry keyedRegistry)
            {
                keyedRegistry.RegisterByAttribute(this, implType);
            }
        }
    }
}
