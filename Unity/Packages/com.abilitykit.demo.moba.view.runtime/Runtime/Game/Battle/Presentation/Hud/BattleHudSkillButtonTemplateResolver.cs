using AbilityKit.Demo.Moba.Config.BattleDemo.MO;
using AbilityKit.Demo.Moba.Config.Core;
using AbilityKit.Game.Battle.Moba.Config;
using AbilityKit.Protocol.Moba;

namespace AbilityKit.Game.Flow
{
    internal static class BattleHudSkillButtonTemplateResolver
    {
        private static MobaConfigDatabase _configs;

        public static bool TryFindLoadout(
            EnterMobaGameRes res,
            string playerId,
            out MobaPlayerLoadout loadout)
        {
            loadout = default;
            if (string.IsNullOrEmpty(playerId)) return false;

            var loadouts = res.PlayersLoadout;
            if (loadouts == null || loadouts.Length == 0) return false;

            for (int i = 0; i < loadouts.Length; i++)
            {
                var candidate = loadouts[i];
                if (candidate.PlayerId.Value == playerId)
                {
                    loadout = candidate;
                    return true;
                }
            }

            return false;
        }

        public static bool TryResolveTemplate(
            in MobaPlayerLoadout loadout,
            int slot,
            out SkillButtonTemplateMO template)
        {
            template = null;
            if (slot <= 0) return false;

            var skills = loadout.SkillIds;
            if (skills == null || skills.Length < slot) return false;

            var skillId = skills[slot - 1];
            if (skillId <= 0) return false;
            if (!TryGetConfigs(out var configs)) return false;

            if (!TryGetSkill(configs, skillId, out var skill)) return false;
            if (skill.SkillButtonTemplateId <= 0) return false;

            return TryGetTemplate(configs, skill.SkillButtonTemplateId, out template);
        }

        private static bool TryGetConfigs(out MobaConfigDatabase configs)
        {
            _configs ??= MobaConfigLoader.LoadDefault();
            configs = _configs;
            return configs != null;
        }

        private static bool TryGetSkill(MobaConfigDatabase configs, int skillId, out SkillMO skill)
        {
            skill = null;
            try
            {
                skill = configs.GetSkill(skillId);
            }
            catch
            {
                return false;
            }

            return skill != null;
        }

        private static bool TryGetTemplate(
            MobaConfigDatabase configs,
            int templateId,
            out SkillButtonTemplateMO template)
        {
            template = null;
            try
            {
                template = configs.GetSkillButtonTemplate(templateId);
            }
            catch
            {
                return false;
            }

            return template != null;
        }
    }
}
