using System;
using AbilityKit.Ability.Host.WorldBlueprints;
using AbilityKit.Ability.World.Abstractions;
using AbilityKit.Ability.World.Management;

namespace AbilityKit.Demo.Moba.Worlds.Blueprints
{
    public static class MobaWorldBlueprintsRegistration
    {
        /// <summary>
        /// 注册到 WorldBlueprintRegistry
        /// </summary>
        public static void RegisterAll(WorldBlueprintRegistry registry)
        {
            if (registry == null) throw new ArgumentNullException(nameof(registry));

            registry
                .Register(new MobaLobbyWorldBlueprint())
                .Register(new MobaBattleWorldBlueprint());
        }

        /// <summary>
        /// 注册到 WorldTypeRegistry (使用 Blueprint 适配器)
        /// </summary>
        public static void RegisterAll(WorldTypeRegistry registry, Func<WorldCreateOptions, IWorld> baseFactory)
        {
            if (registry == null) throw new ArgumentNullException(nameof(registry));
            if (baseFactory == null) throw new ArgumentNullException(nameof(baseFactory));

            var blueprintRegistry = new WorldBlueprintRegistry();
            RegisterAll(blueprintRegistry);

            var adapter = new BlueprintToWorldFactoryAdapter(baseFactory, blueprintRegistry);

            // 直接注册工厂
            registry.Register(MobaBattleWorldBlueprint.Type, options => adapter.Create(options));
            registry.Register(MobaLobbyWorldBlueprint.Type, options => adapter.Create(options));
        }
    }
}
