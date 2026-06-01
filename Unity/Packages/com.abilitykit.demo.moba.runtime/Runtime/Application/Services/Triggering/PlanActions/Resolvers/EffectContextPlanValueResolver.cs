using AbilityKit.Demo.Moba.Services;
using AbilityKit.Demo.Moba.Services.Projectile;
using AbilityKit.Core.Math;

namespace AbilityKit.Demo.Moba.Services.Triggering.PlanActions
{
    internal static class EffectContextPlanValueResolver
    {
        public static bool TryGetCasterActorId(object args, out int casterActorId)
        {
            casterActorId = 0;

            if (args is ProjectileHitArgs pha)
            {
                casterActorId = pha.CasterActorId;
                return casterActorId > 0;
            }

            if (args is IMobaTriggerInvocationContext invocation)
            {
                casterActorId = invocation.SourceActorId;
                return casterActorId > 0;
            }

            if (args is IEffectContext ec)
            {
                casterActorId = ec.SourceActorId;
                return casterActorId > 0;
            }

            return false;
        }

        public static bool TryGetTargetActorId(object args, out int targetActorId)
        {
            targetActorId = 0;

            if (args is ProjectileHitArgs pha)
            {
                targetActorId = pha.TargetActorId;
                return targetActorId > 0;
            }

            if (args is IMobaTriggerInvocationContext invocation)
            {
                targetActorId = invocation.TargetActorId;
                return targetActorId > 0;
            }

            if (args is IEffectContext ec)
            {
                targetActorId = ec.TargetActorId;
                return targetActorId > 0;
            }

            return false;
        }

        public static bool TryGetAim(object args, out Vec3 aimPos, out Vec3 aimDir)
        {
            aimPos = Vec3.Zero;
            aimDir = Vec3.Zero;

            if (args is IEffectContext ec)
            {
                if (ec.Kind != EffectContextKind.Skill) return false;
                if (!ec.TryGetSkill(out var skill)) return false;

                aimPos = skill.AimPos;
                aimDir = skill.AimDir;
                return true;
            }

            return false;
        }

        public static bool TryGetAimPos(object args, out Vec3 aimPos)
        {
            aimPos = Vec3.Zero;

            if (args is IEffectContext ec)
            {
                if (ec.Kind != EffectContextKind.Skill) return false;
                if (!ec.TryGetSkill(out var skill)) return false;

                aimPos = skill.AimPos;
                return true;
            }

            return false;
        }

        public static bool TryGetAimDir(object args, out Vec3 aimDir)
        {
            aimDir = Vec3.Zero;

            if (args is IEffectContext ec)
            {
                if (ec.Kind != EffectContextKind.Skill) return false;
                if (!ec.TryGetSkill(out var skill)) return false;

                aimDir = skill.AimDir;
                return true;
            }

            return false;
        }

        public static bool TryGetBuffId(object args, out int buffId)
        {
            buffId = 0;

            if (args is IBuffTriggerContext buff)
            {
                buffId = buff.BuffId;
                return buffId > 0;
            }

            return false;
        }

        public static bool TryGetBuffStackCount(object args, out int stackCount)
        {
            stackCount = 0;

            if (args is IBuffTriggerContext buff)
            {
                stackCount = buff.StackCount;
                return true;
            }

            return false;
        }

        public static bool TryGetBuffStage(object args, out string stage)
        {
            stage = null;

            if (args is IBuffTriggerContext buff)
            {
                stage = buff.Stage;
                return !string.IsNullOrEmpty(stage);
            }

            return false;
        }

        public static bool TryGetBuffDurationSeconds(object args, out float durationSeconds)
        {
            durationSeconds = 0f;

            if (args is IBuffTriggerContext buff)
            {
                durationSeconds = buff.DurationSeconds;
                return true;
            }

            return false;
        }

        public static bool TryGetBuffRemoveReason(object args, out AbilityKit.Trace.TraceLifecycleReason removeReason)
        {
            removeReason = AbilityKit.Trace.TraceLifecycleReason.None;

            if (args is IBuffTriggerContext buff)
            {
                removeReason = buff.RemoveReason;
                return true;
            }

            return false;
        }
    }
}
