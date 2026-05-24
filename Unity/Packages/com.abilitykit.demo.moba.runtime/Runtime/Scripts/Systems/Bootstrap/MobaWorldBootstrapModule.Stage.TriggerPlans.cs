using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AbilityKit.Ability.Config;
using AbilityKit.Ability.Triggering.Json;
using AbilityKit.Core.Common.Log;
using AbilityKit.Triggering.Runtime.Plan.Json;
using AbilityKit.Ability.World.DI;

namespace AbilityKit.Demo.Moba.Systems
{
    public sealed partial class MobaWorldBootstrapModule
    {
        private static void RegisterTriggerPlans(WorldContainerBuilder builder)
        {
            // 注意：ITextAssetLoader 已经通过 ResourcesTextAssetLoader 的 WorldServiceAttribute 自动注册

            // 注册 TriggerPlanJsonDatabase.ITextLoader
            builder.TryRegister<TriggerPlanJsonDatabase.ITextLoader>(WorldLifetime.Singleton, r =>
            {
                var textAssetLoader = r.Resolve<ITextAssetLoader>();
                var jsonLoader = new UnityResourcesTextLoader(textAssetLoader);
                return new PlanTextLoaderAdapter(jsonLoader);
            });

            builder.TryRegister<PlanActionModuleRegistry>(WorldLifetime.Singleton, _ => PlanActionModuleRegistry.CreateDefault());

            builder.TryRegister<TriggerPlanJsonDatabase>(WorldLifetime.Singleton, r =>
            {
                var db = new TriggerPlanJsonDatabase();
                var textAssetLoader = r.Resolve<ITextAssetLoader>();

                // 1. 加载主配置文件 ability_trigger_plans.json（保证向后兼容）
                Log.Info("[MobaWorldBootstrapModule] Loading main trigger plans from ability/ability_trigger_plans.json");
                try
                {
                    var fsAdapter = new EtFileSystemAdapter(textAssetLoader);
                    db.Load(fsAdapter, "ability/ability_trigger_plans.json");
                    Log.Info($"[MobaWorldBootstrapModule] Main trigger plans loaded. records={db.Records?.Count ?? 0}");
                }
                catch (Exception ex)
                {
                    Log.Warning($"[MobaWorldBootstrapModule] Failed to load main trigger plans: {ex.Message}");
                }

                // 2. 从 triggers 目录加载细粒度配置
                Log.Info("[MobaWorldBootstrapModule] Loading trigger plans from ability/triggers directory");
                try
                {
                    var fsAdapter = new EtFileSystemAdapter(textAssetLoader);
                    var directoryLoader = new TriggerPlanDirectoryLoader(fsAdapter);
                    var loadedDb = directoryLoader.LoadDirectory("ability/triggers");

                    if (loadedDb != null && loadedDb.Records != null)
                    {
                        MergeDatabase(db, loadedDb);
                        Log.Info($"[MobaWorldBootstrapModule] Directory trigger plans merged. total records={db.Records?.Count ?? 0}");
                    }
                }
                catch (Exception ex)
                {
                    Log.Warning($"[MobaWorldBootstrapModule] Failed to load directory trigger plans: {ex.Message}");
                }

                return db;
            });
        }

