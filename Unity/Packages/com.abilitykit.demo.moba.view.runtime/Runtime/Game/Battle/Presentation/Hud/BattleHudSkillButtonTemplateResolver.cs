using AbilityKit.Demo.Moba.Config.BattleDemo.MO;
using AbilityKit.Protocol.Moba;

namespace AbilityKit.Game.Flow
{
    internal sealed class BattleHudSkillButtonTemplateResolver
    {
        private readonly BattleHudSkillButtonTemplateConfigLookup _templates;
        private readonly BattleHudPlayerLoadoutResolver _loadouts;

        public BattleHudSkillButtonTemplateResolver(BattleViewResourceProvider resources = null)
            : this(
                new BattleHudSkillButtonTemplateConfigLookup(resources),
                new BattleHudPlayerLoadoutResolver())
        {
        }

        internal BattleHudSkillButtonTemplateResolver(
            BattleHudSkillButtonTemplateConfigLookup templates,
            BattleHudPlayerLoadoutResolver loadouts)
        {
            _templates = templates ?? new BattleHudSkillButtonTemplateConfigLookup();
            _loadouts = loadouts ?? new BattleHudPlayerLoadoutResolver();
        }

        public bool TryFindLoadout(
            EnterMobaGameRes res,
            string playerId,
            out MobaPlayerLoadout loadout)
        {
            return _loadouts.TryFind(res, playerId, out loadout);
        }

        public bool TryResolveTemplate(
            in MobaPlayerLoadout loadout,
            int slot,
            out SkillButtonTemplateMO template)
        {
            template = null;
            if (slot <= 0) return false;

            var skills = loadout.SkillIds;
            if (skills == null || skills.Length < slot) return false;

            var skillId = skills[slot - 1];
            return _templates.TryResolve(skillId, out template);
        }
    }
}
