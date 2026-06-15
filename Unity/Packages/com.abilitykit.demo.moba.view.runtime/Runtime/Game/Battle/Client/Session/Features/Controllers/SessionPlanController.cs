using System;
using AbilityKit.Core.Logging;
using AbilityKit.Game.Flow.Battle.Modules;

namespace AbilityKit.Game.Flow
{
    internal interface ISessionPlanHost
    {
        void StartSession();
        void StopSession();
        void ApplyAutoPlanActions();
        bool InvokeSubFeaturesPlanBuilt();
        void NotifySessionStarted(BattleStartPlan plan);
        void NotifySessionFailed(Exception exception);
    }

    internal sealed class SessionPlanController
    {
        public void OnAttach(
            ISessionPlanHost host,
            IBattleBootstrapper bootstrapper,
            BattleSessionState state,
            BattleSessionHandles handles,
            BattleSessionHooks hooks,
            BattleContext ctx)
        {
            if (host == null || state == null || handles == null) return;

            var plan = BuildPlan(bootstrapper);
            state.Plan = plan;

            LogPlan(plan);

            if (!IsSessionStartIntercepted(host, hooks, plan))
            {
                if (!TryStartSession(host, plan)) return;
            }

            SessionContextBinder.BindSession(ctx, state, handles, hooks, plan);
        }

        private static BattleStartPlan BuildPlan(IBattleBootstrapper bootstrapper)
        {
            return bootstrapper?.Build() ?? default;
        }

        private static void LogPlan(BattleStartPlan plan)
        {
            var world = plan.World;
            var gateway = plan.Gateway;
            var auto = plan.Auto;
            Log.Info($"[BattleSessionFeature] OnAttach Plan: HostMode={plan.HostMode}, UseGatewayTransport={gateway.UseGatewayTransport}, Gateway={gateway.Host}:{gateway.Port}, NumericRoomId={gateway.NumericRoomId}, AutoConnect={auto.AutoConnect}, AutoCreateWorld={auto.AutoCreateWorld}, AutoJoin={auto.AutoJoin}, AutoReady={auto.AutoReady}, WorldId={world.WorldId}, PlayerId={world.PlayerId}");
        }

        private static bool IsSessionStartIntercepted(ISessionPlanHost host, BattleSessionHooks hooks, BattleStartPlan plan)
        {
            if (hooks != null && hooks.PlanBuilt.Invoke(plan)) return true;

            return host.InvokeSubFeaturesPlanBuilt();
        }

        private static bool TryStartSession(ISessionPlanHost host, BattleStartPlan plan)
        {
            try
            {
                host.StartSession();
                host.NotifySessionStarted(plan);
                host.ApplyAutoPlanActions();
                return true;
            }
            catch (Exception ex)
            {
                Log.Exception(ex, "[BattleSessionFeature] StartSession failed in OnAttach");
                host.StopSession();
                host.NotifySessionFailed(ex);
                return false;
            }
        }

    }
}
