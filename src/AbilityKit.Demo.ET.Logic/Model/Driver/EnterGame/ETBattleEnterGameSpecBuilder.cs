using System;
using System.Collections.Generic;
using AbilityKit.Ability.Host;
using AbilityKit.Ability.Share.Impl.Moba.Struct;
using AbilityKit.Core.Math;
using AbilityKit.Demo.Moba;
using ET.AbilityKit.Demo.ET.Share;

namespace ET.Logic
{
    /// <summary>
    /// Builds the MOBA Runtime enter-game spec from ET room/player spawn data.
    /// </summary>
    public static class ETBattleEnterGameSpecBuilder
    {
        public static MobaGameStartSpec Build(IReadOnlyList<ETPlayerSpawnData> playerSpawnData)
        {
            var loadouts = new MobaPlayerLoadout[playerSpawnData.Count];
            for (int i = 0; i < playerSpawnData.Count; i++)
            {
                var spawnData = playerSpawnData[i];
                var playerId = new PlayerId(spawnData.PlayerId);

                loadouts[i] = new MobaPlayerLoadout(
                    playerId: playerId,
                    teamId: spawnData.TeamId,
                    heroId: spawnData.CharacterId,
                    attributeTemplateId: 0,
                    level: 1,
                    basicAttackSkillId: 0,
                    skillIds: null,
                    spawnIndex: i,
                    unitSubType: (int)UnitSubType.Hero,
                    mainType: (int)EntityMainType.Unit,
                    hasSpawnPosition: 1,
                    spawnX: spawnData.PositionX,
                    spawnY: 0f,
                    spawnZ: spawnData.PositionZ);
            }

            var localPlayerId = playerSpawnData.Count > 0 ? new PlayerId(playerSpawnData[0].PlayerId) : default;
            var enterReq = new EnterMobaGameReq(
                playerId: localPlayerId,
                matchId: $"et_demo_{Environment.TickCount}",
                mapId: 1,
                randomSeed: Environment.TickCount,
                tickRate: 30,
                inputDelayFrames: 0,
                players: loadouts);

            return new MobaGameStartSpec(in enterReq);
        }
    }
}
