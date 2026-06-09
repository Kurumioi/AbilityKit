using System.Collections.Generic;
using AbilityKit.Game.Battle.View.Lib.Skill;
using AbilityKit.Protocol.Moba;

namespace AbilityKit.Game.Flow
{
    internal sealed class BattleHudSkillButtonTemplateBinder
    {
        private readonly BattleHudSkillButtonTemplateResolver _resolver;
        private readonly BattleHudSkillButtonTemplateApplier _applier;

        public BattleHudSkillButtonTemplateBinder(BattleViewResourceProvider resources = null)
            : this(new BattleHudSkillButtonTemplateResolver(resources), null)
        {
        }

        internal BattleHudSkillButtonTemplateBinder(
            BattleHudSkillButtonTemplateResolver resolver,
            BattleHudSkillButtonTemplateApplier applier = null)
        {
            _resolver = resolver ?? new BattleHudSkillButtonTemplateResolver();
            _applier = applier ?? new BattleHudSkillButtonTemplateApplier();
        }

        public void TryApply(
            EnterMobaGameRes res,
            string playerId,
            IReadOnlyList<SkillButtonView> skillViews)
        {
            if (skillViews == null || skillViews.Count == 0) return;
            if (!_resolver.TryFindLoadout(res, playerId, out var loadout)) return;

            for (var i = 0; i < skillViews.Count; i++)
            {
                ApplySkillButtonTemplate(i + 1, skillViews[i], loadout);
            }
        }

        private void ApplySkillButtonTemplate(int slot, SkillButtonView view, in MobaPlayerLoadout loadout)
        {
            if (view == null) return;
            if (!_resolver.TryResolveTemplate(loadout, slot, out var template)) return;

            _applier.Apply(view, template);
        }
    }
}
