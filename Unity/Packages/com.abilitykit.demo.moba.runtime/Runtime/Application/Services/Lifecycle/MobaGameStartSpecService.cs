using System;
using AbilityKit.Ability.Host.Extensions.Moba.CreateWorld;
using AbilityKit.Ability.Host.Extensions.Moba.StartSources;
using AbilityKit.Core.Logging;
using AbilityKit.Protocol.Moba;
using AbilityKit.Ability.World.Services;
using AbilityKit.Ability.World.Services.Attributes;

namespace AbilityKit.Demo.Moba.Services
{
    [WorldService(typeof(IMobaPendingGameStartSpecStore))]
    [WorldService(typeof(MobaGameStartSpecService))]
    public sealed class MobaGameStartSpecService : IService, IMobaPendingGameStartSpecStore
    {
        private MobaGameStartSpec _spec;
        private MobaBattleStartPlan _plan;

        public bool HasSpec { get; private set; }
        public bool HasPlan { get; private set; }

        public void Set(in MobaGameStartSpec spec)
        {
            var normalizedSpec = NormalizeGameStartSpec(in spec);
            var validation = ValidateSpec(in normalizedSpec);
            if (!validation.Succeeded)
            {
                throw new System.InvalidOperationException("invalid battle game start spec. " + validation.Message);
            }

            var plan = MobaBattleStartPlan.FromEnterReq(in normalizedSpec.EnterReq);
            var planValidation = ValidatePlan(in plan);
            if (!planValidation.Succeeded)
            {
                throw new System.InvalidOperationException("invalid battle start plan. " + planValidation.Message);
            }

            _spec = normalizedSpec;
            _plan = plan;
            HasSpec = true;
            HasPlan = true;
        }

        public bool TryGet(out MobaGameStartSpec spec)
        {
            spec = _spec;
            return HasSpec;
        }

        public bool TryGetPlan(out MobaBattleStartPlan plan)
        {
            plan = _plan;
            return HasPlan;
        }

        public MobaBattleStartPlanValidationResult ValidatePendingPlan()
        {
            if (!HasPlan)
            {
                return MobaBattleStartPlanValidationResult.Fail("pending battle start plan is missing.");
            }

            return ValidatePlan(in _plan);
        }

        public MobaGameStartSpecValidationResult ValidatePendingSpec()
        {
            if (!HasSpec)
            {
                return MobaGameStartSpecValidationResult.Fail("pending battle game start spec is missing.");
            }

            return ValidateSpec(in _spec);
        }

        private static MobaGameStartSpec NormalizeGameStartSpec(in MobaGameStartSpec spec)
        {
            var req = spec.EnterReq;
            var players = req.Players;
            if (players == null || players.Length == 0)
            {
                return spec;
            }

            MobaPlayerLoadout[] normalizedPlayers = null;
            for (int i = 0; i < players.Length; i++)
            {
                var p = players[i];
                if (p.BasicAttackSkillId > 0)
                {
                    continue;
                }

                normalizedPlayers ??= CopyPlayers(players);
                var repairedBasicAttackSkillId = ResolveFallbackBasicAttackSkillId(p.HeroId);
                normalizedPlayers[i] = new MobaPlayerLoadout(
                    playerId: p.PlayerId,
                    teamId: p.TeamId,
                    heroId: p.HeroId,
                    attributeTemplateId: p.AttributeTemplateId,
                    level: p.Level,
                    basicAttackSkillId: repairedBasicAttackSkillId,
                    skillIds: p.SkillIds,
                    spawnIndex: p.SpawnIndex,
                    unitSubType: p.UnitSubType,
                    mainType: p.MainType,
                    hasSpawnPosition: p.HasSpawnPosition,
                    spawnX: p.SpawnX,
                    spawnY: p.SpawnY,
                    spawnZ: p.SpawnZ);
                Log.Warning($"[MobaGameStartSpecService] repaired invalid BasicAttackSkillId before validation. playerIndex={i}, heroId={p.HeroId}, repaired={repairedBasicAttackSkillId}");
            }

            if (normalizedPlayers == null)
            {
                return spec;
            }

            var normalizedReq = new EnterMobaGameReq(
                playerId: req.PlayerId,
                matchId: req.MatchId,
                mapId: req.MapId,
                randomSeed: req.RandomSeed,
                tickRate: req.TickRate,
                inputDelayFrames: req.InputDelayFrames,
                opCode: req.OpCode,
                payload: req.Payload,
                players: normalizedPlayers,
                gameplayId: req.GameplayId);
            return new MobaGameStartSpec(in normalizedReq);
        }

        private static MobaPlayerLoadout[] CopyPlayers(MobaPlayerLoadout[] players)
        {
            var copy = new MobaPlayerLoadout[players.Length];
            Array.Copy(players, copy, players.Length);
            return copy;
        }

        private static int ResolveFallbackBasicAttackSkillId(int heroId)
        {
            return 1;
        }

        public static MobaGameStartSpecValidationResult ValidateSpec(in MobaGameStartSpec spec)
        {
            var enterReq = spec.EnterReq;
            var enterValidation = MobaProtocolValidation.ValidateEnterGameReq(in enterReq);
            if (!enterValidation.IsValid)
            {
                return MobaGameStartSpecValidationResult.Fail("enter-game request invalid. " + enterValidation);
            }

            return MobaGameStartSpecValidationResult.Success;
        }

        public static MobaBattleStartPlanValidationResult ValidatePlan(in MobaBattleStartPlan plan)
        {
            var enterReq = plan.ToEnterReq();
            var enterValidation = MobaProtocolValidation.ValidateEnterGameReq(in enterReq);
            if (!enterValidation.IsValid)
            {
                return MobaBattleStartPlanValidationResult.Fail("battle start plan enter-game projection invalid. " + enterValidation);
            }

            if (plan.LocalPlayerId.Value != enterReq.PlayerId.Value)
            {
                return MobaBattleStartPlanValidationResult.Fail($"battle start plan local player mismatch. plan={plan.LocalPlayerId.Value}, enterReq={enterReq.PlayerId.Value}");
            }

            return MobaBattleStartPlanValidationResult.Success;
        }

        public void Clear()
        {
            _spec = default;
            _plan = default;
            HasSpec = false;
            HasPlan = false;
        }

        public void Dispose()
        {
            Clear();
        }
    }
}

