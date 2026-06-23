using System;
using AbilityKit.Ability.FrameSync.Rollback;
using AbilityKit.Ability.Host;
using AbilityKit.Ability.Host.Extensions.FrameSync;
using AbilityKit.Ability.Host.Extensions.Rollback;
using AbilityKit.Ability.Host.Extensions.Time;
using AbilityKit.Ability.Host.Extensions.WorldStart;
using AbilityKit.Ability.Host.Framework;
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
            var serverOptions = new HostRuntimeOptions();
            var server = new HostRuntime(worldManager, serverOptions);
            var modules = CreateModules(options, rollbackRegistryFactory, out var rollbackModule);

            modules.InstallAll(server, serverOptions);
            return new BattleLogicSessionRuntime(worldManager, server, rollbackModule);
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

        private static HostRuntimeModuleHost CreateModules(
            BattleLogicSessionOptions options,
            IBattleRollbackRegistryFactory rollbackRegistryFactory,
            out ServerRollbackModule rollbackModule)
        {
            rollbackModule = null;
            var modules = new HostRuntimeModuleHost()
                .Add(new FrameSyncDriverModule())
                .Add(new ServerFrameTimeModule())
                .Add(new WorldAutoStartModule());

            if (!options.EnableRollback) return modules;

            var history = options.RollbackHistoryFrames;
            if (history <= 0) history = 600;
            var captureEvery = options.RollbackCaptureEveryNFrames;
            if (captureEvery <= 0) captureEvery = 30;

            rollbackModule = new ServerRollbackModule(
                history,
                captureEvery,
                world => rollbackRegistryFactory.Create(world));
            modules.Add(rollbackModule);
            return modules;
        }
    }
}
