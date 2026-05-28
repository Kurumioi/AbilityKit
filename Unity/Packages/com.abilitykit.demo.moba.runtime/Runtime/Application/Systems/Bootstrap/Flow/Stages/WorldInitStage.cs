using System;
using AbilityKit.Ability.Host;
using AbilityKit.Ability.Share.Impl.Moba.CreateWorld;
using AbilityKit.Ability.Share.Impl.Moba.Struct;
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
    /// 初始化世界（设置进入游戏请求）
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

            if (payloadLen == 0)
            {
                Log.Info("[WorldInitStage] WorldInitData payload is empty; skip SetEnterGameReq");
                return;
            }

            // CreateWorld stage: store game start spec for later StartGame (server adjudication)
            EnterMobaGameReq req;
            if (MobaCreateWorldInitCodec.TryDeserializeReq(init.Payload, out var initReq))
            {
                req = initReq;
            }
            else
            {
                req = EnterMobaGameCodec.DeserializeReq(init.Payload);
            }

            // Seed deterministic world random as early as possible.
            if (services.TryResolve<IWorldRandom>(out var random) && random is RollbackWorldRandom rr)
            {
                rr.SetSeed(req.RandomSeed);
                Log.Info($"[WorldInitStage] Seed world random success (seed={req.RandomSeed})");
            }

            var spec = new MobaGameStartSpec(in req);
            if (services.TryResolve<MobaEnterGameFlowService>(out var flow) && flow != null)
            {
                try
                {
                    var ctx = services.Resolve<Entitas.IContexts>();
                    var actorContext = ((Contexts)ctx).actor;
                    flow.ApplyGameStartSpec(actorContext, in spec);
                    Log.Info("[WorldInitStage] ApplyGameStartSpec success");

                    if (services.TryResolve<MobaGamePhaseService>(out var phase) && phase != null)
                    {
                        phase.SetInGame();
                    }
                }
                catch (Exception ex)
                {
                    // ET Demo provides player data via OnAllPlayersReady, not via WorldInitData
                    // Empty players in WorldInitData is expected and should not be treated as error
                    Log.Warning($"[WorldInitStage] ApplyGameStartSpec skipped (ET Demo uses OnAllPlayersReady): {ex.Message}");
                }
            }
            else
            {
                Log.Info("[WorldInitStage] MobaEnterGameFlowService not found; cannot ApplyGameStartSpec");
            }
        }
    }
}
