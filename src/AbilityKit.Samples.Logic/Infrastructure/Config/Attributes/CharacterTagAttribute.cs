using System;

namespace AbilityKit.Samples.Logic.Infrastructure.Config.Attributes
{
    /// <summary>
    /// 瑙掕壊鏍囩鏍囪瘑灞炴€?
    /// 鐢ㄤ簬鏍囪瑙掕壊鎵€灞炵殑鏍囩绫诲埆 (Hero, Boss, Tank, Healer 绛?
    /// 涓€涓鑹插彲浠ユ湁澶氫釜鏍囩
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class CharacterTagAttribute : AbilityKit.Core.Common.Marker.MarkerAttribute
    {
        public string Tag { get; }

        public CharacterTagAttribute(string tag)
        {
            Tag = tag;
        }

        public override void OnScanned(Type implType, AbilityKit.Core.Common.Marker.IMarkerRegistry registry)
        {
            if (registry is CharacterTagRegistry keyedRegistry)
            {
                keyedRegistry.RegisterByAttribute(this, implType);
            }
        }
    }
}
