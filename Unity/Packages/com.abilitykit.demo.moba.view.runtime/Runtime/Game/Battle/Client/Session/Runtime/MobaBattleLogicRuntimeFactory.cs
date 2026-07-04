using System;
using AbilityKit.Ability.FrameSync.Rollback;
using AbilityKit.Ability.Host.Extensions.Moba.Runtime;
using AbilityKit.Ability.World;
using AbilityKit.Ability.World.Abstractions;
using AbilityKit.Ability.World.DI;
using AbilityKit.Ability.World.Management;
using AbilityKit.Ability.World.Services;
using AbilityKit.Demo.Moba.Worlds.Blueprints;

namespace AbilityKit.Game.Battle
{
    public sealed class MobaBattleLogicRuntimeFactory : IBattleLogicRuntimeFactory
    {
        public BattleLogicSessionRuntime CreateRuntime(
            BattleLogicSessionOptions options,
            IBattleRollbackRegistryFactory rollbackRegistryFactory)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));
            if (rollbackRegistryFactory == null) throw new ArgumentNullException(nameof(rollbackRegistryFactory));
            if (options.Mode == BattleLogicMode.Remote) return null;

            var worldManager = CreateWorldManager();
            var profile = new MobaHostRuntimeProfile(
                enableFrameSync: true,
                enableServerFrameTime: true,
                enableWorldAutoStart: true,
                enableRollback: options.EnableRollback,
                rollbackHistoryFrames: options.RollbackHistoryFrames,
                rollbackCaptureEveryNFrames: options.RollbackCaptureEveryNFrames);
            var runtime = MobaHostRuntimeBuilder.CreateRuntime(
                worldManager,
                in profile,
                world => rollbackRegistryFactory.Create(world));

            return new BattleLogicSessionRuntime(worldManager, runtime.Runtime, runtime.RollbackModule);
        }

        public WorldContainerBuilder CreateWorldServices(BattleLogicSessionOptions options)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));
            if (options.WorldServices != null) return options.WorldServices;

            var prefixes = options.NamespacePrefixes;
            if (options.ScanAllLoadedAssemblies)
            {
                return WorldServiceContainerFactory.CreateWithAttributes(
                    options.Profile,
                    true,
                    prefixes);
            }

            var scanAssemblies = options.ScanAssemblies;
            if (scanAssemblies == null || scanAssemblies.Length == 0)
            {
                scanAssemblies = new[]
                {
                    typeof(WorldServiceContainerFactory).Assembly,
                    typeof(BattleLogicSession).Assembly
                };
            }

            return WorldServiceContainerFactory.CreateWithAttributes(
                options.Profile,
                scanAssemblies,
                prefixes);
        }

        private static IWorldManager CreateWorldManager()
        {
            var typeRegistry = new WorldTypeRegistry()
                .RegisterEntitasWorld(MobaLobbyWorldBlueprint.Type)
                .RegisterEntitasWorld(MobaBattleWorldBlueprint.Type);

            var blueprints = new AbilityKit.Ability.Host.WorldBlueprints.WorldBlueprintRegistry();
            MobaWorldBlueprintsRegistration.RegisterAll(blueprints);

            var baseFactory = new RegistryWorldFactory(typeRegistry);
            var factory = new AbilityKit.Ability.Host.WorldBlueprints.WorldBlueprintWorldFactory(baseFactory, blueprints);
            return new WorldManager(factory);
        }

    }
}
