using System;
using System.Collections.Generic;
using AbilityKit.Ability.World.Abstractions;
using AbilityKit.Ability.World.DI;
using AbilityKit.Ability.World.Management;
using AbilityKit.Ability.World.Services;
using AbilityKit.Samples.Abstractions;

namespace AbilityKit.Samples.Logic.Samples.World
{
    /// <summary>
    /// World DI 概述 - 演示依赖注入和服务容器
    /// </summary>
    [Sample]
    public sealed class WorldDIOverview : SampleBase
    {
        public override string Title => "World DI Overview";
        public override string Description => "依赖注入容器、生命周期管理、服务注册";
        public override SampleCategory Category => SampleCategory.World;

        private WorldTypeRegistry _registry;

        protected override void OnRun()
        {
            Log("=== World DI 概述 ===");
            Output.Divider();

            // 1. 服务生命周期
            Log("【1】服务生命周期");
            Output.Bullet("Singleton - 单例，整个 World 期间只创建一个实例");
            Output.Bullet("Transient - 临时，每次 Resolve 都创建新实例");
            Output.Bullet("Scoped - 作用域，基于 WorldScope 创建");
            Log("");

            // 2. 定义服务接口和实现
            Log("【2】定义服务");
            Log("  public interface IGameService { void Update(); }");
            Log("  public interface IConfigService { string GetValue(string key); }");
            Log("");

            // 3. 使用 WorldContainerBuilder 构建
            Log("【3】手动构建容器");
            var builder = new WorldContainerBuilder();
            builder.RegisterServiceType<IGameService, GameService>(WorldLifetime.Singleton);
            builder.RegisterServiceType<IConfigService, ConfigService>(WorldLifetime.Transient);
            builder.RegisterServiceType<IWorldClock, WorldClock>(WorldLifetime.Singleton);
            builder.RegisterServiceType<DefaultWorldRandom, DefaultWorldRandom>(WorldLifetime.Singleton);

            var container = builder.Build();
            Log("已注册 4 个服务");
            Log("");

            // 4. 解析服务
            Log("【4】解析服务");
            var gameService1 = container.Resolve<IGameService>();
            var gameService2 = container.Resolve<IGameService>();

            Log($"Singleton 验证: {ReferenceEquals(gameService1, gameService2)}");
            Log("  (True 表示同一个实例)");

            var configService1 = container.Resolve<IConfigService>();
            var configService2 = container.Resolve<IConfigService>();

            Log($"Transient 验证: {!ReferenceEquals(configService1, configService2)}");
            Log("  (True 表示不同实例)");
            Log("");

            // 5. 使用 WorldManager 管理 World
            Log("【5】使用 WorldManager");
            _registry = new WorldTypeRegistry();
            _registry.Register("GameWorld", options => CreateWorldInstance(container, options));
            var factory = new RegistryWorldFactory(_registry);
            var worldManager = new WorldManager(factory);

            var world = worldManager.Create(new WorldCreateOptions
            {
                Id = new WorldId("game-world-1"),
                WorldType = "GameWorld"
            });

            Log($"创建 World: {world.Id.Value}");
            Log($"WorldType: {world.WorldType}");
            Log("");

            // 6. 从 World 解析服务
            Log("【6】从 World 解析服务");
            var serviceFromWorld = world.Services.Resolve<IGameService>();
            Log($"从 World 获取 IGameService: {serviceFromWorld != null}");
            Log("");

            // 7. Tick 驱动
            Log("【7】Tick 驱动");
            worldManager.Tick(0.016f);
            Log("已执行 1 帧 (16ms)");

            // 8. 销毁
            Log("【8】清理资源");
            worldManager.DisposeAll();
            Log("所有 World 已销毁");

            Output.Divider();
        }

        private IWorld CreateWorldInstance(WorldContainer container, WorldCreateOptions options)
        {
            return new SimpleWorldInstance(options.Id, options.WorldType, container);
        }
    }

    // 示例服务定义
    public interface IGameService
    {
        void Update();
    }

    public interface IConfigService
    {
        string GetValue(string key);
    }

    public sealed class GameService : IGameService
    {
        private int _frameCount;

        public void Update()
        {
            _frameCount++;
        }

        public int FrameCount => _frameCount;
    }

    public sealed class ConfigService : IConfigService
    {
        private readonly Dictionary<string, string> _config = new Dictionary<string, string>
        {
            { "game.title", "AbilityKit Demo" },
            { "game.version", "1.0.0" }
        };

        public string GetValue(string key)
        {
            return _config.TryGetValue(key, out var value) ? value : null;
        }
    }
}
