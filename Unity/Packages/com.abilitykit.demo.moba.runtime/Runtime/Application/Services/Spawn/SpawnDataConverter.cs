using System;
using AbilityKit.Ability.Host;
using AbilityKit.Ability.Host.Extensions.Moba.CreateWorld;
using AbilityKit.Ability.Host.Extensions.Moba.Struct;
using AbilityKit.Protocol.Moba;

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
    /// Legacy compatibility adapter for runtime spawn fallback.
    /// Formal startup plan construction lives in host.extension.
    /// </summary>
    public static class SpawnDataConverter
    {
        public static MobaPlayerLoadout[] ConvertToLoadouts(LogicWorldSpawnData[] spawns, int startIndex = 0)
        {
            return MobaHostSpawnPlanBuilder.ToLoadouts(ToHostSpawns(spawns), startIndex);
        }

        public static MobaPlayerLoadout ConvertToLoadout(LogicWorldSpawnData spawn, int spawnIndex)
        {
            return MobaHostSpawnPlanBuilder.ToLoadout(ToHostSpawn(spawn), spawnIndex);
        }

        public static EnterMobaGameReq ConvertToEnterGameReq(
            LogicWorldSpawnData[] spawns,
            PlayerId playerId,
            string matchId,
            int mapId,
            int tickRate = 30,
            int inputDelayFrames = 0,
            int randomSeed = 0)
        {
            return MobaHostSpawnPlanBuilder.ToEnterReq(
                ToHostSpawns(spawns),
                playerId,
                matchId,
                mapId,
                tickRate,
                inputDelayFrames,
                randomSeed);
        }

        public static MobaGameStartSpec ConvertToGameStartSpec(
            LogicWorldSpawnData[] spawns,
            PlayerId playerId,
            string matchId,
            int mapId,
            int tickRate = 30,
            int inputDelayFrames = 0,
            int randomSeed = 0)
        {
            var startPlan = MobaBattleStartPlanBuilder.FromHostSpawns(
                ToHostSpawns(spawns),
                playerId,
                matchId,
                mapId,
                tickRate,
                inputDelayFrames,
                randomSeed);

            return startPlan.ToGameStartSpec();
        }

        private static MobaHostSpawnData[] ToHostSpawns(LogicWorldSpawnData[] spawns)
        {
            if (spawns == null || spawns.Length == 0)
            {
                return Array.Empty<MobaHostSpawnData>();
            }

            var result = new MobaHostSpawnData[spawns.Length];
            for (int i = 0; i < spawns.Length; i++)
            {
                result[i] = ToHostSpawn(spawns[i]);
            }

            return result;
        }

        private static MobaHostSpawnData ToHostSpawn(LogicWorldSpawnData spawn)
        {
            return new MobaHostSpawnData(
                spawn.PlayerId,
                spawn.CharacterId,
                spawn.TeamId,
                spawn.X,
                spawn.Y,
                spawn.Z,
                spawn.Name);
        }
    }
}

