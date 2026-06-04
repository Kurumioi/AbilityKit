using AbilityKit.Ability.Host;
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
}

