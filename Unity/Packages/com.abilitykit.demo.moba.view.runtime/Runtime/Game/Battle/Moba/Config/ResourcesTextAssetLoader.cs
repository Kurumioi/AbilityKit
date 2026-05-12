using UnityEngine;
using AbilityKit.Ability.Config;
using AbilityKit.Ability.World.DI;
using AbilityKit.Ability.World.Services.Attributes;

namespace AbilityKit.Demo.Moba.View.Config
{
    /// <summary>
    /// Unity Resources 实现的 TextAsset 加载器。
    /// 这是 View 层实现，负责处理 Unity 平台的资源加载。
    /// </summary>
    [WorldService(typeof(ITextAssetLoader), WorldLifetime.Singleton)]
    public sealed class ResourcesTextAssetLoader : ITextAssetLoader
    {
        public bool TryLoadText(string path, out string text)
        {
            text = null;
            if (string.IsNullOrEmpty(path)) return false;

            var asset = Resources.Load<TextAsset>(path);
            if (asset == null) return false;

            text = asset.text;
            return !string.IsNullOrEmpty(text);
        }

        public bool TryLoadBytes(string path, out byte[] bytes)
        {
            bytes = null;
            if (string.IsNullOrEmpty(path)) return false;

            var asset = Resources.Load<TextAsset>(path);
            if (asset == null) return false;

            bytes = asset.bytes;
            return bytes != null && bytes.Length > 0;
        }
    }
}
