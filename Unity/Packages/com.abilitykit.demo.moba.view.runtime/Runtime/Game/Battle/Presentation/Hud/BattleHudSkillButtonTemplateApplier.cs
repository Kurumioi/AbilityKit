using AbilityKit.Demo.Moba.Config.BattleDemo.MO;
using AbilityKit.Game.Battle.View.Lib.Skill;

namespace AbilityKit.Game.Flow
{
    internal static class BattleHudSkillButtonTemplateApplier
    {
        public static void Apply(SkillButtonView view, SkillButtonTemplateMO template)
        {
            if (view == null || template == null) return;

            var cfg = view.Config;
            cfg.LongPressSeconds = template.LongPressSeconds > 0f ? template.LongPressSeconds : cfg.LongPressSeconds;
            cfg.DragThreshold = template.DragThreshold > 0f ? template.DragThreshold : cfg.DragThreshold;
            cfg.EnableAim = template.EnableAim;
            cfg.AimMaxRadius = template.AimMaxRadius > 0f ? template.AimMaxRadius : cfg.AimMaxRadius;

            cfg.AimMode = template.AimMode == (int)SkillAimMode.Point ? SkillAimMode.Point : SkillAimMode.Direction;
            cfg.UsePointMode = template.UsePointMode == (int)SkillUsePointMode.Aim
                ? SkillUsePointMode.Aim
                : template.UsePointMode == (int)SkillUsePointMode.TargetPoint
                    ? SkillUsePointMode.TargetPoint
                    : SkillUsePointMode.None;
            cfg.SelectRange = template.SelectRange;
            cfg.FaceToAim = template.FaceToAim;

            view.Config = cfg;
        }
    }
}
