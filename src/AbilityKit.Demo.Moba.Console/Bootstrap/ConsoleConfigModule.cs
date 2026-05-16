using System;
using System.Collections.Generic;
using AbilityKit.Ability.Config;
using AbilityKit.Ability.World.DI;
using AbilityKit.Ability.World.Services.Attributes;
using AbilityKit.Demo.Moba.Config.BattleDemo;
using AbilityKit.Demo.Moba.Config.Core;

namespace AbilityKit.Demo.Moba.Console.Bootstrap
{
    /// <summary>
    /// Console 环境的配置服务模块。
    /// 负责注册配置相关的 DI 服务，统一管理配置加载。
    /// 
    /// 配置加载优先级（从高到低）：
    /// 1. Luban 导出配置（高性能，策划使用 Luban 编辑）
    /// 2. 简化版 JSON 配置（快速原型，Fallback）
    /// </summary>
    public sealed class ConsoleConfigModule : IWorldModule
    {
        private readonly string _resourcesDir;
        private readonly string _lubanResourcesDir;

        /// <summary>
        /// 创建配置模块
        /// </summary>
        /// <param name="resourcesDir">简化版 JSON 配置目录（默认 moba）</param>
        /// <param name="lubanResourcesDir">Luban 配置目录（默认 luban/moba）</param>
        public ConsoleConfigModule(string resourcesDir = "moba", string lubanResourcesDir = "luban/moba")
        {
            _resourcesDir = resourcesDir;
            _lubanResourcesDir = lubanResourcesDir;
        }

        public void Configure(WorldContainerBuilder builder)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));

            // 注册 ITextAssetLoader（平台相关实现）
            builder.Register<ITextAssetLoader>(WorldLifetime.Singleton, _ => new ConsoleTextAssetLoader());

            // 注册配置表注册器（逻辑层使用）
            builder.Register<IMobaConfigTableRegistry>(WorldLifetime.Singleton, _ => MobaConfigRegistry.Instance);

            // 注册配置表的反序列化器（逻辑层使用）
            builder.Register<IMobaConfigDtoDeserializer>(WorldLifetime.Singleton, _ => JsonNetMobaConfigDtoDeserializer.Instance);
            builder.Register<IMobaConfigDtoBytesDeserializer>(WorldLifetime.Singleton, _ => new LubanMobaConfigDtoBytesDeserializer());

            // 注册逻辑层配置加载器（用于需要完整 MO 对象的场景）
            builder.Register<DefaultMobaConfigLoader>(WorldLifetime.Singleton, container =>
            {
                var textAssetLoader = container.Resolve<ITextAssetLoader>();
                var registry = container.Resolve<IMobaConfigTableRegistry>();
                return new DefaultMobaConfigLoader(registry, textAssetLoader);
            });

            // 注册 Luban 配置组（使用 IConfigGroup 模式）
            builder.Register<LubanConfigGroup>(WorldLifetime.Singleton, container =>
            {
                var textAssetLoader = container.Resolve<ITextAssetLoader>();
                return LubanConfigGroup.Create(textAssetLoader, _lubanResourcesDir);
            });

            // 注册 Luban 配置加载器（旧接口，保持兼容）
            builder.Register<ILubanConfigLoader>(WorldLifetime.Singleton, container =>
            {
                var textAssetLoader = container.Resolve<ITextAssetLoader>();
                return new ConsoleLubanConfigLoader(textAssetLoader, _lubanResourcesDir);
            });

            // 注册框架版 MobaConfigDatabase（用于完整的 MO 配置）
            builder.Register<AbilityKit.Demo.Moba.Config.Core.MobaConfigDatabase>(WorldLifetime.Singleton, container =>
            {
                var registry = container.Resolve<IMobaConfigTableRegistry>();
                var deserializer = container.Resolve<IMobaConfigDtoDeserializer>();
                var textAssetLoader = container.Resolve<ITextAssetLoader>();

                // 优先尝试 Luban 配置组
                var lubanGroup = container.Resolve<LubanConfigGroup>();
                var db = new AbilityKit.Demo.Moba.Config.Core.MobaConfigDatabase(registry, deserializer, null, textAssetLoader);

                try
                {
                    db.LoadFromGroups(new List<IConfigGroup> { lubanGroup });
                    Platform.Log.System("[ConsoleConfigModule] MobaConfigDatabase loaded from Luban config group");
                }
                catch (Exception lubanEx)
                {
                    Platform.Log.Warn($"[ConsoleConfigModule] Failed to load from Luban config group: {lubanEx.Message}");

                    // Fallback 到简化版 JSON 配置
                    try
                    {
                        db.LoadFromResources(_resourcesDir);
                        Platform.Log.System("[ConsoleConfigModule] MobaConfigDatabase loaded from JSON configs (fallback)");
                    }
                    catch (Exception jsonEx)
                    {
                        Platform.Log.Error($"[ConsoleConfigModule] Failed to load from JSON configs: {jsonEx.Message}");
                    }
                }

                return db;
            });

            // 注册简化版 MobaConfigDatabase（Console 专用）
            builder.Register<MobaConfigDatabase>(WorldLifetime.Singleton, container =>
            {
                var textAssetLoader = container.Resolve<ITextAssetLoader>();
                return ConsoleConfigLoader.LoadMobaConfig(textAssetLoader);
            });
        }
    }
}
