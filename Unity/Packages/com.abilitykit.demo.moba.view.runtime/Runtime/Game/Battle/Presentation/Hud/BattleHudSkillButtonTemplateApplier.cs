using AbilityKit.Core.Logging;
using AbilityKit.Demo.Moba.Config.BattleDemo.MO;
using AbilityKit.Game.Battle.View.Lib.Skill;

namespace AbilityKit.Game.Flow
{
    internal sealed class BattleHudSkillButtonTemplateApplier
    {
        public void Apply(SkillButtonView view, SkillButtonTemplateMO template, BattleHudSkillPresentationSpec spec)
        {
            if (view == null || template == null) return;

            var cfg = view.Config;
            cfg.LongPressSeconds = template.LongPressSeconds > 0f ? template.LongPressSeconds : cfg.LongPressSeconds;
            cfg.DragThreshold = template.DragThreshold > 0f ? template.DragThreshold : cfg.DragThreshold;
            cfg.EnableAim = template.EnableAim || spec.IndicatorShape != SkillAimIndicatorShape.Hidden;
            cfg.AimMaxRadius = template.AimMaxRadius > 0f ? template.AimMaxRadius : cfg.AimMaxRadius;

            cfg.AimMode = template.AimMode == (int)SkillAimMode.Point ? SkillAimMode.Point : SkillAimMode.Direction;
            cfg.UsePointMode = template.UsePointMode == (int)SkillUsePointMode.Aim
                ? SkillUsePointMode.Aim
                : template.UsePointMode == (int)SkillUsePointMode.TargetPoint
                    ? SkillUsePointMode.TargetPoint
                    : SkillUsePointMode.None;
            cfg.SelectRange = template.SelectRange;
            cfg.FaceToAim = template.FaceToAim;
            cfg.IndicatorShape = spec.IndicatorShape;
            cfg.IndicatorLengthPixels = spec.UiLengthPixels;
            cfg.IndicatorWidthPixels = spec.UiWidthPixels;

            view.Config = cfg;
            Log.Info($"[BattleHudSkillButtonTemplateApplier] applied skillId={spec.SkillId}, templateId={template.Id}, enableAim={cfg.EnableAim}, aimMode={cfg.AimMode}, usePointMode={cfg.UsePointMode}, dragThreshold={cfg.DragThreshold:F1}");
        }
    }
}
