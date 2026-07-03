#nullable enable

using System;
using AbilityKit.Ability.Host;
using AbilityKit.Ability.World.Abstractions;
using AbilityKit.Coordinator;
using AbilityKit.Coordinator.Core;
using AbilityKit.Demo.Shooter.Runtime;

namespace AbilityKit.Demo.Shooter.View.Hosting
{
    /// <summary>
    /// Hosts a Shooter client world inside the generic SessionCoordinator without creating a second logic world.
    /// </summary>
    public sealed class ShooterCoordinatorSessionHost : ISessionCoordinatorHost, ISessionCoordinatorConfigPolicy
    {
        private readonly ExistingWorldSessionCoordinatorHost _host;

        public ShooterCoordinatorSessionHost(IWorld world, ShooterGatewayCoordinatorInputTransport transport)
        {
            if (world == null) throw new ArgumentNullException(nameof(world));
            if (transport == null) throw new ArgumentNullException(nameof(transport));

            _host = new ExistingWorldSessionCoordinatorHost(
                world,
                serviceOverrides: new object[] { transport },
                configureSession: ConfigureShooterSession);
        }

        public void ConfigureSession(ref SessionConfig config)
        {
            ConfigureShooterSession(ref config);
        }

        public IWorldHost CreateWorldHost(SessionConfig config)
        {
            return _host.CreateWorldHost(config);
        }

        public void ConfigureWorldCreateOptions(in SessionConfig config, WorldCreateOptions options)
        {
            _host.ConfigureWorldCreateOptions(in config, options);
        }

        public void RegisterServices(IWorld world, SessionConfig config)
        {
            _host.RegisterServices(world, config);
        }

        public void LoadConfig(IWorld world, SessionConfig config)
        {
            _host.LoadConfig(world, config);
        }

        public PlayerSpawnData[] CreatePlayerSpawnData(SessionConfig config)
        {
            return _host.CreatePlayerSpawnData(config);
        }

        private static void ConfigureShooterSession(ref SessionConfig config)
        {
            config.SyncMode = SyncMode.StateSync;
            config.HostMode = HostMode.Client;
            config.WorldType = ShooterGameplay.WorldType;
            config.UseCoordinatorSpawnService = false;
            config.RequireLogicWorldDriveGate = true;
            config.EnableClientPrediction = false;
            config.MaxPredictionAheadFrames = 0;
        }
    }
}
