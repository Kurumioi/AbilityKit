using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using AbilityKit.Ability.Config;
using AbilityKit.Game.Battle.Shared.Assets;
using AbilityKit.Ability.World.DI;
using AbilityKit.Ability.World.Services.Attributes;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AbilityKit.Demo.Moba.View.Config
{
    /// <summary>
    /// Unity Resources 实现的 TextAsset 加载器。
    /// 这是 View 层实现，负责处理 Unity 平台的资源加载。
    /// </summary>
    [WorldService(typeof(ITextAssetLoader), WorldLifetime.Singleton)]
    [WorldService(typeof(ITextAssetDirectoryLoader), WorldLifetime.Singleton)]
    public sealed class ResourcesTextAssetLoader : ITextAssetLoader, ITextAssetDirectoryLoader
    {
        private readonly Dictionary<string, TextAsset> _enumeratedAssets = new Dictionary<string, TextAsset>(StringComparer.Ordinal);
        private readonly IAssetProvider _assets;

        public ResourcesTextAssetLoader(IAssetProvider assets = null)
        {
            _assets = assets ?? ResourcesAssetProvider.Shared;
        }

        public bool TryLoadText(string path, out string text)
        {
            text = null;
            var normalizedPath = NormalizeResourcePath(path);
            if (string.IsNullOrEmpty(normalizedPath)) return false;

            if (!_enumeratedAssets.TryGetValue(normalizedPath, out var asset) || asset == null)
            {
                asset = _assets.Load<TextAsset>(normalizedPath);
            }
            if (asset == null) return false;

            text = asset.text;
            return !string.IsNullOrEmpty(text);
        }

        public bool TryLoadBytes(string path, out byte[] bytes)
        {
            bytes = null;
            var normalizedPath = NormalizeResourcePath(path);
            if (string.IsNullOrEmpty(normalizedPath)) return false;

            if (!_enumeratedAssets.TryGetValue(normalizedPath, out var asset) || asset == null)
            {
                asset = _assets.Load<TextAsset>(normalizedPath);
            }
            if (asset == null) return false;

            bytes = asset.bytes;
            return bytes != null && bytes.Length > 0;
        }

        public IEnumerable<string> GetTextAssetPaths(string directory, string pattern)
        {
            var normalizedDirectory = NormalizeResourcePath(directory);
            if (string.IsNullOrEmpty(normalizedDirectory)) return Array.Empty<string>();

#if UNITY_EDITOR
            var editorPaths = GetEditorResourcePaths(normalizedDirectory, pattern);
            if (editorPaths.Count > 0) return editorPaths;
#endif

            var assets = _assets.LoadAll<TextAsset>(normalizedDirectory);
            if (assets == null || assets.Length == 0) return Array.Empty<string>();

            var paths = new List<string>(assets.Length);
            for (int i = 0; i < assets.Length; i++)
            {
                var asset = assets[i];
                if (asset == null || !MatchesPattern(asset.name, pattern)) continue;

                var path = normalizedDirectory + "/" + asset.name;
                _enumeratedAssets[path] = asset;
                paths.Add(path);
            }

            return paths.Distinct(StringComparer.Ordinal).ToArray();
        }

        private static string NormalizeResourcePath(string path)
        {
            if (string.IsNullOrEmpty(path)) return string.Empty;
            var normalized = path.Replace('\\', '/').Trim('/');
            if (normalized.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            {
                normalized = normalized.Substring(0, normalized.Length - 5);
            }
            return normalized;
        }

        private static bool MatchesPattern(string assetName, string pattern)
        {
            if (string.IsNullOrEmpty(assetName)) return false;
            if (string.IsNullOrEmpty(pattern) || pattern == "*.json" || pattern == "**/*.json") return true;

            var normalizedPattern = pattern.Replace('\\', '/');
            var fileName = Path.GetFileNameWithoutExtension(normalizedPattern);
            return string.IsNullOrEmpty(fileName) || fileName == "*" || string.Equals(assetName, fileName, StringComparison.OrdinalIgnoreCase);
        }

#if UNITY_EDITOR
        private static IReadOnlyList<string> GetEditorResourcePaths(string normalizedDirectory, string pattern)
        {
            var result = new List<string>();
            var resourcesRoots = Directory.GetDirectories(Application.dataPath, "Resources", SearchOption.AllDirectories);
            var searchPattern = string.IsNullOrEmpty(pattern) ? "*.json" : pattern.Replace("**/", string.Empty).Replace("**", "*");

            foreach (var root in resourcesRoots)
            {
                var fullDirectory = Path.Combine(root, normalizedDirectory.Replace('/', Path.DirectorySeparatorChar));
                if (!Directory.Exists(fullDirectory)) continue;

                foreach (var file in Directory.GetFiles(fullDirectory, searchPattern, SearchOption.AllDirectories))
                {
                    var assetPath = file.Replace('\\', '/');
                    var resourcesIndex = assetPath.IndexOf("/Resources/", StringComparison.OrdinalIgnoreCase);
                    if (resourcesIndex < 0) continue;

                    var relative = assetPath.Substring(resourcesIndex + "/Resources/".Length);
                    if (relative.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                    {
                        relative = relative.Substring(0, relative.Length - 5);
                    }

                    if (AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/" + assetPath.Substring(Application.dataPath.Length + 1)) != null)
                    {
                        result.Add(relative);
                    }
                }
            }

            return result.Distinct(StringComparer.Ordinal).ToArray();
        }
#endif
    }
}
