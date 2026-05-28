using System;
using AbilityKit.Ability.Config;
using AbilityKit.Ability.World.DI;
using AbilityKit.Core.Common.Log;
using AbilityKit.Demo.Moba.Config.BattleDemo;
using AbilityKit.Demo.Moba.Config.Core;

namespace AbilityKit.Demo.Moba.Systems.Bootstrap.Flow.Stages
{
    /// <summary>
    /// Config Stage
    /// 注册配置相关的服务
    /// </summary>
    [MobaBootstrapStage]
    public sealed class ConfigStage : MobaBootstrapStageBase
    {
        public override string Name => "Config";

        protected internal override void Configure(WorldContainerBuilder builder)
        {
            Log.Info("[ConfigStage] Entered");

            // 注册 IMobaConfigTableRegistry（MobaConfigRegistry 单例）
            builder.TryRegister<IMobaConfigTableRegistry>(WorldLifetime.Singleton, _ => MobaConfigRegistry.Instance);
            Log.Info("[ConfigStage] Registered MobaConfigRegistry");

            builder.TryRegister<IMobaConfigDtoDeserializer>(WorldLifetime.Singleton, _ => JsonNetMobaConfigDtoDeserializer.Instance);
            builder.TryRegister<IMobaConfigDtoBytesDeserializer>(WorldLifetime.Singleton, _ => new LubanMobaConfigDtoBytesDeserializer());
            builder.TryRegister<IMobaConfigLoadProfile>(WorldLifetime.Singleton, _ => ResourcesJsonMobaConfigLoadProfile.Default);

            builder.TryRegister<MobaConfigDatabase>(WorldLifetime.Singleton, _ =>
            {
                Log.Info("[ConfigStage] MobaConfigDatabase Factory invoked");

                // 解析 ITextAssetLoader（由视图层覆盖）
                var textAssetLoader = _.Resolve<ITextAssetLoader>();
                Log.Info($"[ConfigStage] textAssetLoader={textAssetLoader?.GetType().Name ?? "null"}");

                _.TryResolve<IMobaConfigTableRegistry>(out var registry);
                _.TryResolve<IMobaConfigDtoDeserializer>(out var deserializer);
                _.TryResolve<IMobaConfigDtoBytesDeserializer>(out var bytesDeserializer);
                _.TryResolve<IMobaConfigLoadProfile>(out var loadProfile);
                Log.Info($"[ConfigStage] registry={(registry != null ? registry.GetType().Name : "null")}, deserializer={(deserializer != null ? "set" : "null")}, bytesDeserializer={(bytesDeserializer != null ? "set" : "null")}, loadProfile={(loadProfile != null ? loadProfile.Name : "null")}");
                var db = new MobaConfigDatabase(registry, deserializer, bytesDeserializer, textAssetLoader);
                Log.Info($"[ConfigStage] after ctor: _tables.Count={CountTables(db)}, dbHash={db.GetHashCode()}");

                try
                {
                    loadProfile ??= ResourcesJsonMobaConfigLoadProfile.Default;
                    Log.Info($"[ConfigStage] Loading configs by profile: {loadProfile.Name}");
                    loadProfile.Load(db);
                    Log.Info($"[ConfigStage] Config load completed: CountTables={CountTables(db)}, dbHash={db.GetHashCode()}");
                    return db;
                }
                catch (Exception ex)
                {
                    Log.Exception(ex, "[ConfigStage] Failed to load configs");
                    throw;
                }
            });

            Log.Info("[ConfigStage] MobaConfigDatabase registered");
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
