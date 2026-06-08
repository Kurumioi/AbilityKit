using System;
using AbilityKit.Ability.Host;
using AbilityKit.Ability.Host.Extensions.Moba.CreateWorld;
using AbilityKit.Protocol.Moba.CreateWorld;
using AbilityKit.Ability.World.DI;
using AbilityKit.Ability.World.Services;
using AbilityKit.Core.Common.Log;
using AbilityKit.Demo.Moba.Rollback;
using AbilityKit.Demo.Moba.Serialization;
using AbilityKit.Demo.Moba.Services;
using AbilityKit.Protocol.Moba;

namespace AbilityKit.Demo.Moba.Systems.Bootstrap.Flow.Stages
{
    /// <summary>
    /// WorldInit Install Stage
    /// 鍒濆鍖栦笘鐣岋紙璁剧疆杩涘叆娓告垙璇锋眰锛?
    /// </summary>
    [MobaBootstrapStage]
    public sealed class WorldInitStage : MobaBootstrapStageBase
    {
        public override string Name => "Install.WorldInit";

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

            var validation = MobaProtocolValidation.ValidateCreateWorldSpecEnvelope(initPayload.LocalPlayerId, in initPayload.Spec);
            if (!validation.IsValid)
            {
                throw new InvalidOperationException($"WorldInitStage create-world init payload validation failed. {validation}");
            }

            var startPlan = new MobaBattleStartPlan(initPayload.LocalPlayerId, in initPayload.Spec, initPayload.OpCode, initPayload.Payload);
            if (!services.TryResolve<MobaGameStartSpecService>(out var specService) || specService == null)
            {
                throw new InvalidOperationException("WorldInitStage requires MobaGameStartSpecService to store the decoded battle start plan.");
            }

            specService.SetPlan(in startPlan);
            Log.Info("[WorldInitStage] WorldInitData decoded; battle start plan stored");

            // Seed deterministic world random as early as possible.
            if (!services.TryResolve<IWorldRandom>(out var random) || random is not RollbackWorldRandom rr)
            {
                throw new InvalidOperationException("WorldInitStage requires RollbackWorldRandom for deterministic battle startup.");
            }

            rr.SetSeed(initPayload.Spec.RandomSeed);
            Log.Info($"[WorldInitStage] Seed world random success (seed={initPayload.Spec.RandomSeed})");
        }
    }
}

