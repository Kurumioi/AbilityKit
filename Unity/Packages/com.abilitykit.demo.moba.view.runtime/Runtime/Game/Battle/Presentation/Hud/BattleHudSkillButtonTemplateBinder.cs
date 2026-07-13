using System.Collections.Generic;
using AbilityKit.Core.Logging;
using AbilityKit.Game.Battle.View.Lib.Skill;
using AbilityKit.Protocol.Moba;

namespace AbilityKit.Game.Flow
{
    internal sealed class BattleHudSkillButtonTemplateBinder
    {
        private readonly BattleHudSkillButtonTemplateResolver _resolver;
        private readonly BattleHudSkillButtonTemplateApplier _applier;
        private readonly Dictionary<int, BattleHudSkillPresentationSpec> _skillSpecs = new Dictionary<int, BattleHudSkillPresentationSpec>();

        public IReadOnlyDictionary<int, BattleHudSkillPresentationSpec> SkillSpecs => _skillSpecs;

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

        public int ResolveSkillButtonCount(EnterMobaGameRes res, string playerId)
        {
            if (!_resolver.TryFindLoadout(res, playerId, out var loadout)) return 0;
            return _resolver.ResolveSkillButtonCount(loadout);
        }

        public void TryApply(
            EnterMobaGameRes res,
            string playerId,
            IReadOnlyList<SkillButtonView> skillViews)
        {
            if (skillViews == null || skillViews.Count == 0)
            {
                Log.Warning("[BattleHudSkillButtonTemplateBinder] skip apply: no skill views.");
                return;
            }

            if (!_resolver.TryFindLoadout(res, playerId, out var loadout))
            {
                Log.Warning($"[BattleHudSkillButtonTemplateBinder] skip apply: loadout not found. playerId={playerId}, responsePlayerId={res.PlayerId.Value}, viewCount={skillViews.Count}");
                return;
            }

            _skillSpecs.Clear();
            Log.Info($"[BattleHudSkillButtonTemplateBinder] apply loadout. playerId={loadout.PlayerId.Value}, basicAttackSkillId={loadout.BasicAttackSkillId}, skillCount={(loadout.SkillIds != null ? loadout.SkillIds.Length : 0)}, viewCount={skillViews.Count}");

            for (var i = 0; i < skillViews.Count; i++)
            {
                ApplySkillButtonTemplate(i + 1, skillViews[i], loadout);
            }
        }

        public void ClearSpecs()
        {
            _skillSpecs.Clear();
        }

        private void ApplySkillButtonTemplate(int slot, SkillButtonView view, in MobaPlayerLoadout loadout)
        {
            if (view == null)
            {
                Log.Warning($"[BattleHudSkillButtonTemplateBinder] skip slot={slot}: view is null.");
                return;
            }

            if (!_resolver.TryResolveSkill(loadout, slot, out var skill, out var template, out var spec))
            {
                Log.Warning($"[BattleHudSkillButtonTemplateBinder] skip slot={slot}: skill/template resolve failed. playerId={loadout.PlayerId.Value}, basicAttackSkillId={loadout.BasicAttackSkillId}, skillCount={(loadout.SkillIds != null ? loadout.SkillIds.Length : 0)}");
                return;
            }

            _skillSpecs[slot] = spec;
            Log.Info($"[BattleHudSkillButtonTemplateBinder] slot={slot} skillId={(skill != null ? skill.Id : spec.SkillId)}, templateId={(template != null ? template.Id : 0)}, enableAim={(template != null ? template.EnableAim : spec.IndicatorShape != SkillAimIndicatorShape.Hidden)}");
            _applier.Apply(view, template, spec);
        }
    }
}
