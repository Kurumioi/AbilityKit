using AbilityKit.Modifiers;

namespace AbilityKit.Demo.Moba.Services
{
    /// <summary>
    /// 由修饰器驱动的 MOBA 技能/动作参数运行时键。
    /// </summary>
    public static class MobaSkillParamModifierKeys
    {
        public static class Skill
        {
            public static readonly ModifierKey SkillId = ModifierKey.Create(ModifierKey.Categories.Skill, 1);
        }

        public static class Projectile
        {
            public static readonly ModifierKey LauncherId = ModifierKey.Create(ModifierKey.Categories.Projectile, 1);
            public static readonly ModifierKey ProjectileId = ModifierKey.Create(ModifierKey.Categories.Projectile, 2);
            public static readonly ModifierKey CountPerShot = ModifierKey.Create(ModifierKey.Categories.Projectile, 3);
            public static readonly ModifierKey FanAngleDeg = ModifierKey.Create(ModifierKey.Categories.Projectile, 4);
            public static readonly ModifierKey DurationMs = ModifierKey.Create(ModifierKey.Categories.Projectile, 5);
        }

        public static class Summon
        {
            public static readonly ModifierKey SummonId = ModifierKey.Create(ModifierKey.Categories.Skill, 2);
        }
    }
}
