using System;
using AbilityKit.Core.Common.Log;
using AbilityKit.Core.Common.Record.Lockstep;
using AbilityKit.Game.Battle;
using AbilityKit.Game.Flow.Battle.Modules;

namespace AbilityKit.Game.Flow
{
    public sealed partial class BattleSessionFeature
    {
        void ISessionEventsHost.OnStartSessionRequested() => OnStartSessionRequested();

        void ISessionEventsHost.RaiseSessionStarted(BattleStartPlan plan)
        {
            Log.Info("[BattleSessionFeature] Session started");
            SessionStarted?.Invoke();
            Hooks?.SessionStarted.Invoke(plan);
        }

        void ISessionEventsHost.RaiseSessionFailed(Exception exception)
        {
            SessionFailed?.Invoke(exception);
            Hooks?.SessionFailed.Invoke(exception);
        }

        void ISessionEventsHost.RaiseFirstFrameReceived()
        {
            FirstFrameReceived?.Invoke();
            Hooks?.FirstFrameReceived.Invoke();
        }

        BattleContext ISessionEventsHost.Context => _ctx;

        Exception ISessionEventsHost.PendingModuleValidationFailure
        {
            get => _pendingModuleValidationFailure;
            set => _pendingModuleValidationFailure = value;
        }

        BattleSessionHooks ISessionEventsHost.Hooks
        {
            get => Hooks;
            set => Hooks = value;
        }

        internal BattleSessionHooks Hooks { get; private set; }

        public event Action SessionStarted;
        public event Action FirstFrameReceived;
        public event Action<Exception> SessionFailed;
    }
}
