using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AbilityKit.Ability.Config;
using AbilityKit.Ability.World.DI;
using AbilityKit.Ability.World.Services.Attributes;

namespace ET.Logic
{
    /// <summary>
    /// ET text asset loader backed by local config files.
    /// Provides JSON text and byte loading for formal MOBA config pipelines.
    /// </summary>
    [WorldService(typeof(ITextAssetLoader), WorldLifetime.Singleton)]
    public sealed class ETTextAssetLoader : ITextAssetLoader, ITextAssetDirectoryLoader
    {
        private readonly string _basePath;

        public ETTextAssetLoader() : this(GetDefaultBasePath())
        {
        }

        public ETTextAssetLoader(string basePath)
        {
            _basePath = string.IsNullOrEmpty(basePath) ? GetDefaultBasePath() : basePath;
            Log.Info($"[ETTextAssetLoader] Created with basePath: {_basePath}");
        }

        public bool TryLoadText(string path, out string text)
        {
            text = null;
            if (string.IsNullOrEmpty(path)) return false;

            var fullPath = GetFullPath(path);

            if (!File.Exists(fullPath))
            {
                Log.Warning($"[ETTextAssetLoader] File not found: {fullPath}");
                return false;
            }

            try
            {
                text = File.ReadAllText(fullPath);
                Log.Info($"[ETTextAssetLoader] Loaded: {fullPath}");
                return !string.IsNullOrEmpty(text);
            }
            catch (Exception ex)
            {
                Log.Warning($"[ETTextAssetLoader] Failed to read: {fullPath}, Error: {ex.Message}");
                return false;
            }
        }

        public bool TryLoadBytes(string path, out byte[] bytes)
        {
            bytes = null;
            if (string.IsNullOrEmpty(path)) return false;

            var fullPath = GetFullPath(path);

            if (!File.Exists(fullPath))
            {
                return false;
            }

            try
            {
                bytes = File.ReadAllBytes(fullPath);
                return bytes != null && bytes.Length > 0;
            }
            catch (Exception ex)
            {
                Log.Warning($"[ETTextAssetLoader] Failed to read bytes: {fullPath}, Error: {ex.Message}");
                return false;
            }
        }

        public IEnumerable<string> GetTextAssetPaths(string directory, string pattern)
        {
            if (string.IsNullOrEmpty(directory)) return Array.Empty<string>();

            var fullDir = GetFullPath(directory);
            if (!Directory.Exists(fullDir))
            {
                return Array.Empty<string>();
            }

            var searchOption = pattern != null && pattern.Contains("**")
                ? SearchOption.AllDirectories
                : SearchOption.TopDirectoryOnly;
            var searchPattern = string.IsNullOrEmpty(pattern)
                ? "*.json"
                : pattern.Replace("**/", string.Empty).Replace("**", "*");

            try
            {
                var files = Directory.GetFiles(fullDir, searchPattern, searchOption)
                    .Where(f => f.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                    .Select(GetRelativePath)
                    .ToArray();
                return files;
            }
            catch (Exception ex)
            {
                Log.Warning($"[ETTextAssetLoader] Failed to enumerate files: {fullDir}, Error: {ex.Message}");
                return Array.Empty<string>();
            }
        }

        private string GetFullPath(string path)
        {
            if (string.IsNullOrEmpty(path)) return _basePath;

            var normalizedPath = path.Replace('/', Path.DirectorySeparatorChar);

            if (Path.IsPathRooted(normalizedPath))
            {
                return normalizedPath;
            }
            return Path.Combine(_basePath, normalizedPath);
        }

        private string GetRelativePath(string fullPath)
        {
            if (string.IsNullOrEmpty(fullPath)) return fullPath;

            var relative = fullPath.StartsWith(_basePath, StringComparison.OrdinalIgnoreCase)
                ? fullPath.Substring(_basePath.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                : fullPath;
            return relative.Replace('\\', '/');
        }

        private static string GetDefaultBasePath()
        {
            var exeDir = AppDomain.CurrentDomain.BaseDirectory;
            var configDir = Path.Combine(exeDir, "Configs");

            if (Directory.Exists(configDir))
            {
                return configDir;
            }

            // Walk parent directories to support running from nested build folders.
            var current = new DirectoryInfo(exeDir);
            while (current != null)
            {
                var testPath = Path.Combine(current.FullName, "Configs");
                if (Directory.Exists(testPath))
                {
                    return testPath;
                }
                current = current.Parent;
            }

            // Fall back to the executable directory.
            return exeDir;
        }
    }
}