        /// <summary>
        /// ET 环境文件系统适配器
        /// 实现 IFileSystemTextLoader，支持从目录枚举文件
        /// 同时实现 AbilityKit.Ability.Triggering.Json.ITextLoader 接口
        /// </summary>
        private sealed class EtFileSystemAdapter :
            AbilityKit.Ability.Triggering.Json.ITextLoader,
            IFileSystemTextLoader
        {
            private readonly ITextAssetLoader _textAssetLoader;
            private readonly string _basePath;

            public EtFileSystemAdapter(ITextAssetLoader textAssetLoader)
            {
                _textAssetLoader = textAssetLoader ?? throw new ArgumentNullException(nameof(textAssetLoader));
                _basePath = GetBasePath();
            }

            // 实现 AbilityKit.Ability.Triggering.Json.ITextLoader
            bool AbilityKit.Ability.Triggering.Json.ITextLoader.TryLoad(string id, out string text)
            {
                text = null;
                if (string.IsNullOrEmpty(id)) return false;

                return _textAssetLoader.TryLoadText(id, out text);
            }

            // 实现 AbilityKit.Triggering.Runtime.Plan.Json.TriggerPlanJsonDatabase.ITextLoader
            bool AbilityKit.Triggering.Runtime.Plan.Json.TriggerPlanJsonDatabase.ITextLoader.TryLoad(string id, out string text)
            {
                return ((AbilityKit.Ability.Triggering.Json.ITextLoader)this).TryLoad(id, out text);
            }

        public IEnumerable<string> GetFiles(string directory, string pattern)
        {
            if (string.IsNullOrEmpty(_basePath)) return Enumerable.Empty<string>();

            var fullDir = Path.Combine(_basePath, directory);
            if (!Directory.Exists(fullDir))
            {
                Log.Warning($"[EtFileSystemAdapter] Directory not found: {fullDir}");
                return Enumerable.Empty<string>();
            }

            var searchOption = pattern.Contains("**")
                ? SearchOption.AllDirectories
                : SearchOption.TopDirectoryOnly;

            var searchPattern = pattern.Replace("**/", "").Replace("**", "*");

            try
            {
                var files = Directory.GetFiles(fullDir, searchPattern, searchOption);
                var jsonFiles = files.Where(f => f.EndsWith(".json", StringComparison.OrdinalIgnoreCase)).ToArray();
                Log.Info($"[EtFileSystemAdapter] GetFiles: dir={directory}, pattern={pattern}, fullDir={fullDir}, found={jsonFiles.Length}");
                for (int i = 0; i < jsonFiles.Length && i < 5; i++)
                {
                    Log.Info($"[EtFileSystemAdapter]   File[{i}]: {GetRelativePath(jsonFiles[i])}");
                }
                return jsonFiles.Select(f => GetRelativePath(f));
            }
            catch (Exception ex)
            {
                Log.Warning($"[EtFileSystemAdapter] Failed to enumerate files in {fullDir}: {ex.Message}");
                return Enumerable.Empty<string>();
            }
        }

            private string GetRelativePath(string fullPath)
            {
                if (string.IsNullOrEmpty(_basePath)) return fullPath;
                var relative = fullPath.Replace(_basePath + Path.DirectorySeparatorChar, "");
                return relative.Replace('\\', '/');
            }

            private string GetBasePath()
            {
                // 通过反射获取 ETTextAssetLoader 的基础路径
                var field = _textAssetLoader.GetType().GetField("_basePath",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                return field?.GetValue(_textAssetLoader) as string ?? string.Empty;
            }
        }

        /// <summary>
        /// 合并两个 TriggerPlanJsonDatabase
        /// </summary>
        private static void MergeDatabase(TriggerPlanJsonDatabase target, TriggerPlanJsonDatabase source)
        {
            if (source == null || source.Records == null) return;

            var type = typeof(TriggerPlanJsonDatabase);

            // 获取源数据库的字段
            var srcRecordsField = type.GetField("_records", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var srcByIdField = type.GetField("_byTriggerId", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var srcStringsField = type.GetField("_strings", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            // 获取目标数据库的字段
            var tgtRecordsField = type.GetField("_records", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var tgtByIdField = type.GetField("_byTriggerId", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var tgtStringsField = type.GetField("_strings", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            // 获取源数据
            var srcRecords = srcRecordsField?.GetValue(source) as List<TriggerPlanJsonDatabase.Record>;
            var srcById = srcByIdField?.GetValue(source) as System.Collections.IDictionary;
            var srcStrings = srcStringsField?.GetValue(source) as System.Collections.IDictionary;

            // 获取目标数据
            var tgtRecords = tgtRecordsField?.GetValue(target) as List<TriggerPlanJsonDatabase.Record>;
            var tgtById = tgtByIdField?.GetValue(target) as System.Collections.IDictionary;
            var tgtStrings = tgtStringsField?.GetValue(target) as System.Collections.IDictionary;

            // 合并 Records
            if (srcRecords != null && tgtRecords != null)
            {
                foreach (var record in srcRecords)
                {
                    bool exists = false;
                    foreach (var existing in tgtRecords)
                    {
                        if (existing.TriggerId == record.TriggerId)
                        {
                            exists = true;
                            break;
                        }
                    }
                    if (!exists)
                    {
                        tgtRecords.Add(record);
                    }
                }
            }

            // 合并 _byTriggerId
            if (srcById != null && tgtById != null)
            {
                foreach (System.Collections.DictionaryEntry kvp in srcById)
                {
                    if (!tgtById.Contains(kvp.Key))
                    {
                        tgtById.Add(kvp.Key, kvp.Value);
                    }
                }
            }

            // 合并 _strings
            if (srcStrings != null && tgtStrings != null)
            {
                foreach (System.Collections.DictionaryEntry kvp in srcStrings)
                {
                    if (!tgtStrings.Contains(kvp.Key))
                    {
                        tgtStrings.Add(kvp.Key, kvp.Value);
                    }
                }
            }
        }
    }
}
