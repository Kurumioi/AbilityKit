using System;
using AbilityKit.Ability.Host;
using AbilityKit.Ability.Host.Extensions.Moba.CreateWorld;
using AbilityKit.Protocol.Moba;

namespace AbilityKit.Ability.Host.Extensions.Moba.Struct
{
    public readonly struct MobaRoomLoadoutOverrides
    {
        public readonly int Level;
        public readonly int AttributeTemplateId;
        public readonly int BasicAttackSkillId;
        public readonly int[] SkillIds;

        public MobaRoomLoadoutOverrides(int level, int attributeTemplateId, int basicAttackSkillId, int[] skillIds)
        {
            Level = level;
            AttributeTemplateId = attributeTemplateId;
            BasicAttackSkillId = basicAttackSkillId;
            SkillIds = skillIds;
        }

        public bool HasAnyOverride => Level > 0 || AttributeTemplateId > 0 || BasicAttackSkillId > 0 || (SkillIds != null && SkillIds.Length > 0);
    }

    public readonly struct MobaHostSpawnData
    {
        public readonly int PlayerId;
        public readonly int HeroId;
        public readonly int TeamId;
        public readonly float X;
        public readonly float Y;
        public readonly float Z;
        public readonly string Name;

        public MobaHostSpawnData(int playerId, int heroId, int teamId, float x, float y, float z, string name = null)
        {
            PlayerId = playerId;
            HeroId = heroId;
            TeamId = teamId;
            X = x;
            Y = y;
            Z = z;
            Name = name;
        }

        public static MobaHostSpawnData CreateLocalPlayer(int playerId, int heroId, float x, float z)
        {
            return new MobaHostSpawnData(playerId, heroId, 1, x, 0f, z, "LocalPlayer");
        }
    }

    public readonly struct MobaRoomPlayerSlot
    {
        public readonly PlayerId PlayerId;
        public readonly int TeamId;
        public readonly int HeroId;
        public readonly int SpawnPointId;
        public readonly MobaRoomLoadoutOverrides Overrides;

        public MobaRoomPlayerSlot(PlayerId playerId, int teamId, int heroId, int spawnPointId, in MobaRoomLoadoutOverrides overrides)
        {
            PlayerId = playerId;
            TeamId = teamId;
            HeroId = heroId;
            SpawnPointId = spawnPointId;
            Overrides = overrides;
        }

        public MobaPlayerLoadout ToPlayerLoadout(int spawnIndexFallback)
        {
            var ov = Overrides;

            var level = ov.Level > 0 ? ov.Level : 1;
            var attributeTemplateId = ov.AttributeTemplateId;
            var basicAttackSkillId = ov.BasicAttackSkillId;
            var skillIds = ov.SkillIds;

            return new MobaPlayerLoadout(
                playerId: PlayerId,
                teamId: TeamId,
                heroId: HeroId,
                attributeTemplateId: attributeTemplateId,
                level: level,
                basicAttackSkillId: basicAttackSkillId,
                skillIds: skillIds,
                spawnIndex: SpawnPointId > 0 ? SpawnPointId : spawnIndexFallback);
        }
    }

    public readonly struct MobaRoomGameStartSpec
    {
        public readonly string MatchId;
        public readonly int MapId;
        public readonly int GameplayId;

        public readonly int RandomSeed;
        public readonly int TickRate;
        public readonly int InputDelayFrames;

        public readonly MobaRoomPlayerSlot[] Players;

        public MobaRoomGameStartSpec(string matchId, int mapId, int randomSeed, int tickRate, int inputDelayFrames, MobaRoomPlayerSlot[] players, int gameplayId = 0)
        {
            MatchId = matchId;
            MapId = mapId;
            GameplayId = gameplayId;
            RandomSeed = randomSeed;
            TickRate = tickRate;
            InputDelayFrames = inputDelayFrames;
            Players = players;
        }

        public EnterMobaGameReq ToEnterReq(PlayerId localPlayerId)
        {
            var ps = Players;
            if (ps == null || ps.Length == 0)
            {
                return new EnterMobaGameReq(
                    playerId: localPlayerId,
                    matchId: MatchId,
                    mapId: MapId,
                    randomSeed: RandomSeed,
                    tickRate: TickRate,
                    inputDelayFrames: InputDelayFrames,
                    opCode: 0,
                    payload: null,
                    players: null,
                    gameplayId: GameplayId);
            }

            var loadouts = new MobaPlayerLoadout[ps.Length];
            for (int i = 0; i < ps.Length; i++)
            {
                loadouts[i] = ps[i].ToPlayerLoadout(spawnIndexFallback: i);
            }

            return new EnterMobaGameReq(
                playerId: localPlayerId,
                matchId: MatchId,
                mapId: MapId,
                randomSeed: RandomSeed,
                tickRate: TickRate,
                inputDelayFrames: InputDelayFrames,
                opCode: 0,
                payload: null,
                players: loadouts,
                gameplayId: GameplayId);
        }

    }

