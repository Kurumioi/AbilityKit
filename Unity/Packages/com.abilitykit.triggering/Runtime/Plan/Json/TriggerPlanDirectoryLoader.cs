using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace AbilityKit.Triggering.Runtime.Plan.Json
{
    /// <summary>
    /// 触发器计划目录加载器实现
    /// 支持多文件分离的触发器配置加载
    /// </summary>
    public sealed class TriggerPlanDirectoryLoader : ITriggerPlanDirectoryLoader, ITriggerPlanFileEnumerator
    {
        private readonly TriggerPlanJsonDatabase.ITextLoader _textLoader;

        /// <summary>
        /// 创建一个新的目录加载器
        /// </summary>
        /// <param name="textLoader">文本加载器（用于从 Resources 或文件系统加载）</param>
        public TriggerPlanDirectoryLoader(TriggerPlanJsonDatabase.ITextLoader textLoader)
        {
            _textLoader = textLoader ?? throw new ArgumentNullException(nameof(textLoader));
        }

        /// <inheritdoc />
        public TriggerPlanJsonDatabase LoadDirectory(string directory, string pattern = "*.json")
        {
            if (string.IsNullOrEmpty(directory))
                throw new ArgumentException("directory cannot be null or empty", nameof(directory));

            var files = GetFiles(directory, pattern).ToArray();
            LogTrace($"[AI-DIAG] [TriggerPlanDirectoryLoader] LoadDirectory. directory={directory}, pattern={pattern}, fileCount={files.Length}, files={string.Join(",", files)}");
            return LoadFiles(files);
        }

        /// <inheritdoc />
        public TriggerPlanJsonDatabase LoadDirectories(IEnumerable<string> directories, string pattern = "*.json")
        {
            if (directories == null)
                throw new ArgumentNullException(nameof(directories));

            var allFiles = new List<string>();
            foreach (var dir in directories)
            {
                if (string.IsNullOrEmpty(dir)) continue;
                allFiles.AddRange(GetFiles(dir, pattern));
            }

            return LoadFiles(allFiles);
        }

        /// <inheritdoc />
        public TriggerPlanJsonDatabase LoadWithManifest(string manifestPath, string moduleDirectory)
        {
            if (string.IsNullOrEmpty(manifestPath))
                throw new ArgumentException("manifestPath cannot be null or empty", nameof(manifestPath));
            if (string.IsNullOrEmpty(moduleDirectory))
                throw new ArgumentException("moduleDirectory cannot be null or empty", nameof(moduleDirectory));

            if (!_textLoader.TryLoad(manifestPath, out var manifestContent) || string.IsNullOrEmpty(manifestContent))
            {
                throw new InvalidOperationException($"Manifest file not found or empty: {manifestPath}");
            }

            var manifest = JsonConvert.DeserializeObject<TriggerPlanManifest>(manifestContent);
            if (manifest?.Entries == null || manifest.Entries.Count == 0)
            {
                return new TriggerPlanJsonDatabase();
            }

            var allFiles = new List<string>();
            foreach (var entry in manifest.Entries)
            {
                if (string.IsNullOrEmpty(entry.Path)) continue;
                var fullPath = NormalizePath(moduleDirectory, entry.Path);
                allFiles.Add(fullPath);
            }

            return LoadFiles(allFiles);
        }

        /// <inheritdoc />
        public IEnumerable<string> GetFiles(string directory, string pattern)
        {
            if (string.IsNullOrEmpty(directory))
                return Enumerable.Empty<string>();

            if (_textLoader is IFileSystemTextLoader fsLoader)
            {
                return fsLoader.GetFiles(directory, pattern);
            }

#if UNITY_EDITOR || (!UNITY_IOS && !UNITY_ANDROID)
            try
            {
                if (Directory.Exists(directory))
                {
                    return Directory.GetFiles(directory, pattern, SearchOption.AllDirectories)
                        .Where(f => f.EndsWith(".json", StringComparison.OrdinalIgnoreCase));
                }
            }
            catch
            {
            }
#endif

            return Enumerable.Empty<string>();
        }

        /// <inheritdoc />
        public bool TryReadFile(string path, out string content)
        {
            return _textLoader.TryLoad(path, out content);
        }

        private TriggerPlanJsonDatabase LoadFiles(IEnumerable<string> files)
        {
            var db = new TriggerPlanJsonDatabase();
            var mergedDto = new TriggerPlanJsonDatabase.TriggerPlanDatabaseDto
            {
                FormatVersion = 1,
                Triggers = new List<TriggerPlanJsonDatabase.TriggerPlanDto>(),
                Strings = new Dictionary<int, string>()
            };

            foreach (var file in files)
            {
                if (!_textLoader.TryLoad(file, out var content) || string.IsNullOrEmpty(content))
                {
                    LogTrace($"[AI-DIAG] [TriggerPlanDirectoryLoader] Skip empty/missing file. file={file}");
                    continue;
                }

                try
                {
                    var runtimeDto = TriggerPlanJsonDatabase.ParseRuntimeDto(content, file);
                    var triggerCount = runtimeDto?.Triggers?.Count ?? 0;
                    var triggerSummary = runtimeDto?.Triggers == null
                        ? string.Empty
                        : string.Join(",", runtimeDto.Triggers.Select(t => $"{t.TriggerId}:{t.EventName}:scope={(int)t.Scope}"));
                    LogTrace($"[AI-DIAG] [TriggerPlanDirectoryLoader] Parsed file. file={file}, triggerCount={triggerCount}, triggers={triggerSummary}");
                    if (runtimeDto?.Triggers != null)
                    {
                        mergedDto.Triggers.AddRange(runtimeDto.Triggers);
                    }

                    if (runtimeDto?.Strings != null)
                    {
                        foreach (var kvp in runtimeDto.Strings)
                        {
                            mergedDto.Strings[kvp.Key] = kvp.Value;
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogWarning($"[TriggerPlanDirectoryLoader] Failed to load file {file}: {ex.Message}");
                }
            }

            LogTrace($"[AI-DIAG] [TriggerPlanDirectoryLoader] Merged dto. triggerCount={mergedDto.Triggers.Count}, stringCount={mergedDto.Strings.Count}");
            db.LoadFromDto(mergedDto);
            return db;
        }

        private static string NormalizePath(string baseDir, string relativePath)
        {
            if (string.IsNullOrEmpty(relativePath)) return baseDir;
            return Path.Combine(baseDir, relativePath).Replace('\\', '/');
        }

        private static void LogWarning(string message)
        {
#if UNITY_EDITOR
            UnityEngine.Debug.LogWarning(message);
#else
            Console.Error.WriteLine(message);
#endif
        }

        private static void LogTrace(string message)
        {
#if UNITY_EDITOR
            UnityEngine.Debug.Log(message);
#else
            Console.WriteLine(message);
#endif
        }

        #region JSON DTOs

        private class TriggerPlanManifest
        {
            [JsonProperty("entries")]
            public List<ManifestEntry> Entries;
        }

        private class ManifestEntry
        {
            [JsonProperty("trigger_id")]
            public int TriggerId;

            [JsonProperty("path")]
            public string Path;
        }

        #endregion
    }

    /// <summary>
    /// 文件系统文本加载器接口
    /// </summary>
    public interface IFileSystemTextLoader : TriggerPlanJsonDatabase.ITextLoader
    {
        /// <summary>
        /// 获取目录下匹配的文件
        /// </summary>
        IEnumerable<string> GetFiles(string directory, string pattern);
    }
}
