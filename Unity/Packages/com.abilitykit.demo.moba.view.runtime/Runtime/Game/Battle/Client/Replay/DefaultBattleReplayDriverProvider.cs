using System;
using System.IO;
using AbilityKit.Core.Logging;
using AbilityKit.Core.Recording.Lockstep;
using AbilityKit.Ability.World.Abstractions;

namespace AbilityKit.Game.Flow.Battle.Replay
{
    internal sealed class DefaultBattleReplayDriverProvider : IBattleReplayDriverProvider
    {
        public bool TryCreate(in BattleStartPlan plan, out LockstepReplayDriver driver)
        {
            driver = null;

            try
            {
                var world = plan.World;
                if (string.IsNullOrEmpty(world.WorldId)) return false;

                var path = plan.RunModeOptions.InputReplayPath;
                if (string.IsNullOrEmpty(path)) return false;

                LockstepInputRecordFile file;
                file = LockstepInputRecordCodecs.Current.Load(path);
                if (file == null) return false;

                driver = new LockstepReplayDriver(new WorldId(world.WorldId), file);
                return true;
            }
            catch (Exception ex)
            {
                Log.Exception(ex, "[DefaultBattleReplayDriverProvider] TryCreate failed");
                driver = null;
                return false;
            }
        }
    }
}
