using AbilityKit.Demo.Moba.Config.BattleDemo.MO;
using AbilityKit.Demo.Moba.Config.Core;

namespace AbilityKit.Game.Flow
{
    internal sealed class BattleHudSkillButtonTemplateConfigLookup
    {
        private readonly BattleViewResourceProvider _resources;

        public BattleHudSkillButtonTemplateConfigLookup(BattleViewResourceProvider resources = null)
        {
            _resources = BattleViewResourceProvider.OrDefault(resources);
        }

        public bool TryResolve(int skillId, out SkillButtonTemplateMO template)
        {
            template = null;
            if (skillId <= 0) return false;
            if (!TryGetConfigs(out var configs)) return false;
            if (!TryGetSkill(configs, skillId, out var skill)) return false;
            if (skill.SkillButtonTemplateId <= 0) return false;

            return TryGetTemplate(configs, skill.SkillButtonTemplateId, out template);
        }

        private bool TryGetConfigs(out MobaConfigDatabase configs)
        {
            configs = _resources.GetOrLoadConfigs();
            return configs != null;
        }

        private bool TryGetSkill(MobaConfigDatabase configs, int skillId, out SkillMO skill)
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

        private bool TryGetTemplate(
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
