using System;
using AbilityKit.Core.Common.Marker;
using AbilityKit.Samples.Logic.Infrastructure.Config.Attributes;

namespace AbilityKit.Samples.Logic.Infrastructure.Config
{
    /// <summary>
    /// 鎶€鑳介樁娈电被鍨嬫敞鍐岃〃
    /// 閫氳繃 SkillPhaseTypeIdAttribute 鑷姩鍙戠幇鍜屾敞鍐屾妧鑳介樁娈电被鍨?(PreCheck, CastTime, ApplyEffect 绛?
    /// </summary>
    public sealed class SkillPhaseTypeRegistry : KeyedMarkerRegistry<string, SkillPhaseTypeIdAttribute>
    {
        private static readonly Lazy<SkillPhaseTypeRegistry> _instance = new(() => new SkillPhaseTypeRegistry());
        public static SkillPhaseTypeRegistry Instance => _instance.Value;

        private SkillPhaseTypeRegistry()
        {
            ScanCurrentAssembly();
        }

        private void ScanCurrentAssembly()
        {
            var assembly = typeof(SkillPhaseTypeRegistry).Assembly;
            MarkerScanner<SkillPhaseTypeIdAttribute>.Scan(new[] { assembly }, this);
        }

        internal void RegisterByAttribute(SkillPhaseTypeIdAttribute attr, Type implType)
        {
            if (attr == null || implType == null) return;
            Register(attr.PhaseName, implType);
        }

        /// <summary>
        /// 鏍规嵁闃舵鍚嶇О鍒涘缓鎶€鑳介樁娈靛疄渚?
        /// </summary>
        public object CreatePhase(string phaseName)
        {
            return GetOrCreateInstance(phaseName);
        }
    }
}
