using UnityEngine;

namespace AbilityKit.Game.Flow
{
    internal static class BattleHudInputLayout
    {
        public static readonly BattleHudSkillButtonLayoutSpec Skill1 =
            new BattleHudSkillButtonLayoutSpec(1, "Skill1", new Vector2(-260f, 200f));

        public static readonly BattleHudSkillButtonLayoutSpec Skill2 =
            new BattleHudSkillButtonLayoutSpec(2, "Skill2", new Vector2(-140f, 110f));

        public static readonly BattleHudSkillButtonLayoutSpec Skill3 =
            new BattleHudSkillButtonLayoutSpec(3, "Skill3", new Vector2(-120f, 260f));

        public static readonly Vector2 InfoButtonPosition = new Vector2(-80f, -80f);
    }
}
