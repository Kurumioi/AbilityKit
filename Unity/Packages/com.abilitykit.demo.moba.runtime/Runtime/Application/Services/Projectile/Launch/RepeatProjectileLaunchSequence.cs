using System;
using AbilityKit.Core.Common.Log;
using AbilityKit.Core.Common.Projectile;
using AbilityKit.Core.Continuous;

namespace AbilityKit.Demo.Moba.Services.Projectile.Launch
{
    public sealed class RepeatProjectileLaunchSequence : IMobaProjectileLaunchSequence
    {
        public bool TryStart(in MobaProjectileLaunchContext context, out MobaProjectileLaunchResult result)
        {
            result = default;
            if (context.Projectiles == null)
            {
                result = MobaProjectileLaunchResult.Failed("Projectile service is null");
                return false;
            }

            if (context.LauncherEntity == null)
            {
                result = MobaProjectileLaunchResult.Failed("Launcher entity is null");
                return false;
            }

            var patternProvider = new MobaModifierProjectileSpawnPatternProvider(
                context.SkillParamModifiers,
                context.Launcher.CountPerShot,
                context.Launcher.FanAngleDeg,
                context.BulletsPerShot,
                context.FallbackFanAngleDeg);

            var schedule = ProjectileScheduleParams.Repeat(context.StartFrame, context.IntervalFrames, context.RepeatCount);
            var launcherSource = context.LauncherSource;
            if (context.Links != null && launcherSource.IsValid)
            {
                context.Links.BindLauncherSource(context.LauncherActorId, in launcherSource);
            }

            var baseSpawn = context.BaseSpawn;
            var scheduleId = context.Projectiles.ScheduleEmit(patternProvider, in baseSpawn, in schedule);
            context.LauncherEntity.AddProjectileLauncher(
                newLauncherId: context.Launcher.Id,
                newProjectileId: context.Projectile.Id,
                newRootActorId: context.Request.CasterActorId,
                newEndTimeMs: context.EndTimeMs,
                newActiveBullets: 0,
                newScheduleId: scheduleId.Value,
                newIntervalFrames: context.IntervalFrames,
                newTotalCount: context.TotalCount);

            result = new MobaProjectileLaunchResult(
                true,
                context.LauncherActorId,
                scheduleId,
                context.IntervalFrames,
                context.TotalCount,
                context.StartFrame,
                context.EndTimeMs,
                context.LauncherEntity,
                this,
                context.Projectiles,
                context.Runtime,
                null);
            return true;
        }

        public bool IsComplete(in MobaProjectileLaunchResult result)
        {
            if (!result.Success) return true;

            var runtime = result.SequenceRuntime;
            var currentFrame = runtime != null ? runtime.CurrentFrame : result.StartFrame + 1;
            if (currentFrame <= result.StartFrame) return false;

            var launcherEntity = result.LauncherEntity;
            if (launcherEntity == null && result.LauncherActorId > 0 && runtime != null)
            {
                runtime.TryGetLauncherEntity(result.LauncherActorId, out launcherEntity);
            }

            if (launcherEntity == null || !launcherEntity.hasProjectileLauncher) return true;

            var plc = launcherEntity.projectileLauncher;
            var nowMs = runtime != null ? runtime.NowMs : result.EndTimeMs;
            if (plc.EndTimeMs > 0 && nowMs < plc.EndTimeMs) return false;
            return plc.ActiveBullets <= 0;
        }

        public void Stop(in MobaProjectileLaunchResult result, ContinuousEndReason reason)
        {
            if (!result.Success) return;

            var runtime = result.SequenceRuntime;
            var launcherEntity = result.LauncherEntity;
            if (launcherEntity == null && result.LauncherActorId > 0 && runtime != null)
            {
                runtime.TryGetLauncherEntity(result.LauncherActorId, out launcherEntity);
            }

            var projectiles = result.ProjectileService;
            if (projectiles != null && result.ScheduleId.Value > 0)
            {
                try { projectiles.CancelSchedule(result.ScheduleId); }
                catch (Exception ex) { Log.Exception(ex, $"[RepeatProjectileLaunchSequence] Cancel launch schedule failed (scheduleId={result.ScheduleId.Value})"); }
            }

            if (launcherEntity == null || !launcherEntity.hasProjectileLauncher) return;

            var plc = launcherEntity.projectileLauncher;
            var nowMs = runtime != null ? runtime.NowMs : result.EndTimeMs;
            launcherEntity.ReplaceProjectileLauncher(
                newLauncherId: plc.LauncherId,
                newProjectileId: plc.ProjectileId,
                newRootActorId: plc.RootActorId,
                newEndTimeMs: nowMs,
                newActiveBullets: plc.ActiveBullets,
                newScheduleId: 0,
                newIntervalFrames: plc.IntervalFrames,
                newTotalCount: plc.TotalCount);
        }
    }
}
