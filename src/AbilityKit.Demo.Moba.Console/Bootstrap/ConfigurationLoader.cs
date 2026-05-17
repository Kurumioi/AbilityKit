using System;
using AbilityKit.Ability.Config;
using AbilityKit.Demo.Moba.Console.Platform;
using AbilityKit.Demo.Moba.Console.Battle;

namespace AbilityKit.Demo.Moba.Console.Bootstrap
{
    /// <summary>
    /// 配置加载器
    /// 统一管理战斗启动配置和 Moba 配置的加载
    /// </summary>
    public sealed class ConfigurationLoader
    {
        private readonly ITextAssetLoader _textAssetLoader;

        public ConfigurationLoader(ITextAssetLoader? textAssetLoader = null)
        {
            _textAssetLoader = textAssetLoader ?? new ConsoleTextAssetLoader();
        }

        /// <summary>
        /// 加载战斗启动配置
        /// </summary>
        public BattleStartConfig LoadBattleConfig()
        {
            var configPath = $"{ConsoleConfigLoader.MobaConfigDir}/battle_start";
            if (_textAssetLoader.TryLoadText(configPath, out var json) && !string.IsNullOrEmpty(json))
            {
                try
                {
                    var config = Newtonsoft.Json.JsonConvert.DeserializeObject<BattleStartConfig>(json);
                    if (config != null)
                    {
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
            return BattleStartConfig.CreateDefault();
        }

        /// <summary>
        /// 加载 Moba 配置数据库
        /// </summary>
        public ConsoleMobaConfigDatabase LoadMobaConfig()
        {
            var db = new ConsoleMobaConfigDatabase(_textAssetLoader);
            db.LoadFromResources();
            return db;
        }

        /// <summary>
        /// 记录配置信息
        /// </summary>
        public void LogConfig(BattleStartConfig config, ConsoleMobaConfigDatabase mobaDb)
        {
            Log.Config("=== Battle Configuration ===");
            Log.Config($"  World: {config.WorldId} ({config.WorldType})");
            Log.Config($"  Sync: {config.SyncMode}, TickRate: {config.TickRate}");
            Log.Config($"  Max Players: {config.MaxPlayerCount}");
            Log.Config($"  Debug: {config.EnableDebug}");
            Log.Config($"  Input Delay: {config.InputDelayFrames} frames");

            if (mobaDb.CharacterCount > 0)
            {
                Log.Config("  Characters:");
                foreach (var c in mobaDb.GetAllCharacters())
                {
                    var attrs = mobaDb.GetCharacterAttributes(c);
                    var hp = attrs?.Hp ?? 0;
                    var atk = attrs?.PhysicsAttack ?? 0;
                    var def = attrs?.PhysicsDefense ?? 0;
                    Log.Config($"    - {c.Name} (HP:{hp:F0}, ATK:{atk:F0}, DEF:{def:F0}, TemplateId:{c.AttributeTemplateId})");
                }
            }

            Log.Config("============================");
        }
    }
}
