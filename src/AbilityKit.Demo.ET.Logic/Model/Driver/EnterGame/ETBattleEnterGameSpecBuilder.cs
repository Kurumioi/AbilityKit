using System;
using System.Collections.Generic;
using AbilityKit.Ability.Host;
using AbilityKit.Ability.Host.Extensions.Moba.CreateWorld;
using AbilityKit.Core.Math;
using AbilityKit.Demo.Moba;
using AbilityKit.Protocol.Moba;
using AbilityKit.Demo.Moba.Share;
using ET.AbilityKit.Demo.ET.Share;

namespace ET.Logic
{
    /// <summary>
    /// Builds the MOBA Runtime enter-game spec from ET room/player spawn data.
    /// </summary>
    public static class ETBattleEnterGameSpecBuilder
    {
        public static MobaGameStartSpec Build(BattleStartPlan plan, IReadOnlyList<ETPlayerSpawnData> playerSpawnData)
        {
            return BuildLaunchSpec(plan, playerSpawnData).ToGameStartSpec();
        }

        public static MobaBattleLaunchSpec BuildLaunchSpec(BattleStartPlan plan, IReadOnlyList<ETPlayerSpawnData> playerSpawnData)
        {
            var loadouts = BuildLoadouts(plan, playerSpawnData);
            var worldId = plan.WorldId > 0 ? plan.WorldId.ToString() : $"et_world_{Environment.TickCount}";
            var matchId = worldId;
            var localPlayerId = plan.PlayerId > 0
                ? new PlayerId(plan.PlayerId.ToString())
                : loadouts.Length > 0 ? loadouts[0].PlayerId : default;

            var profile = MobaBattleLaunchProfile.Create(
                clientId: plan.ClientId > 0 ? plan.ClientId.ToString() : "et_logic",
                launchMode: MobaBattleLaunchMode.EtServer,
                syncMode: ToLaunchSyncMode(plan.SyncMode),
                authorityMode: ToLaunchAuthorityMode(plan.HostMode),
                tickRate: plan.TickRate > 0 ? plan.TickRate : 30,
                inputDelayFrames: 0);

            return MobaBattleLaunchSpecBuilder.FromLoadouts(
                battleId: matchId,
                localPlayerId: localPlayerId,
                mapId: plan.MapId > 0 ? plan.MapId : 1,
                players: loadouts,
                profile: in profile,
                matchId: matchId,
                worldId: worldId);
        }

        private static MobaPlayerLoadout[] BuildLoadouts(BattleStartPlan plan, IReadOnlyList<ETPlayerSpawnData> playerSpawnData)
        {
            if (playerSpawnData != null && playerSpawnData.Count > 0)
            {
                var loadouts = new MobaPlayerLoadout[playerSpawnData.Count];
                for (int i = 0; i < playerSpawnData.Count; i++)
                {
                    var spawnData = playerSpawnData[i];
                    loadouts[i] = new MobaPlayerLoadout(
                        playerId: new PlayerId(spawnData.PlayerId),
                        teamId: spawnData.TeamId,
                        heroId: spawnData.CharacterId,
                        attributeTemplateId: 0,
                        level: 1,
                        basicAttackSkillId: 0,
                        skillIds: Array.Empty<int>(),
                        spawnIndex: i,
                        unitSubType: (int)UnitSubType.Hero,
                        mainType: (int)EntityMainType.Unit,
                        hasSpawnPosition: 1,
                        spawnX: spawnData.PositionX,
                        spawnY: 0f,
                        spawnZ: spawnData.PositionZ);
                }

                return loadouts;
            }

            var playerIds = plan.PlayerIds;
            if (playerIds == null || playerIds.Count == 0)
            {
                return Array.Empty<MobaPlayerLoadout>();
            }

            var fallbackLoadouts = new MobaPlayerLoadout[playerIds.Count];
            for (int i = 0; i < playerIds.Count; i++)
            {
                fallbackLoadouts[i] = new MobaPlayerLoadout(
                    playerId: new PlayerId(playerIds[i].ToString()),
                    teamId: 0,
                    heroId: 0,
                    attributeTemplateId: 0,
                    level: 1,
                    basicAttackSkillId: 0,
                    skillIds: Array.Empty<int>(),
                    spawnIndex: i,
                    unitSubType: (int)UnitSubType.Hero,
                    mainType: (int)EntityMainType.Unit,
                    hasSpawnPosition: 0,
                    spawnX: 0f,
                    spawnY: 0f,
                    spawnZ: 0f);
            }

            return fallbackLoadouts;
        }

        private static MobaBattleLaunchSyncMode ToLaunchSyncMode(SyncMode syncMode)
        {
            return syncMode switch
            {
                SyncMode.Lockstep => MobaBattleLaunchSyncMode.FrameSync,
                SyncMode.SnapshotAuthority => MobaBattleLaunchSyncMode.StateSync,
                SyncMode.StateSync => MobaBattleLaunchSyncMode.StateSync,
                SyncMode.Hybrid => MobaBattleLaunchSyncMode.Hybrid,
                _ => MobaBattleLaunchSyncMode.Unspecified,
            };
        }

        private static MobaBattleLaunchAuthorityMode ToLaunchAuthorityMode(HostMode hostMode)
        {
            return hostMode == HostMode.GatewayRemote
                ? MobaBattleLaunchAuthorityMode.ServerAuthority
                : MobaBattleLaunchAuthorityMode.LocalAuthority;
        }
    }
}
