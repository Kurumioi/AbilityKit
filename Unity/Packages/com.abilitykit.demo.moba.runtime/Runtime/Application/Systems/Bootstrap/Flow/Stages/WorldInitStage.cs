using System;
using AbilityKit.Ability.Host;
using AbilityKit.Ability.Host.Extensions.Moba.StartSources;
using AbilityKit.Demo.Moba.Gameplay;
using AbilityKit.Protocol.Moba.CreateWorld;
using AbilityKit.Ability.World.DI;
using AbilityKit.Ability.World.Services;
using AbilityKit.Core.Logging;
using AbilityKit.Demo.Moba.Rollback;
using AbilityKit.Demo.Moba.Serialization;
using AbilityKit.Demo.Moba.Services;
using AbilityKit.Protocol.Moba;

namespace AbilityKit.Demo.Moba.Systems.Bootstrap.Flow.Stages
{
    /// <summary>
    /// 世界初始化阶段。
    /// 在确定性战斗启动前解码并验证创建世界请求。
    /// </summary>
    [MobaBootstrapStage]
    public sealed class WorldInitStage : MobaBootstrapStageBase
    {
        public override string Name => MobaBootstrapStageNames.WorldInit;

        public override string[] Dependencies => new[]
        {
            MobaBootstrapStageNames.TargetingAndSkills,
            MobaBootstrapStageNames.TriggerPlans,
        };

        protected internal override void Install(
            Entitas.IContexts contexts,
            Entitas.Systems systems,
            IWorldResolver services)
        {
            DemoWireSerializerBootstrap.TryInstallMemoryPack();

            if (!services.TryResolve<WorldInitData>(out var init))
            {
                throw new InvalidOperationException("WorldInitStage requires WorldInitData for formal battle startup.");
            }

            var payloadLen = init.Payload != null ? init.Payload.Length : 0;
            Log.Info($"[WorldInitStage] WorldInitData found. opCode={init.OpCode}, payloadLen={payloadLen}");

            if (init.OpCode != MobaWorldBootstrapModule.InitOpCode)
            {
                throw new InvalidOperationException($"WorldInitStage opCode mismatch. expected={MobaWorldBootstrapModule.InitOpCode}, actual={init.OpCode}");
            }

            if (payloadLen == 0)
            {
                throw new InvalidOperationException("WorldInitStage requires a non-empty create-world init payload.");
            }

            if (!MobaCreateWorldInitCodec.TryDeserialize(init.Payload, out var initPayload, out var deserializeError))
            {
                throw new InvalidOperationException($"WorldInitStage create-world init payload is invalid. error={deserializeError}");
            }

            initPayload = SanitizeInitPayload(in initPayload);

            var validation = MobaProtocolValidation.ValidateCreateWorldSpecEnvelope(initPayload.LocalPlayerId, in initPayload.Spec);
            if (!validation.IsValid)
            {
                throw new InvalidOperationException($"WorldInitStage create-world init payload validation failed. {validation}");
            }

            if (!services.TryResolve<IMobaPendingGameStartSpecStore>(out var specService) || specService == null)
            {
                throw new InvalidOperationException("WorldInitStage requires IMobaPendingGameStartSpecStore to store the decoded battle game start spec.");
            }

            var spec = initPayload.ToGameStartSpec();
            specService.Set(in spec);
            if (services.TryResolve<MobaGameplayConfigSettings>(out var gameplaySettings) && gameplaySettings != null)
            {
                gameplaySettings.DefaultGameplayId = initPayload.Spec.GameplayId;
            }

            Log.Info("[WorldInitStage] WorldInitData decoded; battle game start spec stored");
 
            // 尽早为确定性世界随机数设置种子。
            if (!services.TryResolve<IWorldRandom>(out var random) || random is not RollbackWorldRandom rr)
            {
                throw new InvalidOperationException("WorldInitStage requires RollbackWorldRandom for deterministic battle startup.");
            }

            rr.SetSeed(initPayload.Spec.RandomSeed);
            Log.Info($"[WorldInitStage] Seed world random success (seed={initPayload.Spec.RandomSeed})");
        }

        private static MobaCreateWorldInitPayload SanitizeInitPayload(in MobaCreateWorldInitPayload payload)
        {
            var spec = payload.Spec;
            var players = spec.Players;
            if (players == null || players.Length == 0)
            {
                return payload;
            }

            MobaPlayerLoadout[] sanitized = null;
            for (int i = 0; i < players.Length; i++)
            {
                var p = players[i];
                Log.Info($"[WorldInitStage] decoded player[{i}] playerId={p.PlayerId.Value}, heroId={p.HeroId}, attr={p.AttributeTemplateId}, level={p.Level}, basicAttack={p.BasicAttackSkillId}, skillCount={(p.SkillIds != null ? p.SkillIds.Length : 0)}, spawnIndex={p.SpawnIndex}");

                var basicAttackSkillId = p.BasicAttackSkillId;
                if (basicAttackSkillId > 0)
                {
                    continue;
                }

                sanitized ??= CopyPlayers(players);
                basicAttackSkillId = ResolveFallbackBasicAttackSkillId(p.HeroId);
                sanitized[i] = new MobaPlayerLoadout(
                    playerId: p.PlayerId,
                    teamId: p.TeamId,
                    heroId: p.HeroId,
                    attributeTemplateId: p.AttributeTemplateId,
                    level: p.Level,
                    basicAttackSkillId: basicAttackSkillId,
                    skillIds: p.SkillIds,
                    spawnIndex: p.SpawnIndex,
                    unitSubType: p.UnitSubType,
                    mainType: p.MainType,
                    hasSpawnPosition: p.HasSpawnPosition,
                    spawnX: p.SpawnX,
                    spawnY: p.SpawnY,
                    spawnZ: p.SpawnZ);
                Log.Warning($"[WorldInitStage] repaired invalid BasicAttackSkillId for player[{i}]. heroId={p.HeroId}, repaired={basicAttackSkillId}");
            }

            if (sanitized == null)
            {
                return payload;
            }

            var sanitizedSpec = new MobaCreateWorldSpec(
                matchId: spec.MatchId,
                mapId: spec.MapId,
                randomSeed: spec.RandomSeed,
                tickRate: spec.TickRate,
                inputDelayFrames: spec.InputDelayFrames,
                players: sanitized,
                gameplayId: spec.GameplayId);
            return new MobaCreateWorldInitPayload(payload.LocalPlayerId, in sanitizedSpec, payload.OpCode, payload.Payload);
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
    }
}

