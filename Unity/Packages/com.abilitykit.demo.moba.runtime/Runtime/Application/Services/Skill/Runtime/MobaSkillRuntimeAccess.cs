using System;
using AbilityKit.Ability.FrameSync;
using AbilityKit.Demo.Moba.Components;

namespace AbilityKit.Demo.Moba.Services
{
    public static class MobaSkillRuntimeAccess
    {
        public static long GetCurrentTimeMs(IFrameTime time)
        {
            if (time == null) return 0L;
            return (long)MathF.Round(time.Time * 1000f);
        }

        public static bool TryGetActiveSkill(
            MobaActorLookupService actors,
            int actorId,
            int skillSlot,
            int skillId,
            out ActiveSkillRuntime runtime)
        {
            runtime = null;
            if (actors == null || actorId <= 0 || skillSlot <= 0 || skillId <= 0) return false;
            if (!actors.TryGetActorEntity(actorId, out var actor) || actor == null) return false;
            if (!actor.hasSkillLoadout || actor.skillLoadout.ActiveSkills == null) return false;

            var index = skillSlot - 1;
            if (index < 0 || index >= actor.skillLoadout.ActiveSkills.Length) return false;

            var candidate = actor.skillLoadout.ActiveSkills[index];
            if (candidate == null || candidate.SkillId != skillId) return false;

            runtime = candidate;
            return true;
        }

        public static bool TrySetActiveSkillCooldown(
            MobaActorLookupService actors,
            int actorId,
            int skillSlot,
            int skillId,
            long cooldownEndTimeMs)
        {
            if (!TryGetActiveSkill(actors, actorId, skillSlot, skillId, out var runtime)) return false;
            runtime.CooldownEndTimeMs = cooldownEndTimeMs;
            return true;
        }
    }
}
