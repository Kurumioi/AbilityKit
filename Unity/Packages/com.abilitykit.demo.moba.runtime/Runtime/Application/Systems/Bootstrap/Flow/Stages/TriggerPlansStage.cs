using System;
using System.Collections.Generic;
using System.Linq;
using AbilityKit.Ability.Config;
using AbilityKit.Ability.Triggering.Json;
using AbilityKit.Demo.Moba.Services;
using AbilityKit.Triggering.Runtime.Plan.Json;
using AbilityKit.Ability.World.DI;
using AbilityKit.Demo.Moba.Systems.Bootstrap;
using AbilityKit.Demo.Moba.Systems.Bootstrap.Flow;

namespace AbilityKit.Demo.Moba.Systems.Bootstrap.Flow.Stages
{
    /// <summary>
    /// TriggerPlans Stage
    /// 注册触发器计划相关的服务
    /// </summary>
    [MobaBootstrapStage]
    public sealed class TriggerPlansStage : MobaBootstrapStageBase
    {
        public override string Name => "TriggerPlans";

        protected internal override void Configure(WorldContainerBuilder builder)
        {
            // 注意：ITextAssetLoader 已经通过 ResourcesTextAssetLoader 的 WorldServiceAttribute 自动注册

            // 注册 TriggerPlanJsonDatabase.ITextLoader
            builder.TryRegister<TriggerPlanJsonDatabase.ITextLoader>(WorldLifetime.Singleton, r =>
            {
                var textAssetLoader = r.Resolve<ITextAssetLoader>();
                var jsonLoader = new UnityResourcesTextLoader(textAssetLoader);
                return new PlanTextLoaderAdapter(jsonLoader);
            });

            builder.TryRegister<MobaTriggerPlanLoadProfile>(WorldLifetime.Singleton, _ => MobaTriggerPlanLoadProfile.Default);

            builder.TryRegister<TriggerPlanJsonDatabase>(WorldLifetime.Singleton, r =>
            {
                var db = new TriggerPlanJsonDatabase();
                var textAssetLoader = r.Resolve<ITextAssetLoader>();
                var fsAdapter = new EtFileSystemAdapter(textAssetLoader);
                var directoryLoader = new TriggerPlanDirectoryLoader(fsAdapter);
                var profile = r.Resolve<MobaTriggerPlanLoadProfile>() ?? MobaTriggerPlanLoadProfile.Default;

                LoadEntries(db, fsAdapter, directoryLoader, profile.Entries);

                return db;
            });
        }

        private static void LoadEntries(TriggerPlanJsonDatabase db, EtFileSystemAdapter fsAdapter, TriggerPlanDirectoryLoader directoryLoader, TriggerPlanLoadEntry[] entries)
        {
            if (entries == null) return;

            for (int i = 0; i < entries.Length; i++)
            {
                var entry = entries[i];
                if (string.IsNullOrEmpty(entry.Path)) continue;

                if (entry.IsDirectory)
                {
                    LoadDirectory(db, directoryLoader, entry);
                }
                else
                {
                    LoadFile(db, fsAdapter, entry);
                }
            }
        }

        private static void LoadFile(TriggerPlanJsonDatabase db, EtFileSystemAdapter fsAdapter, TriggerPlanLoadEntry entry)
        {
            MobaRuntimeLog.Info(MobaRuntimeLogModule.Bootstrap, MobaRuntimeLogPurpose.Configuration, nameof(TriggerPlansStage), $"Loading {entry.Name} from {entry.Path}");
            try
            {
                var loadedDb = new TriggerPlanJsonDatabase();
                loadedDb.Load(fsAdapter, entry.Path);
                db.MergeFrom(loadedDb, replaceExisting: true);
                MobaRuntimeLog.Info(MobaRuntimeLogModule.Bootstrap, MobaRuntimeLogPurpose.Configuration, nameof(TriggerPlansStage), $"{entry.Name} merged. total records={db.Records?.Count ?? 0}");
            }
            catch (Exception ex)
            {
                MobaRuntimeLog.Exception(ex, MobaRuntimeLogModule.Bootstrap, MobaRuntimeLogPurpose.Exception, nameof(TriggerPlansStage), $"Failed to load required trigger plan file. name={entry.Name}, path={entry.Path}");
                throw;
            }
        }

        private static void LoadDirectory(TriggerPlanJsonDatabase db, TriggerPlanDirectoryLoader directoryLoader, TriggerPlanLoadEntry entry)
        {
            var pattern = string.IsNullOrEmpty(entry.Pattern) ? "**/*.json" : entry.Pattern;
            MobaRuntimeLog.Info(MobaRuntimeLogModule.Bootstrap, MobaRuntimeLogPurpose.Configuration, nameof(TriggerPlansStage), $"Loading {entry.Name} from {entry.Path} directory");
            try
            {
                var loadedDb = directoryLoader.LoadDirectory(entry.Path, pattern);
                if (loadedDb != null && loadedDb.Records != null)
                {
                    db.MergeFrom(loadedDb, replaceExisting: true);
                    MobaRuntimeLog.Info(MobaRuntimeLogModule.Bootstrap, MobaRuntimeLogPurpose.Configuration, nameof(TriggerPlansStage), $"{entry.Name} merged. total records={db.Records?.Count ?? 0}");
                }
            }
            catch (Exception ex)
            {
                MobaRuntimeLog.Exception(ex, MobaRuntimeLogModule.Bootstrap, MobaRuntimeLogPurpose.Exception, nameof(TriggerPlansStage), $"Failed to load required trigger plan directory. name={entry.Name}, path={entry.Path}, pattern={pattern}");
                throw;
            }
        }

        /// <summary>
        /// Text asset adapter for trigger plan loading.
        /// </summary>
        private sealed class EtFileSystemAdapter :
            AbilityKit.Ability.Triggering.Json.ITextLoader,
            IFileSystemTextLoader
        {
            private readonly ITextAssetLoader _textAssetLoader;
            private readonly ITextAssetDirectoryLoader _directoryLoader;

            public EtFileSystemAdapter(ITextAssetLoader textAssetLoader)
            {
                _textAssetLoader = textAssetLoader ?? throw new ArgumentNullException(nameof(textAssetLoader));
                _directoryLoader = textAssetLoader as ITextAssetDirectoryLoader;
            }

            bool AbilityKit.Ability.Triggering.Json.ITextLoader.TryLoad(string id, out string text)
            {
                text = null;
                if (string.IsNullOrEmpty(id)) return false;
                return _textAssetLoader.TryLoadText(id, out text);
            }

            bool AbilityKit.Triggering.Runtime.Plan.Json.TriggerPlanJsonDatabase.ITextLoader.TryLoad(string id, out string text)
            {
                return ((AbilityKit.Ability.Triggering.Json.ITextLoader)this).TryLoad(id, out text);
            }

            public IEnumerable<string> GetFiles(string directory, string pattern)
            {
                if (_directoryLoader == null) return Enumerable.Empty<string>();
                return _directoryLoader.GetTextAssetPaths(directory, pattern) ?? Enumerable.Empty<string>();
            }
        }

    }
}
