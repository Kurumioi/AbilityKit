using System;
using AbilityKit.Ability.Config;
using AbilityKit.Demo.Moba.Config.BattleDemo;
using AbilityKit.Demo.Moba.Config.Core;
using AbilityKit.Ability.World.DI;
using AbilityKit.Core.Common.Log;

namespace AbilityKit.Demo.Moba.Systems
{
    public sealed partial class MobaWorldBootstrapModule
    {
        private static void RegisterConfig(WorldContainerBuilder builder)
        {
            Log.Info("[RegisterConfig] Entered");

            // 注册 IMobaConfigTableRegistry（MobaConfigRegistry 单例）
            builder.TryRegister<IMobaConfigTableRegistry>(WorldLifetime.Singleton, _ => AbilityKit.Demo.Moba.Config.BattleDemo.MobaConfigRegistry.Instance);
            Log.Info("[RegisterConfig] Registered MobaConfigRegistry");

            // 注意：ITextAssetLoader 在 RegisterTriggerPlans 中统一注册

            builder.TryRegister<IMobaConfigDtoDeserializer>(WorldLifetime.Singleton, _ => JsonNetMobaConfigDtoDeserializer.Instance);
            builder.TryRegister<IMobaConfigDtoBytesDeserializer>(WorldLifetime.Singleton, _ => new LubanMobaConfigDtoBytesDeserializer());

            builder.TryRegister<MobaConfigDatabase>(WorldLifetime.Singleton, _ =>
            {
                Log.Info("[MobaConfigDatabase Factory] invoked");

                // 解析 ITextAssetLoader（由视图层覆盖）
                var textAssetLoader = _.Resolve<ITextAssetLoader>();
                Log.Info($"[MobaConfigDatabase Factory] textAssetLoader={textAssetLoader?.GetType().Name ?? "null"}");

                _.TryResolve<IMobaConfigTableRegistry>(out var registry);
                _.TryResolve<IMobaConfigDtoDeserializer>(out var deserializer);
                _.TryResolve<IMobaConfigDtoBytesDeserializer>(out var bytesDeserializer);
                Log.Info($"[MobaConfigDatabase Factory] registry={(registry != null ? registry.GetType().Name : "null")}, deserializer={(deserializer != null ? "set" : "null")}, bytesDeserializer={(bytesDeserializer != null ? "set" : "null")}");
                var db = new MobaConfigDatabase(registry, deserializer, bytesDeserializer, textAssetLoader);
                Log.Info($"[MobaConfigDatabase Factory] after ctor: _tables.Count={CountTables(db)}, dbHash={db.GetHashCode()}");

                try
                {
                    Log.Info("[MobaConfigDatabase Factory] Loading from Resources");
                    db.LoadFromResources(MobaConfigPaths.DefaultResourcesDir, strict: false);
                    Log.Info($"[MobaConfigDatabase Factory] completed: CountTables={CountTables(db)}, dbHash={db.GetHashCode()}");
                    return db;
                }
                catch (Exception ex)
                {
                    Log.Exception(ex, "[MobaConfigDatabase Factory] Failed to load configs");
                    throw;
                }
            });

            Log.Info("[RegisterConfig] MobaConfigDatabase registered");
        }

        private static int CountTables(MobaConfigDatabase db)
        {
            var field = typeof(MobaConfigDatabase).GetField("_tables",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field?.GetValue(db) is System.Collections.Generic.Dictionary<Type, object> tables)
                return tables.Count;
            return -1;
        }
    }
}