    public static class MobaHostSpawnPlanBuilder
    {
        public const int DefaultLevel = 1;
        public const int DefaultUnitSubType = 1;
        public const int DefaultMainType = 1;
        public const int DefaultBasicAttackSkillId = 1001;

        private static readonly int[] DefaultSkillIds = { 1001, 1002, 1003, 1004 };

        public static MobaPlayerLoadout[] ToLoadouts(MobaHostSpawnData[] spawns, int startIndex = 0)
        {
            if (spawns == null || spawns.Length == 0)
            {
                return Array.Empty<MobaPlayerLoadout>();
            }

            var loadouts = new MobaPlayerLoadout[spawns.Length];
            for (int i = 0; i < spawns.Length; i++)
            {
                loadouts[i] = ToLoadout(spawns[i], startIndex + i);
            }

            return loadouts;
        }

        public static MobaPlayerLoadout ToLoadout(MobaHostSpawnData spawn, int spawnIndex)
        {
            return new MobaPlayerLoadout(
                playerId: new PlayerId(spawn.PlayerId.ToString()),
                teamId: spawn.TeamId,
                heroId: spawn.HeroId,
                attributeTemplateId: 0,
                level: DefaultLevel,
                basicAttackSkillId: DefaultBasicAttackSkillId,
                skillIds: CloneDefaultSkillIds(),
                spawnIndex: spawnIndex,
                unitSubType: DefaultUnitSubType,
                mainType: DefaultMainType,
                hasSpawnPosition: 1,
                spawnX: spawn.X,
                spawnY: spawn.Y,
                spawnZ: spawn.Z);
        }

        public static EnterMobaGameReq ToEnterReq(
            MobaHostSpawnData[] spawns,
            PlayerId localPlayerId,
            string matchId,
            int mapId,
            int tickRate = 30,
            int inputDelayFrames = 0,
            int randomSeed = 0,
            int gameplayId = 0,
            int enterGameOpCode = 0,
            byte[] enterGamePayload = null)
        {
            if (spawns == null || spawns.Length == 0)
            {
                throw new ArgumentException("Spawns cannot be null or empty", nameof(spawns));
            }

            var seed = randomSeed != 0 ? randomSeed : Environment.TickCount;
            return new EnterMobaGameReq(
                playerId: localPlayerId,
                matchId: matchId,
                mapId: mapId,
                randomSeed: seed,
                tickRate: tickRate,
                inputDelayFrames: inputDelayFrames,
                opCode: enterGameOpCode,
                payload: enterGamePayload,
                players: ToLoadouts(spawns),
                gameplayId: gameplayId);
        }

        public static MobaBattleStartPlan ToStartPlan(
            MobaHostSpawnData[] spawns,
            PlayerId localPlayerId,
            string matchId,
            int mapId,
            int tickRate = 30,
            int inputDelayFrames = 0,
            int randomSeed = 0,
            int gameplayId = 0,
            int enterGameOpCode = 0,
            byte[] enterGamePayload = null)
        {
            var req = ToEnterReq(
                spawns,
                localPlayerId,
                matchId,
                mapId,
                tickRate,
                inputDelayFrames,
                randomSeed,
                gameplayId,
                enterGameOpCode,
                enterGamePayload);

            return MobaBattleStartPlan.FromEnterReq(in req);
        }

        private static int[] CloneDefaultSkillIds()
        {
            var skills = new int[DefaultSkillIds.Length];
            Array.Copy(DefaultSkillIds, skills, DefaultSkillIds.Length);
            return skills;
        }
    }
}

