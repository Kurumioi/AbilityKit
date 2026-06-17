using System;
using AbilityKit.Ability.Host;
using AbilityKit.Ability.Host.Extensions.Moba.CreateWorld;
using AbilityKit.Ability.World.Abstractions;
using AbilityKit.Coordinator;
using AbilityKit.Demo.Moba.Session;
using AbilityKit.Demo.Moba.Share;
using AbilityKit.Protocol.Moba;

namespace ET.Logic
{
    public sealed class ETBattleWorldFactory : IETBattleWorldFactory
    {
        public ETBattleWorldCreateResult Create(in ETBattleWorldCreateContext context)
        {
            var plan = context.Plan;
            var players = context.PlayerSpawnData;
            if (players == null || players.Count == 0)
            {
                throw new InvalidOperationException("ET battle world initialization requires player spawn data before creating the MOBA runtime world.");
            }

            var launchSpec = ETBattleEnterGameSpecBuilder.BuildLaunchSpec(plan, players);
            var defaults = CreateSessionDefaults(in plan, in launchSpec);
            var sessionConfig = CreateSessionConfig(in plan, in launchSpec);

            var sessionHost = new MobaSessionCoordinatorHost(context.TextAssetLoader, defaults);
            sessionHost.ConfigureSession(ref sessionConfig);
            sessionHost.SetPendingPlayerLoadouts(launchSpec.Players);

            var worldHost = sessionHost.CreateWorldHost(sessionConfig);
            var options = new WorldCreateOptions();
            sessionHost.ConfigureWorldCreateOptions(in sessionConfig, options);

            var world = worldHost.CreateWorld(options);
            if (world == null)
            {
                throw new InvalidOperationException($"Failed to create moba logic world: WorldId={plan.WorldId}, WorldType={sessionConfig.WorldType}");
            }

            world.Initialize();
            sessionHost.RegisterServices(world, sessionConfig);
            sessionHost.LoadConfig(world, sessionConfig);

            var hostRuntime = sessionHost.HostRuntime;
            var driverHost = new MobaBattleDriverHost();
            driverHost.BindLogicWorld(world, hostRuntime);

            return new ETBattleWorldCreateResult(sessionHost, driverHost, world, hostRuntime, hostRuntime?.Worlds);
        }

        private static MobaSessionDefaults CreateSessionDefaults(in BattleStartPlan plan, in MobaBattleLaunchSpec launchSpec)
        {
            return new MobaSessionDefaults
            {
                WorldId = launchSpec.WorldId,
                WorldType = launchSpec.WorldType,
                MatchId = launchSpec.MatchId,
                MapId = plan.MapId,
                TickRate = plan.TickRate > 0 ? plan.TickRate : 30,
                InputDelayFrames = plan.InputDelayFrames,
                RandomSeed = launchSpec.RandomSeed
            };
        }

        private static SessionConfig CreateSessionConfig(in BattleStartPlan plan, in MobaBattleLaunchSpec launchSpec)
        {
            var localPlayerId = plan.PlayerId > 0 ? plan.PlayerId : ParsePlayerId(launchSpec.LocalPlayerId);
            var clientId = plan.ClientId > 0 ? plan.ClientId : localPlayerId;
            return new SessionConfig
            {
                SessionId = new SessionId(plan.WorldId > 0 ? plan.WorldId : DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()),
                MapId = plan.MapId,
                WorldId = plan.WorldId,
                WorldType = launchSpec.WorldType,
                LocalPlayerId = localPlayerId,
                ClientId = clientId,
                SyncMode = global::AbilityKit.Coordinator.Core.SyncMode.Lockstep,
                HostMode = global::AbilityKit.Coordinator.Core.HostMode.Local,
                TickRate = plan.TickRate > 0 ? plan.TickRate : 30,
                ServerEndpoint = NetworkEndpoint.None,
                RoomId = plan.WorldId
            };
        }

        private static int ParsePlayerId(PlayerId playerId)
        {
            return int.TryParse(playerId.Value, out var value) ? value : 1;
        }
    }
}
