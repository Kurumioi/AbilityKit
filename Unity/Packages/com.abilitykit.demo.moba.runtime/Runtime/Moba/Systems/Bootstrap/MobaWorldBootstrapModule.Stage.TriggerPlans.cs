using System;
using AbilityKit.Ability.Config;
using AbilityKit.Ability.Triggering.Json;
using AbilityKit.Core.Common.Log;
using AbilityKit.Triggering.Runtime.Plan.Json;
using AbilityKit.Ability.World.DI;

namespace AbilityKit.Demo.Moba.Systems
{
    public sealed partial class MobaWorldBootstrapModule
    {
        private static void RegisterTriggerPlans(WorldContainerBuilder builder)
        {
            // 注意：ITextAssetLoader 已经通过 ResourcesTextAssetLoader 的 WorldServiceAttribute 自动注册

            // 注册 TriggerPlanJsonDatabase.ITextLoader
            builder.TryRegister<TriggerPlanJsonDatabase.ITextLoader>(WorldLifetime.Singleton, r =>
            {
                var textAssetLoader = r.Resolve<ITextAssetLoader>();
                var jsonLoader = new UnityResourcesTextLoader(textAssetLoader);
                return new PlanTextLoaderAdapter(jsonLoader);
            });

            builder.TryRegister<PlanActionModuleRegistry>(WorldLifetime.Singleton, _ => PlanActionModuleRegistry.CreateDefault());

            builder.TryRegister<TriggerPlanJsonDatabase>(WorldLifetime.Singleton, r =>
            {
                var loader = r.Resolve<TriggerPlanJsonDatabase.ITextLoader>();
                var db = new TriggerPlanJsonDatabase();
                Log.Info("[MobaWorldBootstrapModule] TriggerPlanJsonDatabase.Load begin");
                try
                {
                    db.Load(loader, "ability/ability_trigger_plans");
                    Log.Info($"[MobaWorldBootstrapModule] TriggerPlanJsonDatabase.Load end. records={db.Records?.Count ?? 0}");
                }
                catch (Exception ex)
                {
                    Log.Warning($"[MobaWorldBootstrapModule] TriggerPlanJsonDatabase.Load failed: {ex.Message}. Using empty database.");
                }
                return db;
            });
        }
    }
}
