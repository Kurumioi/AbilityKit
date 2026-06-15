using System;
using AbilityKit.Core.Logging;
using AbilityKit.Core.Recording.Lockstep;
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

        Exception ISessionEventsHost.PendingSubFeatureValidationFailure
        {
            get => _pendingSubFeatureValidationFailure;
            set => _pendingSubFeatureValidationFailure = value;
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
