using AbilityKit.Core.Logging;
using AbilityKit.Demo.Moba.Config.BattleDemo.MO;
using AbilityKit.Game.Battle.View.Lib.Skill;

namespace AbilityKit.Game.Flow
{
    internal sealed class BattleHudSkillButtonTemplateApplier
    {
        public void Apply(SkillButtonView view, SkillButtonTemplateMO template, BattleHudSkillPresentationSpec spec)
        {
            if (view == null) return;

            var cfg = view.Config;
            if (template != null)
            {
                cfg.LongPressSeconds = template.LongPressSeconds > 0f ? template.LongPressSeconds : cfg.LongPressSeconds;
                cfg.DragThreshold = template.DragThreshold > 0f ? template.DragThreshold : cfg.DragThreshold;
                cfg.AimMaxRadius = template.AimMaxRadius > 0f ? template.AimMaxRadius : cfg.AimMaxRadius;
                cfg.AimMode = template.AimMode == (int)SkillAimMode.Point ? SkillAimMode.Point : SkillAimMode.Direction;
                cfg.UsePointMode = template.UsePointMode == (int)SkillUsePointMode.Aim
                    ? SkillUsePointMode.Aim
                    : template.UsePointMode == (int)SkillUsePointMode.TargetPoint
                        ? SkillUsePointMode.TargetPoint
                        : SkillUsePointMode.None;
                cfg.SelectRange = template.SelectRange;
                cfg.FaceToAim = template.FaceToAim;
                cfg.EnableAim = template.EnableAim || spec.IndicatorShape != SkillAimIndicatorShape.Hidden;
            }
            else
            {
                cfg.LongPressSeconds = cfg.LongPressSeconds > 0f ? cfg.LongPressSeconds : 0.35f;
                cfg.DragThreshold = cfg.DragThreshold > 0f ? cfg.DragThreshold : 12f;
                cfg.AimMaxRadius = cfg.AimMaxRadius > 0f ? cfg.AimMaxRadius : 220f;
                cfg.AimMode = spec.IndicatorShape == SkillAimIndicatorShape.TargetCircle ? SkillAimMode.Point : SkillAimMode.Direction;
                cfg.UsePointMode = cfg.AimMode == SkillAimMode.Point ? SkillUsePointMode.TargetPoint : SkillUsePointMode.Aim;
                cfg.FaceToAim = cfg.AimMode == SkillAimMode.Direction;
                cfg.EnableAim = spec.IndicatorShape != SkillAimIndicatorShape.Hidden;
            }
            cfg.IndicatorShape = spec.IndicatorShape;
            cfg.IndicatorLengthPixels = spec.UiLengthPixels;
            cfg.IndicatorWidthPixels = spec.UiWidthPixels;

            view.Config = cfg;
            Log.Info($"[BattleHudSkillButtonTemplateApplier] applied skillId={spec.SkillId}, templateId={(template != null ? template.Id : 0)}, enableAim={cfg.EnableAim}, aimMode={cfg.AimMode}, usePointMode={cfg.UsePointMode}, dragThreshold={cfg.DragThreshold:F1}");
        }
    }
}
