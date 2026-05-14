using System;
using System.Collections.Generic;
using AbilityKit.Ability.Config;
using AbilityKit.Demo.Moba.Console.Battle;
using AbilityKit.Demo.Moba.Console.Platform;

namespace AbilityKit.Demo.Moba.Console.Bootstrap
{
    public static class ConsoleConfigLoader
    {
        public const string ConfigDirName = "Configs";
        public const string MobaConfigDir = "moba";

        public static BattleStartConfig LoadBattleStartConfig(ITextAssetLoader? loader = null)
        {
            loader ??= new ConsoleTextAssetLoader();

            var configPath = $"{MobaConfigDir}/battle_start";
            if (loader.TryLoadText(configPath, out var json) && !string.IsNullOrEmpty(json))
            {
                try
                {
                    var config = Newtonsoft.Json.JsonConvert.DeserializeObject<BattleStartConfig>(json);
                    if (config != null)
                    {
                        // 应用环境变量覆盖
                        ApplyEnvironmentOverrides(config);
                        Log.System($"Loaded BattleStartConfig from: {configPath}");
                        return config;
                    }
                }
                catch (Exception ex)
                {
                    Log.Warn($"Failed to parse BattleStartConfig from {configPath}: {ex.Message}");
                }
            }

            Log.System("Using default BattleStartConfig");
            var defaultConfig = BattleStartConfig.CreateDefault();
            ApplyEnvironmentOverrides(defaultConfig);
            return defaultConfig;
        }

        /// <summary>
        /// 应用环境变量覆盖配置
        /// 支持以下环境变量：
        /// - SYNC_MODE: Lockstep, SnapshotAuthority, Hybrid
        /// </summary>
        private static void ApplyEnvironmentOverrides(BattleStartConfig config)
        {
            // 检查 SYNC_MODE 环境变量
            var syncMode = Environment.GetEnvironmentVariable("SYNC_MODE");
            if (!string.IsNullOrEmpty(syncMode))
            {
                if (Enum.TryParse<BattleSyncMode>(syncMode, ignoreCase: true, out var mode))
                {
                    config.SyncMode = mode;
                    Log.System($"[ConfigOverride] SyncMode set to: {mode}");
                }
                else
                {
                    Log.Warn($"[ConfigOverride] Unknown SyncMode: {syncMode}, ignoring");
                }
            }
        }

        public static MobaConfigDatabase LoadMobaConfig(ITextAssetLoader? loader = null)
        {
            loader ??= new ConsoleTextAssetLoader();
            return new MobaConfigDatabase(loader);
        }

        public static MobaConfigDatabase LoadDefault()
        {
            return LoadMobaConfig();
        }
    }
}
