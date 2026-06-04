using System;
using System.Collections.Generic;
using AbilityKit.Ability.Config;
using AbilityKit.Ability.Host;
using AbilityKit.Ability.Host.Framework;
using AbilityKit.Ability.World;
using AbilityKit.Ability.World.Abstractions;
using AbilityKit.Ability.World.DI;
using AbilityKit.Ability.World.Management;
using AbilityKit.Ability.World.Services;
using AbilityKit.Core.Math;
using AbilityKit.Demo.Moba.EntitasAdapters;
using AbilityKit.Demo.Moba.Services;
using AbilityKit.Demo.Moba.Systems;

namespace ET.Logic
{
    /// <summary>
    /// Moba 逻辑世界创建器
    /// 负责创建和配置 Moba Battle 世界
    ///
    /// 使用方式:
    /// 1. 添加 [LogicWorldRegistry.WorldCreator("battle")] 特性标记
    /// 2. 实现 CreateAndInitialize 方法
    /// 3. 由 LogicWorldRegistry 自动发现并注册
    /// </summary>
    [LogicWorldRegistry.WorldCreator(BattleWorldTypes.Battle)]
    public sealed class MobaLogicWorldCreator : ILogicWorldCreator
    {
        /// <summary>
        /// 世界类型标识符
        /// </summary>
        public string WorldType => BattleWorldTypes.Battle;

        /// <summary>
        /// 创建并初始化 Moba Battle 世界
        /// </summary>
        public void CreateAndInitialize(
            ETMobaBattleDriver driver,
            int worldId,
            int mapId,
            int playerId,
            int tickRate)
        {
            if (driver == null)
                throw new ArgumentNullException(nameof(driver));

            Log.Info($"[MobaLogicWorldCreator] Creating world: WorldId={worldId}, MapId={mapId}, PlayerId={playerId}, TickRate={tickRate}");

            // 步骤 1: 创建 WorldManager
            var worldManager = new WorldManager(BattleWorldFactory.Instance);
            driver.WorldManager = worldManager;

            // 步骤 2: 创建 HostRuntime
            driver.HostRuntime = new HostRuntime(worldManager);

            // 步骤 3: 配置模块列表
            // 注意：模块注册顺序很重要！
            // - BattleServiceModule 注册额外的 ET 专用服务
            // - MobaWorldBootstrapModule 注册所有 moba.core 核心服务
            var modules = new List<IWorldModule>
            {
                new BattleServiceModule(),
                new MobaWorldBootstrapModule()
            };

            // 步骤 4: 创建 WorldCreateOptions
            var options = new WorldCreateOptions
            {
                Id = new WorldId($"battle-{worldId}"),
                WorldType = BattleWorldTypes.Battle,
                ServiceBuilder = WorldServiceContainerFactory.CreateDefaultOnly()
            };

            options.ServiceBuilder.TryRegister<ICollisionService>(
                WorldLifetime.Singleton,
                _ => new CollisionService());

            // 步骤 5: 设置 Entitas 上下文工厂（必须！用于创建 EntitasWorld）
            options.SetEntitasContextsFactory(new MobaEntitasContextsFactory());

            // 步骤 6: 添加模块
            foreach (var module in modules)
            {
                options.Modules.Add(module);
            }

            // 步骤 7: 通过 HostRuntime 创建 World
            var world = driver.HostRuntime.CreateWorld(options);
            if (world == null)
            {
                throw new InvalidOperationException($"Failed to create world with options: Id={options.Id}, Type={options.WorldType}");
            }

            driver.World = world;

            Log.Info($"[MobaLogicWorldCreator] World created successfully: Id={world.Id}, Type={world.WorldType}");
            Log.Info($"[MobaLogicWorldCreator] World services available: {world.Services != null}");

            // 验证正式入口端口和运行时关键服务是否已注册
            ValidateServices(driver.World);
        }

        /// <summary>
        /// 验证关键服务是否已正确注册
        /// </summary>
        private void ValidateServices(IWorld world)
        {
            if (world?.Services == null)
            {
                Log.Warning("[MobaLogicWorldCreator] World.Services is null");
                return;
            }

            if (world.Services.TryResolve<IMobaBattleRuntimePort>(out var runtime) && runtime != null)
            {
                Log.Info("[MobaLogicWorldCreator] Runtime port validated: " + runtime.Status);
                return;
            }

            Log.Warning("[MobaLogicWorldCreator] IMobaBattleRuntimePort not found; runtime adapter integration will be unavailable");
        }

    }
}
