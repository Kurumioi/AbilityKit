using System;
using AbilityKit.Core.Markers;
using AbilityKit.Samples.Logic.Infrastructure.Config.Attributes;

namespace AbilityKit.Samples.Logic.Infrastructure.Config
{
    /// <summary>
    /// 瑙掕壊绫诲瀷娉ㄥ唽琛?
    /// 閫氳繃 CharacterTypeIdAttribute 鑷姩鍙戠幇鍜屾敞鍐岃鑹插疄鐜扮被鍨?(Hero, Boss, Tower 绛?
    /// </summary>
    public sealed class CharacterTypeRegistry : KeyedMarkerRegistry<string, CharacterTypeIdAttribute>
    {
        private static readonly Lazy<CharacterTypeRegistry> _instance = new(() => new CharacterTypeRegistry());
        public static CharacterTypeRegistry Instance => _instance.Value;

        private CharacterTypeRegistry()
        {
            ScanCurrentAssembly();
        }

        private void ScanCurrentAssembly()
        {
            var assembly = typeof(CharacterTypeRegistry).Assembly;
            MarkerScanner<CharacterTypeIdAttribute>.Scan(new[] { assembly }, this);
        }

        internal void RegisterByAttribute(CharacterTypeIdAttribute attr, Type implType)
        {
            if (attr == null || implType == null) return;
            Register(attr.CharacterId, implType);
        }

        /// <summary>
        /// 鏍规嵁瑙掕壊 ID 鍒涘缓瑙掕壊瀹炰緥
        /// </summary>
        public object CreateCharacter(string characterId)
        {
            return GetOrCreateInstance(characterId);
        }
    }
}
