using System;
using AbilityKit.Ability.Host.Framework;
using AbilityKit.Ability.World.Abstractions;
using AbilityKit.Ability.World.Management;
using AbilityKit.Demo.Moba.Session;

namespace ET.Logic
{
    public sealed class ETBattleWorldCreateResult
    {
        public ETBattleWorldCreateResult(
            MobaSessionCoordinatorHost sessionHost,
            MobaBattleDriverHost driverHost,
            IWorld world,
            HostRuntime hostRuntime,
            IWorldManager worldManager)
        {
            SessionHost = sessionHost ?? throw new ArgumentNullException(nameof(sessionHost));
            DriverHost = driverHost ?? throw new ArgumentNullException(nameof(driverHost));
            World = world ?? throw new ArgumentNullException(nameof(world));
            HostRuntime = hostRuntime;
            WorldManager = worldManager;
        }

        public MobaSessionCoordinatorHost SessionHost { get; }
        public MobaBattleDriverHost DriverHost { get; }
        public IWorld World { get; }
        public HostRuntime HostRuntime { get; }
        public IWorldManager WorldManager { get; }
    }
}
