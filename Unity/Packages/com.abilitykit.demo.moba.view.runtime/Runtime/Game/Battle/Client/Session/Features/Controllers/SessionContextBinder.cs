using AbilityKit.Game.Flow.Battle.Modules;

namespace AbilityKit.Game.Flow
{
    internal static class SessionContextBinder
    {
        public static void BindRuntimeSession(
            BattleContext ctx,
            BattleSessionState state,
            BattleSessionHandles handles)
        {
            if (ctx == null) return;

            ctx.Session = handles.Session;
            BindLastFrame(ctx, state);
        }

        public static void BindLastFrame(BattleContext ctx, BattleSessionState state)
        {
            if (ctx == null || state == null) return;

            ctx.LastFrame = state.Tick.LastFrame;
        }

        public static void BindSession(
            BattleContext ctx,
            BattleSessionState state,
            BattleSessionHandles handles,
            BattleSessionHooks hooks,
            BattleStartPlan plan)
        {
            if (ctx == null) return;

            ctx.Plan = plan;
            BindRuntimeSession(ctx, state, handles);
            ctx.Hooks = hooks;
        }

        public static void ClearSession(BattleContext ctx)
        {
            if (ctx == null) return;

            ctx.Session = null;
            ctx.Hooks = null;
        }
    }
}
