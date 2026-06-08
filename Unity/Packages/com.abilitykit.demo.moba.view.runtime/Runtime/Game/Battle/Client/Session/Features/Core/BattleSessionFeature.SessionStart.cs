using System;
using AbilityKit.Core.Common.Log;

namespace AbilityKit.Game.Flow
{
    public sealed partial class BattleSessionFeature
    {
        private void OnStartSessionRequested()
        {
            try
            {
                Log.Info("[BattleSessionFeature] Starting session");
                StartSession();
                _eventsCtrl.NotifySessionStarted(this, _plan);
                ApplyAutoPlanActions();
            }
            catch (Exception ex)
            {
                Log.Exception(ex, "[BattleSessionFeature] StartSession failed after gateway room preparation");
                StopSession();
                _eventsCtrl.NotifySessionFailed(this, ex);
                return;
            }

            SessionContextBinder.BindSession(_ctx, _state, _handles, Hooks, _plan);
        }
    }
}
