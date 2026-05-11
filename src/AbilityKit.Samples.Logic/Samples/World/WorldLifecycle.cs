using System;
using AbilityKit.Ability.World.Abstractions;
using AbilityKit.Ability.World.DI;
using AbilityKit.Ability.World.Management;
using AbilityKit.Ability.World.Services;
using AbilityKit.Samples.Abstractions;

namespace AbilityKit.Samples.Logic.Samples.World
{
    /// <summary>
    /// WorldLifecycle - 演示 World 的完整生命周期管理
    /// 包括创建、初始化、Tick、销毁等各个阶段
    /// </summary>
    [Sample]
    public sealed class WorldLifecycle : SampleBase
    {
        public override string Title => "World Lifecycle";
        public override string Description => "World 创建、初始化、Tick、销毁的完整生命周期";
        public override SampleCategory Category => SampleCategory.World;

        protected override void OnRun()
        {
            Log("=== World Lifecycle 示例 ===");
            Output.Divider();

            // 1. 初始化 WorldTypeRegistry
            Log("【1】创建 WorldTypeRegistry");
            var registry = new WorldTypeRegistry();

            // 注册 WorldType 工厂
            registry.Register("GameWorld", options => CreateWorldInstance(options));
            registry.Register("BattleWorld", options => CreateWorldInstance(options));

            Log($"WorldTypeRegistry 已创建，已注册 {registry.GetType().GetMethod("Register").DeclaringType?.Name}");
            Output.Line();

            // 2. 创建 World 实例
            Log("【2】创建 World 实例");
            var gameWorld = registry.Create(new WorldCreateOptions
            {
                Id = new WorldId("game-world"),
                WorldType = "GameWorld"
            });
            Log($"World ID: {gameWorld.Id.Value}");
            Log($"World Type: {gameWorld.WorldType}");
            Log($"World Services: {gameWorld.Services != null}");

            Output.Line();

            // 3. 从 World 解析服务
            Log("【3】从 World 解析服务");
            var clock = gameWorld.Services.Resolve<IWorldClock>();
            var logger = gameWorld.Services.Resolve<IWorldLogger>();
            Log($"已解析 IWorldClock: {clock != null}");
            Log($"已解析 IWorldLogger: {logger != null}");

            Output.Line();

            // 4. Tick 驱动
            Log("【4】Tick 驱动 World");
            Log("执行 3 帧:");
            for (int i = 0; i < 3; i++)
            {
                gameWorld.Tick(0.016f);
                Log($"  帧 {i + 1}: delta=0.016s, WorldTime={clock.Time:F3}s");
            }

            Output.Line();

            // 5. 创建第二个 World
            Log("【5】创建 BattleWorld");
            var battleWorld = registry.Create(new WorldCreateOptions
            {
                Id = new WorldId("battle-world"),
                WorldType = "BattleWorld"
            });
            Log($"World ID: {battleWorld.Id.Value}");

            Output.Line();

            // 6. 清理
            Log("【6】清理资源");
            gameWorld.Dispose();
            battleWorld.Dispose();
            Log("所有 World 已销毁");

            Output.Divider();
        }

        private IWorld CreateWorldInstance(WorldCreateOptions options)
        {
            var builder = new WorldContainerBuilder();

            // 基础服务 - 使用 Singleton 生命周期
            builder.RegisterServiceType<IWorldClock, WorldClock>(WorldLifetime.Singleton);
            builder.RegisterServiceType<IWorldLogger, NullWorldLogger>(WorldLifetime.Singleton);
            builder.RegisterServiceType<DefaultWorldRandom, DefaultWorldRandom>(WorldLifetime.Singleton);

            var container = builder.Build();
            return new WorldInstance(options.Id, options.WorldType, container);
        }
    }

    /// <summary>
    /// 简单的 World 实例实现
    /// </summary>
    public sealed class WorldInstance : IWorld
    {
        private readonly WorldId _id;
        private readonly string _worldType;
        private readonly IWorldResolver _services;
        private bool _disposed;

        public WorldInstance(WorldId id, string worldType, IWorldResolver services)
        {
            _id = id;
            _worldType = worldType;
            _services = services;
        }

        public WorldId Id => _id;
        public string WorldType => _worldType;
        public IWorldResolver Services => _services;

        public void Initialize()
        {
        }

        public void Tick(float deltaTime)
        {
            if (_services.TryResolve<IWorldClock>(out var clock))
            {
                clock.Tick(deltaTime);
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            if (_services is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }
}
