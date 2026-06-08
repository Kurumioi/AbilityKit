using System;
using System.Linq;
using AbilityKit.Demo.Moba.Config.Core;
using AbilityKit.Protocol.Moba;

namespace AbilityKit.Demo.Moba.Services
{
    public static class MobaGameStartSpecNormalizer
    {
        public static EnterMobaGameReq Normalize(MobaConfigDatabase config, in EnterMobaGameReq req)
        {
            if (config == null) throw new InvalidOperationException("MobaGameStartSpecNormalizer requires MobaConfigDatabase.");
            if (req.Players == null || req.Players.Length == 0) return req;

            var src = req.Players;
            var dst = new MobaPlayerLoadout[src.Length];

            for (int i = 0; i < src.Length; i++)
            {
                var p = src[i];

                var attributeTemplateId = p.AttributeTemplateId;
                int[] skillIds = p.SkillIds;

                if (attributeTemplateId <= 0 || skillIds == null)
                {
                    if (!config.TryGetCharacter(p.HeroId, out var character) || character == null)
                    {
                        throw new InvalidOperationException($"Player loadout cannot be normalized because character config is missing. playerId={p.PlayerId.Value}, heroId={p.HeroId}");
                    }

                    if (attributeTemplateId <= 0)
                    {
                        attributeTemplateId = character.AttributeTemplateId;
                        if (attributeTemplateId <= 0)
                        {
                            throw new InvalidOperationException($"Player loadout cannot be normalized because character attribute template id is invalid. playerId={p.PlayerId.Value}, heroId={p.HeroId}, attributeTemplateId={attributeTemplateId}");
                        }
                    }

                    if (skillIds == null)
                    {
                        if (!config.TryGetAttributeTemplate(attributeTemplateId, out var attrTemplate) || attrTemplate == null)
                        {
                            throw new InvalidOperationException($"Player loadout cannot be normalized because attribute template config is missing. playerId={p.PlayerId.Value}, heroId={p.HeroId}, attributeTemplateId={attributeTemplateId}");
                        }

                        skillIds = attrTemplate.ActiveSkills != null ? attrTemplate.ActiveSkills.ToArray() : Array.Empty<int>();
                    }
                }

                dst[i] = new MobaPlayerLoadout(
                        playerId: p.PlayerId,
                        teamId: p.TeamId,
                        heroId: p.HeroId,
                        attributeTemplateId: attributeTemplateId,
                        level: p.Level,
                        basicAttackSkillId: p.BasicAttackSkillId,
                        skillIds: skillIds,
                        spawnIndex: p.SpawnIndex,
                        unitSubType: p.UnitSubType,
                        mainType: p.MainType,
                        hasSpawnPosition: p.HasSpawnPosition,
                        spawnX: p.SpawnX,
                        spawnY: p.SpawnY,
                        spawnZ: p.SpawnZ);
            }

            return new EnterMobaGameReq(
                playerId: req.PlayerId,
                matchId: req.MatchId,
                mapId: req.MapId,
                randomSeed: req.RandomSeed,
                tickRate: req.TickRate,
                inputDelayFrames: req.InputDelayFrames,
                opCode: req.OpCode,
                payload: req.Payload,
                players: dst,
                gameplayId: req.GameplayId);
        }
    }
}

