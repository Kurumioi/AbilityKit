using AbilityKit.Demo.Moba.Console.Battle.Context;
using AbilityKit.Demo.Moba.Console.Platform;
using AbilityKit.Demo.Moba.Console.View;

namespace AbilityKit.Demo.Moba.Console.Battle.Features
{
    /// <summary>
    /// Console 视图 Feature，作为同步与 HUD 的依赖锚点。
    /// </summary>
    public sealed class ConsoleViewFeature : ConsoleSubFeatureBase
    {
        private readonly IConsoleBattleView _battleView;

        public ConsoleViewFeature(IConsoleBattleView battleView)
        {
            _battleView = battleView ?? throw new System.ArgumentNullException(nameof(battleView));
        }

        protected override string GetSubFeatureId() => "console_view_feature";

        public override void OnAttach(ConsoleBattleContext ctx)
        {
            base.OnAttach(ctx);
            _battleView.OnGameStart(ctx.Plan.MaxPlayerCount);
            Log.View("[View] Attached");
        }

        public override void OnDetach(ConsoleBattleContext ctx)
        {
            Log.View("[View] Detached");
            base.OnDetach(ctx);
        }

        public override void Tick(ConsoleBattleContext ctx, float deltaTime)
        {
            if (Context == null) return;

            _battleView.Tick(deltaTime);
        }
    }
}
