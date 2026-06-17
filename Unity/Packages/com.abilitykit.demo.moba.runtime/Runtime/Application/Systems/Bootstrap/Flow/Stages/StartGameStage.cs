using System;
using AbilityKit.Ability.Host.Extensions.Moba.Runtime;
using AbilityKit.Ability.World.DI;
using AbilityKit.Core.Logging;
using AbilityKit.Demo.Moba.Services;

namespace AbilityKit.Demo.Moba.Systems.Bootstrap.Flow.Stages
{
    [MobaBootstrapStage]
    public sealed class StartGameStage : MobaBootstrapStageBase
    {
        public override string Name => MobaBootstrapStageNames.StartGame;

        public override string[] Dependencies => new[]
        {
            MobaBootstrapStageNames.WorldInit,
        };

        protected internal override void Install(
            Entitas.IContexts contexts,
            Entitas.Systems systems,
            IWorldResolver services)
        {
            if (!services.TryResolve<MobaGameStartSpecService>(out var specs) || specs == null || !specs.TryGet(out var spec))
            {
                throw new InvalidOperationException("StartGameStage requires a pending MobaGameStartSpec produced by WorldInitStage.");
            }

            if (!services.TryResolve<IMobaGameStartPort>(out var gameStart) || gameStart == null)
            {
                throw new InvalidOperationException("StartGameStage requires IMobaGameStartPort to start the battle.");
            }

            var result = gameStart.TryStartGame(in spec);
            if (result.Succeeded)
            {
                specs.Clear();
                Log.Info("[StartGameStage] game start spec applied");
                return;
            }

            throw new InvalidOperationException($"StartGameStage game start spec rejected. {result}");
        }
    }
}
