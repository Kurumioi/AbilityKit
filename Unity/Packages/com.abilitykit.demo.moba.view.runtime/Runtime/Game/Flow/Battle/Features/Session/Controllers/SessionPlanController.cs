using System;
using AbilityKit.Core.Common.Log;
using AbilityKit.Game.Flow.Battle.Modules;

namespace AbilityKit.Game.Flow
{
    public sealed partial class BattleSessionFeature
    {
        internal interface ISessionPlanHost
        {
            void StartSession();
            void StopSession();
            void ApplyAutoPlanActions();
            bool InvokeModulesPlanBuilt();
        }

        private sealed class SessionPlanController
        {
            public delegate void OnPlanBuilt(BattleStartPlan plan);
            public delegate void OnSessionStarted(BattleStartPlan plan);
            public delegate void OnSessionFailed(Exception exception);

            private OnPlanBuilt _onPlanBuilt;
            private OnSessionStarted _onSessionStarted;
            private OnSessionFailed _onSessionFailed;

            public void SetCallbacks(OnPlanBuilt onPlanBuilt, OnSessionStarted onSessionStarted, OnSessionFailed onSessionFailed)
            {
                _onPlanBuilt = onPlanBuilt;
                _onSessionStarted = onSessionStarted;
                _onSessionFailed = onSessionFailed;
            }

            public void OnAttach(
                ISessionPlanHost host,
                IBattleBootstrapper bootstrapper,
                BattleSessionState state,
                BattleSessionHandles handles,
                BattleSessionHooks hooks,
                BattleContext ctx)
            {
                if (host == null || state == null || handles == null) return;

                var plan = bootstrapper?.Build() ?? default;
                state.Plan = plan;

                _onPlanBuilt?.Invoke(plan);
                hooks?.PlanBuilt.Invoke(plan);

                Log.Info($"[BattleSessionFeature] OnAttach Plan: HostMode={plan.HostMode}, UseGatewayTransport={plan.UseGatewayTransport}, Gateway={plan.GatewayHost}:{plan.GatewayPort}, NumericRoomId={plan.NumericRoomId}, AutoConnect={plan.AutoConnect}, AutoCreateWorld={plan.AutoCreateWorld}, AutoJoin={plan.AutoJoin}, AutoReady={plan.AutoReady}, WorldId={plan.WorldId}, PlayerId={plan.PlayerId}");

                var planIntercepted = hooks != null && hooks.PlanBuilt.Invoke(plan);

                if (!(planIntercepted || host.InvokeModulesPlanBuilt()))
                {
                    try
                    {
                        host.StartSession();
                        _onSessionStarted?.Invoke(plan);
                        hooks?.SessionStarted.Invoke(plan);
                        host.ApplyAutoPlanActions();
                    }
                    catch (Exception ex)
                    {
                        Log.Exception(ex, "[BattleSessionFeature] StartSession failed in OnAttach");
                        host.StopSession();
                        _onSessionFailed?.Invoke(ex);
                        hooks?.SessionFailed.Invoke(ex);
                        return;
                    }
                }

                if (ctx != null)
                {
                    ctx.Plan = plan;
                    ctx.Session = handles.Session;
                    ctx.LastFrame = state.Tick.LastFrame;
                    ctx.Hooks = hooks;
                }
            }
        }
    }
}
