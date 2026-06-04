using AbilityKit.Ability.Host;
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
                Log.Info("[WorldInitStage] WorldInitData not found; skip SetEnterGameReq");
                return;
            }

            var payloadLen = init.Payload != null ? init.Payload.Length : 0;
            Log.Info($"[WorldInitStage] WorldInitData found. opCode={init.OpCode}, payloadLen={payloadLen}");

            if (init.OpCode != MobaWorldBootstrapModule.InitOpCode)
            {
                Log.Error($"[WorldInitStage] WorldInitData opCode mismatch. expected={MobaWorldBootstrapModule.InitOpCode}, actual={init.OpCode}");
                return;
            }

            if (payloadLen == 0)
            {
                Log.Info("[WorldInitStage] WorldInitData payload is empty; skip SetEnterGameReq");
                return;
            }

            if (!MobaCreateWorldInitCodec.TryDeserialize(init.Payload, out var initPayload, out var deserializeError))
            {
                Log.Error($"[WorldInitStage] WorldInitData payload is not a valid create-world init payload. error={deserializeError}");
                return;
            }

            var validation = MobaProtocolValidation.ValidateCreateWorldSpecEnvelope(initPayload.LocalPlayerId, in initPayload.Spec);
            if (!validation.IsValid)
            {
                Log.Error($"[WorldInitStage] WorldInitData payload validation failed. {validation}");
                return;
            }

            var spec = initPayload.ToGameStartSpec();
            if (services.TryResolve<MobaGameStartSpecService>(out var specService))
            {
                specService.Set(in spec);
                Log.Info("[WorldInitStage] WorldInitData decoded; game start spec stored");
            }
            else
            {
                Log.Error("[WorldInitStage] MobaGameStartSpecService not found; cannot store game start spec");
            }

            // Seed deterministic world random as early as possible.
            if (services.TryResolve<IWorldRandom>(out var random) && random is RollbackWorldRandom rr)
            {
                rr.SetSeed(initPayload.Spec.RandomSeed);
                Log.Info($"[WorldInitStage] Seed world random success (seed={initPayload.Spec.RandomSeed})");
            }
        }
    }
}

