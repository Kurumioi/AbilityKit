using AbilityKit.Ability.World.DI;
using AbilityKit.Core.Common.Log;
using AbilityKit.Demo.Moba.Services;

namespace AbilityKit.Demo.Moba.Systems.Bootstrap.Flow.Stages
{
    [MobaBootstrapStage]
    public sealed class StartGameStage : MobaBootstrapStageBase
    {
        public override string Name => "StartGame";

        public override string[] Dependencies => new[]
        {
            "Install.WorldInit",
        };

        protected internal override void Install(
            Entitas.IContexts contexts,
            Entitas.Systems systems,
            IWorldResolver services)
        {
            if (!services.TryResolve<MobaGameStartSpecService>(out var specs) || !specs.TryGet(out var spec))
            {
                Log.Info("[StartGameStage] no pending game start spec; skip start game");
                return;
            }

            if (!services.TryResolve<IMobaGameStartPort>(out var gameStart))
            {
                Log.Error("[StartGameStage] IMobaGameStartPort not found; cannot start game");
                return;
            }

            var result = gameStart.TryStartGame(in spec);
            if (result.Succeeded)
            {
                specs.Clear();
                Log.Info("[StartGameStage] game start spec applied");
                return;
            }

            Log.Error($"[StartGameStage] game start spec rejected. {result}");
        }
    }
}
