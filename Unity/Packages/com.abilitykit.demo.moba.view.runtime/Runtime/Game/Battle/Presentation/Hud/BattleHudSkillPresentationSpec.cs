using AbilityKit.Demo.Moba.Config.BattleDemo.MO;
using AbilityKit.Game.Battle.View.Lib.Skill;
using UnityEngine;

namespace AbilityKit.Game.Flow
{
    internal enum BattleHudSkillPreviewShape
    {
        None = 0,
        DirectionLine = 1,
        TargetCircle = 2,
        SelfCircle = 3,
        Sector = 4,
        DashLine = 5,
        LockProjectile = 6,
        FanArea = 7,
    }

    internal readonly struct BattleHudSkillPresentationSpec
    {
        public readonly int SkillId;
        public readonly string Name;
        public readonly BattleHudSkillPreviewShape PreviewShape;
        public readonly SkillAimIndicatorShape IndicatorShape;
        public readonly float Range;
        public readonly float Width;
        public readonly float Radius;
        public readonly Color Color;

        public BattleHudSkillPresentationSpec(
            int skillId,
            string name,
            BattleHudSkillPreviewShape previewShape,
            SkillAimIndicatorShape indicatorShape,
            float range,
            float width,
            float radius,
            Color color)
        {
            SkillId = skillId;
            Name = name;
            PreviewShape = previewShape;
            IndicatorShape = indicatorShape;
            Range = range;
            Width = width;
            Radius = radius;
            Color = color;
        }

        public float UiLengthPixels => Mathf.Max(80f, Range * 22f);
        public float UiWidthPixels => Mathf.Max(28f, Width * 24f);

        public static BattleHudSkillPresentationSpec Hidden(int skillId, string name)
        {
            return new BattleHudSkillPresentationSpec(
                skillId,
                name,
                BattleHudSkillPreviewShape.None,
                SkillAimIndicatorShape.Hidden,
                0f,
                0f,
                0f,
                new Color(0.2f, 0.75f, 1f, 0.25f));
        }
    }

    internal sealed class BattleHudSkillPresentationSpecResolver
    {
        private static readonly Color s_directionColor = new Color(0.2f, 0.75f, 1f, 0.3f);
        private static readonly Color s_targetColor = new Color(0.2f, 0.75f, 1f, 0.28f);

        public BattleHudSkillPresentationSpec Resolve(SkillMO skill, SkillButtonTemplateMO template)
        {
            if (skill == null)
            {
                return BattleHudSkillPresentationSpec.Hidden(0, string.Empty);
            }

            if (template == null || !template.EnableAim)
            {
                return BattleHudSkillPresentationSpec.Hidden(skill.Id, skill.Name);
            }

            var range = Mathf.Max(1f, skill.Range);
            if (template.AimMode == (int)SkillAimMode.Point)
            {
                return new BattleHudSkillPresentationSpec(
                    skill.Id,
                    skill.Name,
                    BattleHudSkillPreviewShape.TargetCircle,
                    SkillAimIndicatorShape.TargetCircle,
                    range,
                    5.6f,
                    2.8f,
                    s_targetColor);
            }

            return new BattleHudSkillPresentationSpec(
                skill.Id,
                skill.Name,
                BattleHudSkillPreviewShape.DirectionLine,
                SkillAimIndicatorShape.DirectionLine,
                range,
                1.5f,
                0f,
                s_directionColor);
        }
    }
}
