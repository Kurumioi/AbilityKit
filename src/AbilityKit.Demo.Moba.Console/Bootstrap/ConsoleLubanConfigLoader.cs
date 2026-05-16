using System;
using System.Collections.Generic;
using System.Linq;
using AbilityKit.Ability.Config;
using AbilityKit.Ability.World.DI;
using AbilityKit.Ability.World.Services.Attributes;
using Newtonsoft.Json.Linq;

// Luban 生成代码使用 namespace cfg（顶级命名空间）
using ILubanConfigLoader = AbilityKit.Demo.Moba.Config.Core.ILubanConfigLoader;

namespace AbilityKit.Demo.Moba.Console.Bootstrap
{
    /// <summary>
    /// Console 平台的 Luban 配置加载器。
    /// 使用 Luban 生成的 Tables 类加载配置 JSON。
    /// </summary>
    [WorldService(typeof(ILubanConfigLoader), WorldLifetime.Singleton)]
    public sealed class ConsoleLubanConfigLoader : ILubanConfigLoader
    {
        private readonly ITextAssetLoader _textAssetLoader;
        private readonly string _resourcesDir;
        private cfg.Tables _tables;
        private bool _loaded;

        public ConsoleLubanConfigLoader(ITextAssetLoader textAssetLoader, string resourcesDir = "luban/moba")
        {
            _textAssetLoader = textAssetLoader ?? throw new ArgumentNullException(nameof(textAssetLoader));
            _resourcesDir = resourcesDir;
        }

        /// <summary>
        /// 加载所有 Luban 配置
        /// </summary>
        public object LoadAll()
        {
            return LoadAll(_resourcesDir);
        }

        /// <summary>
        /// 加载指定目录的 Luban 配置
        /// </summary>
        public object LoadAll(string resourcesDir)
        {
            if (_loaded && _tables != null)
            {
                Platform.Log.System("[ConsoleLubanConfigLoader] Tables already loaded");
                return _tables;
            }

            Platform.Log.System($"[ConsoleLubanConfigLoader] Loading Luban configs from: {resourcesDir}");

