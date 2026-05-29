using System;
using System.Collections.Generic;
using AbilityKit.Ability.Host;
using AbilityKit.Core.Generic;
using AbilityKit.Ability.Share.Impl.Moba.Struct;
using AbilityKit.Ability.Share.Impl.Moba.CreateWorld;

namespace AbilityKit.Demo.Moba.Services
{
    /// <summary>
    /// Logic-world spawn data used by runtime services.
    /// Host adapters can map coordinator, server, console, or ET spawn requests into this model.
    /// </summary>
    public struct LogicWorldSpawnData
    {
        public int PlayerId;
        public int CharacterId;
        public int TeamId;
        public float X;
        public float Y;
        public float Z;
        public string Name;

        public LogicWorldSpawnData(int playerId, int characterId, int teamId, float x, float y, float z, string name = null)
        {
            PlayerId = playerId;
            CharacterId = characterId;
            TeamId = teamId;
            X = x;
            Y = y;
            Z = z;
            Name = name;
        }

        public static LogicWorldSpawnData CreateLocalPlayer(int playerId, int characterId, float x, float z)
        {
            return new LogicWorldSpawnData(playerId, characterId, 1, x, 0f, z, "LocalPlayer");
        }
    }

    /// <summary>
    /// 数据结构转换器
    ///
    /// 负责将外部生成计划转换为 moba.core 进场数据结构
    ///
    /// Design:
    /// - 提供静态转换方法，可独立使用
    /// - 封装逻辑世界生成数据到 moba.core 层的转换逻辑
    /// - 外部协议或协调层数据应先由 adapter 转换为 LogicWorldSpawnData
    /// </summary>
    public static class SpawnDataConverter
    {
        /// <summary>
        /// 将 LogicWorldSpawnData[] 转换为 MobaPlayerLoadout[]
        /// </summary>
        public static MobaPlayerLoadout[] ConvertToLoadouts(LogicWorldSpawnData[] spawns, int startIndex = 0)
        {
            if (spawns == null || spawns.Length == 0)
            {
                return Array.Empty<MobaPlayerLoadout>();
            }

            var loadouts = new List<MobaPlayerLoadout>(spawns.Length);
            for (int i = 0; i < spawns.Length; i++)
            {
                var loadout = ConvertToLoadout(spawns[i], startIndex + i);
                loadouts.Add(loadout);
            }
            return loadouts.ToArray();
        }

        /// <summary>
        /// 将单个 LogicWorldSpawnData 转换为 MobaPlayerLoadout
        /// </summary>
        public static MobaPlayerLoadout ConvertToLoadout(LogicWorldSpawnData spawn, int spawnIndex)
        {
            var playerId = new PlayerId(spawn.PlayerId.ToString());

            return new MobaPlayerLoadout(
                playerId: playerId,
                teamId: spawn.TeamId,
                heroId: spawn.CharacterId,
                attributeTemplateId: 1, // Default attribute template
                level: 1,
                basicAttackSkillId: 1001, // Default basic attack
                skillIds: new int[] { 1001, 1002, 1003, 1004 }, // Default skills
                spawnIndex: spawnIndex,
                unitSubType: (int)UnitSubType.Hero,
                mainType: (int)EntityMainType.Unit,
                hasSpawnPosition: 1,
                spawnX: spawn.X,
                spawnY: spawn.Y,
                spawnZ: spawn.Z
            );
        }

        /// <summary>
        /// 将 LogicWorldSpawnData[] 转换为 EnterMobaGameReq
        /// </summary>
        public static EnterMobaGameReq ConvertToEnterGameReq(
            LogicWorldSpawnData[] spawns,
            PlayerId playerId,
            string matchId,
            int mapId,
            int tickRate = 30,
            int inputDelayFrames = 0,
            int randomSeed = 0)
        {
            if (spawns == null || spawns.Length == 0)
            {
                throw new ArgumentException("Spawns cannot be null or empty", nameof(spawns));
            }

            var loadouts = ConvertToLoadouts(spawns);

            // Use provided random seed or generate a new one
            var seed = randomSeed != 0 ? randomSeed : Environment.TickCount;

            return new EnterMobaGameReq(
                playerId: playerId,
                matchId: matchId,
                mapId: mapId,
                randomSeed: seed,
                tickRate: tickRate,
                inputDelayFrames: inputDelayFrames,
                opCode: 0,
                payload: null,
                players: loadouts
            );
        }

        /// <summary>
        /// 将 LogicWorldSpawnData[] 转换为 MobaGameStartSpec
        /// </summary>
        public static MobaGameStartSpec ConvertToGameStartSpec(
            LogicWorldSpawnData[] spawns,
            PlayerId playerId,
            string matchId,
            int mapId,
            int tickRate = 30,
            int inputDelayFrames = 0,
            int randomSeed = 0)
        {
            var req = ConvertToEnterGameReq(spawns, playerId, matchId, mapId, tickRate, inputDelayFrames, randomSeed);
            return new MobaGameStartSpec(in req);
        }
    }
}
