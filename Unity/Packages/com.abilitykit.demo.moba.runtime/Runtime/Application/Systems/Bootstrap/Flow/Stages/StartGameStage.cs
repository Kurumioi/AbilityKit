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

            if (!services.TryResolve<MobaEnterGameFlowService>(out var enterGame))
            {
                Log.Error("[StartGameStage] MobaEnterGameFlowService not found; cannot start game");
                return;
            }

            var actorContext = (contexts as global::Contexts)?.actor;
            if (actorContext == null)
            {
                Log.Error("[StartGameStage] ActorContext is null; cannot start game");
                return;
            }

            if (enterGame.ApplyGameStartSpec(actorContext, in spec))
            {
                specs.Clear();
                Log.Info("[StartGameStage] game start spec applied");
            }
        }
    }
}