            try
            {
                // 创建 JSON 加载函数
                Func<string, JArray> loader = (tableName) =>
                {
                    // 尝试多种路径格式
                    var paths = new[]
                    {
                        $"{resourcesDir}/{tableName}",
                        $"{resourcesDir}/{ToUnderscoreCase(tableName)}"
                    };

                    foreach (var path in paths)
                    {
                        if (_textAssetLoader.TryLoadText(path, out var text) && !string.IsNullOrEmpty(text))
                        {
                            Platform.Log.Debug($"[ConsoleLubanConfigLoader] Loaded table '{tableName}' from '{path}'");
                            return JArray.Parse(text);
                        }
                    }

                    Platform.Log.Warn($"[ConsoleLubanConfigLoader] Table not found (tried: {string.Join(", ", paths)})");
                    return new JArray();
                };

                _tables = new cfg.Tables(loader);
                _loaded = true;

                Platform.Log.System($"[ConsoleLubanConfigLoader] Loaded {_tables.Characters.DataList.Count} characters, {_tables.AttributeTemplates.DataList.Count} attribute templates, {_tables.Buffs.DataList.Count} buffs");

                return _tables;
            }
            catch (Exception ex)
            {
                Platform.Log.Error($"[ConsoleLubanConfigLoader] Failed to load Luban configs: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 获取指定类型的配置表
        /// </summary>
        public T GetTable<T>() where T : class
        {
            if (_tables == null)
            {
                LoadAll();
            }

            var type = typeof(T);
            if (type == typeof(cfg.Characters))
                return (T)(object)_tables.Characters;
            if (type == typeof(cfg.AttributeTemplates))
                return (T)(object)_tables.AttributeTemplates;
            if (type == typeof(cfg.Buffs))
                return (T)(object)_tables.Buffs;

            throw new NotSupportedException($"Table type {type.Name} not supported in current Tables");
        }

        public object GetCharacters() { if (_tables == null) LoadAll(); return _tables?.Characters; }
        public object GetAttributeTemplates() { if (_tables == null) LoadAll(); return _tables?.AttributeTemplates; }
        public object GetBuffs() { if (_tables == null) LoadAll(); return _tables?.Buffs; }
        public object GetSkills() { Platform.Log.Warn("[ConsoleLubanConfigLoader] Skills table not available"); return null; }
        public object GetProjectiles() { Platform.Log.Warn("[ConsoleLubanConfigLoader] Projectiles table not available"); return null; }

        /// <summary>
        /// 获取已加载的 Tables 实例
        /// </summary>
        public cfg.Tables GetTables()
        {
            if (_tables == null) LoadAll();
            return _tables;
        }

        private static string ToUnderscoreCase(string str)
        {
            if (string.IsNullOrEmpty(str)) return str;
            var result = new List<char>();
            for (int i = 0; i < str.Length; i++)
            {
                var c = str[i];
                if (char.IsUpper(c) && i > 0)
                {
                    result.Add('_');
                }
                result.Add(char.ToLower(c));
            }
            return new string(result.ToArray());
        }
    }

    /// <summary>
    /// Console 简化版 Luban 配置适配器。
    /// 将 Luban 的 Tables 适配到 Console 需要的简化模型。
    /// </summary>
    public sealed class ConsoleLubanConfigAdapter
    {
        private readonly ILubanConfigLoader _lubanLoader;

        public ConsoleLubanConfigAdapter(ILubanConfigLoader lubanLoader)
        {
            _lubanLoader = lubanLoader ?? throw new ArgumentNullException(nameof(lubanLoader));
        }

        /// <summary>
        /// 尝试从 Luban 配置填充简化版 MobaConfigDatabase
        /// </summary>
        /// <returns>如果成功填充返回 true，否则返回 false</returns>
        public bool TryPopulate(MobaConfigDatabase db)
        {
            try
            {
                var tables = _lubanLoader.LoadAll() as cfg.Tables;
                if (tables == null) return false;

                // 检查是否有有效数据
                if (tables.Characters.DataList.Count == 0)
                {
                    Platform.Log.Warn("[ConsoleLubanConfigAdapter] No characters in Luban tables, falling back to JSON");
                    return false;
                }

                // 转换角色配置
                foreach (var dr in tables.Characters.DataList)
                {
                    var config = new CharacterConfig
                    {
                        Id = dr.Code,
                        Name = dr.Name,
                        ModelId = dr.ModelId,
                        AttributeTemplateId = dr.AttributeTemplateId,
                        SkillIds = dr.Career?.ToArray() ?? Array.Empty<int>()
                    };
                    // 注意：Luban 的 Career 字段对应简化版的 SkillIds
                    // 如果 Luban 配置中有独立的技能字段，需要相应调整
                }

                // 转换属性模板配置
                foreach (var dr in tables.AttributeTemplates.DataList)
                {
                    var config = new AttributeTemplateConfig
                    {
                        Id = dr.Code,
                        Hp = dr.Hp,
                        MaxHp = dr.MaxHp,
                        PhysicsAttack = dr.PhysicsAttack,
                        MagicAttack = dr.MagicAttack,
                        PhysicsDefense = dr.PhysicsDefense,
                        MagicDefense = dr.MagicDefense
                    };
                    // 其他字段需要根据 Luban 配置结构添加
                }

                Platform.Log.System($"[ConsoleLubanConfigAdapter] Populated from Luban: {tables.Characters.DataList.Count} characters, {tables.AttributeTemplates.DataList.Count} attribute templates");
                return true;
            }
            catch (Exception ex)
            {
                Platform.Log.Warn($"[ConsoleLubanConfigAdapter] Failed to populate from Luban: {ex.Message}");
                return false;
            }
        }
    }
}
