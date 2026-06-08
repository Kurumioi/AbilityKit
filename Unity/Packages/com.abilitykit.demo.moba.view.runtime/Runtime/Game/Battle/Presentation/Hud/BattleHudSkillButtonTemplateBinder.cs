using AbilityKit.Game.Battle.View.Lib.Skill;
using AbilityKit.Protocol.Moba;

namespace AbilityKit.Game.Flow
{
    internal static class BattleHudSkillButtonTemplateBinder
    {
        public static void TryApply(
            EnterMobaGameRes res,
            string playerId,
            SkillButtonView skill1View,
            SkillButtonView skill2View,
            SkillButtonView skill3View)
        {
            if (skill1View == null && skill2View == null && skill3View == null) return;
            if (!BattleHudSkillButtonTemplateResolver.TryFindLoadout(res, playerId, out var loadout)) return;

            ApplySkillButtonTemplate(1, skill1View, loadout);
            ApplySkillButtonTemplate(2, skill2View, loadout);
            ApplySkillButtonTemplate(3, skill3View, loadout);
        }

        private static void ApplySkillButtonTemplate(int slot, SkillButtonView view, in MobaPlayerLoadout loadout)
        {
            if (view == null) return;
            if (!BattleHudSkillButtonTemplateResolver.TryResolveTemplate(loadout, slot, out var template)) return;

            BattleHudSkillButtonTemplateApplier.Apply(view, template);
        }
    }
}
