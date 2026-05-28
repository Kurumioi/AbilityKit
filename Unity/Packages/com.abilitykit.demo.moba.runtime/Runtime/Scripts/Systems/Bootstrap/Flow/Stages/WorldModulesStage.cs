using System;
using AbilityKit.Ability.Share.ECS.Entitas;
using AbilityKit.Ability.Triggering.Runtime;
using AbilityKit.Ability.World;
using AbilityKit.Ability.World.DI;
using AbilityKit.Ability.World.Services.Attributes;
using AbilityKit.Core.Common.Log;
using AbilityKit.Core.Common.Projectile;
using AbilityKit.Triggering.Registry;

namespace AbilityKit.Demo.Moba.Systems.Bootstrap.Flow.Stages
{
    /// <summary>
    /// WorldModules Stage
    /// 注册基础模块和触发器模块
    /// 
    /// 注意：EntitasEcsWorldModule 使用延迟解析，在 Configure 阶段注册，
    /// 但工厂方法中的 IContexts 解析会在第一次解析时才执行（此时 IContexts 已可用）。
    /// 
    /// 重要：必须在 Configure 阶段注册 AbilityKit.Triggering.Eventing.IEventBus，
    /// 因为 MobaEntityManager 等服务在 Install 阶段首次解析时需要此依赖。
    /// 
    /// 重要：由于 AttributeWorldServicesModule 使用静态缓存，
    /// 必须在 MobaServicesAutoModule 之前手动注册 FunctionRegistry 和 ActionRegistry，
    /// 以确保 MobaEffectExecutionService 等服务能够被正确解析。
    /// </summary>
    [MobaBootstrapStage]
    public sealed class WorldModulesStage : MobaBootstrapStageBase
    {
        public override string Name => "WorldModules";

        protected internal override void Configure(WorldContainerBuilder builder)
        {
            Log.Info("[WorldModulesStage] Configure: adding modules");

            // 重要：由于 AttributeWorldServicesModule 使用静态缓存，
            // 第一次调用时缓存会包含不完整的注册列表。
            // 这里先清理缓存，确保后续使用最新的注册列表。
            // 注意：这必须在 MobaServicesAutoModule.Configure 之前执行。
            try
            {
                var cacheField = typeof(AttributeWorldServicesModule)
                    .GetField("Cache", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
                if (cacheField != null)
                {
                    var cache = cacheField.GetValue(null) as System.Collections.Generic.Dictionary<object, object>;
                    if (cache != null && cache.Count > 0)
                    {
                        Log.Warning($"[WorldModulesStage] Clearing AttributeWorldServicesModule static cache ({cache.Count} entries)");
                        cache.Clear();
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warning($"[WorldModulesStage] Failed to clear AttributeWorldServicesModule cache: {ex.Message}");
            }

            // 注意：DefaultWorldServicesModule 现在在 CoreStateStage 中注册
            // 这里只需要添加其他模块

            // 重要：注册 AbilityKit.Triggering.Eventing.IEventBus
            // MobaEntityManager、SkillExecutor 等服务在 Install 阶段首次解析时需要此依赖
            // 必须在 MobaServicesAutoModule 配置服务类型之前注册
            Log.Info("[WorldModulesStage] Configure: registering AbilityKit.Triggering.Eventing.IEventBus");
            builder.TryRegisterType<AbilityKit.Triggering.Eventing.IEventBus, AbilityKit.Triggering.Eventing.EventBus>(WorldLifetime.Singleton);

            // 重要：注册 FunctionRegistry 和 ActionRegistry
            // MobaEffectExecutionService 需要这些依赖
            // 必须在 MobaServicesAutoModule 之前注册
            Log.Info("[WorldModulesStage] Configure: registering FunctionRegistry and ActionRegistry");
            builder.Register<FunctionRegistry>(WorldLifetime.Singleton, _ => new FunctionRegistry());
            builder.Register<ActionRegistry>(WorldLifetime.Singleton, _ => new ActionRegistry());

            // 注册 SkillConditionRegistry（MobaEffectExecutionService 需要）
            Log.Info("[WorldModulesStage] Configure: registering SkillConditionRegistry");
            builder.Register<AbilityKit.Demo.Moba.Services.SkillConditionRegistry>(WorldLifetime.Singleton, _ => new AbilityKit.Demo.Moba.Services.SkillConditionRegistry());

            // 注册 TriggerRunner<IWorldResolver>（MobaEffectExecutionService 需要）
            // TriggerRunner 依赖 IEventBus, FunctionRegistry, ActionRegistry（已注册）
            Log.Info("[WorldModulesStage] Configure: registering TriggerRunner<IWorldResolver>");
            builder.Register<AbilityKit.Triggering.Runtime.TriggerRunner<IWorldResolver>>(WorldLifetime.Singleton, r =>
                new AbilityKit.Triggering.Runtime.TriggerRunner<IWorldResolver>(
                    r.Resolve<AbilityKit.Triggering.Eventing.IEventBus>(),
                    r.Resolve<FunctionRegistry>(),
                    r.Resolve<ActionRegistry>()));

            // EntitasEcsWorldModule 使用延迟解析 - 在 Configure 阶段注册工厂方法，
            // IContexts 的解析会在第一次解析时才执行（此时 IContexts 已可用）
            Log.Info("[WorldModulesStage] Configure: adding EntitasEcsWorldModule");
            builder.AddModule(new EntitasEcsWorldModule());

            Log.Info("[WorldModulesStage] Configure: adding ProjectileWorldModule");
            builder.AddModule(new ProjectileWorldModule());

            // Auto-discover and register all services with [WorldService] attribute
            Log.Info("[WorldModulesStage] Configure: adding MobaServicesAutoModule");
            builder.AddModule(new MobaServicesAutoModule());
            Log.Info("[WorldModulesStage] Configure: done");
        }

        protected internal override void Install(
            Entitas.IContexts contexts,
            Entitas.Systems systems,
            IWorldResolver services)
        {
            Log.Info("[WorldModulesStage] Install: nothing extra needed");
        }
    }
}
