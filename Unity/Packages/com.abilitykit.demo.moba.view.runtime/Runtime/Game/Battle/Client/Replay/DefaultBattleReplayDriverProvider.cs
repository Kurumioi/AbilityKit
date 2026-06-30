using System;
using System.IO;
using AbilityKit.Core.Logging;
using AbilityKit.Core.Recording.FrameRecord;
using AbilityKit.Ability.World.Abstractions;

namespace AbilityKit.Game.Flow.Battle.Replay
{
    internal sealed class DefaultBattleReplayDriverProvider : IBattleReplayDriverProvider
    {
        public bool TryCreate(in BattleStartPlan plan, out FrameReplayDriver driver)
        {
            driver = null;

            try
            {
                var world = plan.World;
                if (string.IsNullOrEmpty(world.WorldId)) return false;

                var path = plan.RunModeOptions.InputReplayPath;
                if (string.IsNullOrEmpty(path)) return false;

                FrameRecordFile file;
                file = FrameRecordCodecs.Current.Load(path);
                if (file == null) return false;

                driver = new FrameReplayDriver(new WorldId(world.WorldId), file);
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
