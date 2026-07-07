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
        public BattleHudSkillPresentationSpec Resolve(SkillMO skill, SkillButtonTemplateMO template)
        {
            if (skill == null)
            {
                return BattleHudSkillPresentationSpec.Hidden(0, string.Empty);
            }

            if (TryResolveKnownSkill(skill, out var spec))
            {
                return spec;
            }

            return ResolveFromTemplate(skill, template);
        }

        private static bool TryResolveKnownSkill(SkillMO skill, out BattleHudSkillPresentationSpec spec)
        {
            var range = Mathf.Max(1f, skill.Range);
            switch (skill.Id)
            {
                case 10010101: // 廉颇-爆裂冲撞：直线突进击飞
                    spec = Direction(skill, range, 1.8f, new Color(0.95f, 0.55f, 0.18f, 0.34f));
                    return true;
                case 10010201: // 廉颇-熔岩重击：自身范围蓄力
                    spec = Self(skill, 4.2f, new Color(0.95f, 0.34f, 0.18f, 0.32f));
                    return true;
                case 10010301: // 廉颇-天崩地裂：指定区域三段砸击
                    spec = Target(skill, range, 3.4f, new Color(0.95f, 0.68f, 0.18f, 0.3f));
                    return true;
                case 10020101: // 小乔-绽放之舞：扇子直线飞行
                    spec = Direction(skill, range, 1.2f, new Color(0.35f, 0.82f, 1f, 0.32f));
                    return true;
                case 10020201: // 小乔-甜蜜恋风：指定区域击飞
                    spec = Target(skill, range, 2.8f, new Color(0.65f, 0.92f, 1f, 0.3f));
                    return true;
                case 10020301: // 小乔-星华缭乱：自身周围自动打击
                    spec = Self(skill, 7.5f, new Color(0.48f, 0.7f, 1f, 0.24f));
                    return true;
                case 10030101: // 赵云-惊雷之龙：短突进强化普攻
                    spec = Direction(skill, 5.5f, 1.5f, new Color(0.52f, 0.76f, 1f, 0.32f));
                    return true;
                case 10030201: // 赵云-破云之龙：连续枪刺
                    spec = Sector(skill, range, 2.3f, new Color(0.42f, 0.88f, 1f, 0.3f));
                    return true;
                case 10030301: // 赵云-天翔之龙：指定区域突进击飞
                    spec = Target(skill, Mathf.Min(range, 12f), 3.2f, new Color(0.72f, 0.85f, 1f, 0.32f));
                    return true;
                case 10040101: // 墨子-和平漫步：位移后炮击
                    spec = Direction(skill, 5.5f, 1.6f, new Color(0.55f, 0.95f, 0.8f, 0.3f));
                    return true;
                case 10040201: // 墨子-机关重炮：远程直线炮
                    spec = Direction(skill, range, 1.4f, new Color(0.18f, 0.95f, 0.72f, 0.34f));
                    return true;
                case 10040301: // 墨子-墨守成规：自身持续控制力场
                    spec = Self(skill, 4.8f, new Color(0.18f, 0.9f, 0.66f, 0.28f));
                    return true;
                default:
                    spec = default;
                    return false;
            }
        }

        private static BattleHudSkillPresentationSpec ResolveFromTemplate(SkillMO skill, SkillButtonTemplateMO template)
        {
            if (template == null || !template.EnableAim)
            {
                return BattleHudSkillPresentationSpec.Hidden(skill.Id, skill.Name);
            }

            var range = Mathf.Max(1f, skill.Range);
            if (template.AimMode == (int)SkillAimMode.Point)
            {
                return Target(skill, range, 2.8f, new Color(0.2f, 0.75f, 1f, 0.28f));
            }

            return Direction(skill, range, 1.5f, new Color(0.2f, 0.75f, 1f, 0.3f));
        }

        private static BattleHudSkillPresentationSpec Direction(SkillMO skill, float range, float width, Color color)
        {
            return new BattleHudSkillPresentationSpec(
                skill.Id,
                skill.Name,
                BattleHudSkillPreviewShape.DirectionLine,
                SkillAimIndicatorShape.DirectionLine,
                range,
                width,
                0f,
                color);
        }

        private static BattleHudSkillPresentationSpec Target(SkillMO skill, float range, float radius, Color color)
        {
            return new BattleHudSkillPresentationSpec(
                skill.Id,
                skill.Name,
                BattleHudSkillPreviewShape.TargetCircle,
                SkillAimIndicatorShape.TargetCircle,
                range,
                radius * 2f,
                radius,
                color);
        }

        private static BattleHudSkillPresentationSpec Self(SkillMO skill, float radius, Color color)
        {
            return new BattleHudSkillPresentationSpec(
                skill.Id,
                skill.Name,
                BattleHudSkillPreviewShape.SelfCircle,
                SkillAimIndicatorShape.SelfCircle,
                radius,
                radius * 2f,
                radius,
                color);
        }

        private static BattleHudSkillPresentationSpec Sector(SkillMO skill, float range, float width, Color color)
        {
            return new BattleHudSkillPresentationSpec(
                skill.Id,
                skill.Name,
                BattleHudSkillPreviewShape.Sector,
                SkillAimIndicatorShape.Sector,
                range,
                width,
                0f,
                color);
        }
    }
}
