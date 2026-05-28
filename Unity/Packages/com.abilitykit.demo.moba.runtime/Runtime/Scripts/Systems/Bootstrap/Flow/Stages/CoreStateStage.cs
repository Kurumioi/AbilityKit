using System;
using AbilityKit.Ability.World.DI;
using AbilityKit.Ability.World.Services;
using AbilityKit.Ability.World.Services.Attributes;
using AbilityKit.Core.Common.Log;
using AbilityKit.Demo.Moba.Rollback;

namespace AbilityKit.Demo.Moba.Systems.Bootstrap.Flow.Stages
{
    /// <summary>
    /// CoreState Stage
    /// 注册核心状态服务和基础服务
    /// </summary>
    [MobaBootstrapStage]
    public sealed class CoreStateStage : MobaBootstrapStageBase
    {
        public override string Name => "CoreState";

        protected internal override void Configure(WorldContainerBuilder builder)
        {
            // 重要：必须先添加 DefaultWorldServicesModule 注册基础服务
            // 包括 IEventBus, IWorldClock, IFrameTime 等
            // 这些服务必须在其他 Stage 之前注册，因为它们是其他服务的依赖
            Log.Info("[CoreStateStage] Configure: adding DefaultWorldServicesModule");
            builder.AddModule(new DefaultWorldServicesModule());

            // 清除 AttributeWorldServicesModule 的缓存
            // 这是必需的，因为 MobaServicesAutoModule 使用静态缓存
            // 如果在缓存填充时 IEventBus 还未注册，缓存会记录"缺失"
            // 后续注册 IEventBus 不会更新缓存，所以需要清除缓存
            Log.Info("[CoreStateStage] Configure: clearing AttributeWorldServicesModule cache");
            ClearAttributeWorldServicesModuleCache();

            // Deterministic + rollbackable RNG (override default world random)
            builder.Register<IWorldRandom>(WorldLifetime.Scoped, _ => new RollbackWorldRandom());
        }

        private static void ClearAttributeWorldServicesModuleCache()
        {
            try
            {
                // Cache 类型是 Dictionary<CacheKey, Registration[]>
                // CacheKey 是 AttributeWorldServicesModule 的嵌套结构
                var cacheType = Type.GetType("AbilityKit.Ability.World.Services.Attributes.AttributeWorldServicesModule+CacheKey, AbilityKit.World.DI");
                var dictType = typeof(System.Collections.Generic.Dictionary<,>).MakeGenericType(cacheType, typeof(object).MakeArrayType());

                var cacheField = typeof(AttributeWorldServicesModule)
                    .GetField("Cache", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

                if (cacheField != null)
                {
                    var cache = cacheField.GetValue(null);
                    if (cache != null)
                    {
                        var clearMethod = cache.GetType().GetMethod("Clear");
                        clearMethod?.Invoke(cache, null);
                        Log.Info("[CoreStateStage] AttributeWorldServicesModule cache cleared successfully");
                        return;
                    }
                }

                Log.Warning("[CoreStateStage] Could not clear AttributeWorldServicesModule cache (field or cache is null)");
            }
            catch (System.Exception ex)
            {
                Log.Warning($"[CoreStateStage] Failed to clear AttributeWorldServicesModule cache: {ex.Message}");
            }
        }
    }
}
