using System.Collections.Generic;

namespace AbilityKit.Samples.Logic.Samples.Demo.ProgressiveSkill.Phase0.Skills
{
    /// <summary>
    /// 技能定义
    /// </summary>
    public class SkillDefinition
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        
        // 资源消耗
        public float ManaCost { get; set; }
        
        // 时间参数
        public float CastTime { get; set; }      // 施法时间
        public float Cooldown { get; set; }       // 冷却时间
        public float Range { get; set; }          // 施法距离
        
        // 伤害参数
        public float BaseDamage { get; set; }
        
        // 效果列表
        public List<SkillEffect> Effects { get; set; } = new();
        
        /// <summary>
        /// 创建火球术
        /// </summary>
        public static SkillDefinition CreateFireball()
        {
            return new SkillDefinition
            {
                Id = 1001,
                Name = "火球术",
                Description = "发射一个火球，对目标造成火焰伤害，并有几率点燃目标",
                ManaCost = 30f,
                CastTime = 1.5f,
                Cooldown = 5f,
                Range = 15f,
                BaseDamage = 80f,
                Effects = new List<SkillEffect>
                {
                    new SkillEffect
                    {
                        Type = EffectType.Damage,
                        DamageType = "Fire",
                        Value = 0,
                        Chance = 1.0f
                    },
                    new SkillEffect
                    {
                        Type = EffectType.Debuff,
                        BuffId = "Burning",
                        Chance = 0.3f
                    }
                }
            };
        }
        
        /// <summary>
        /// 创建治疗术
        /// </summary>
        public static SkillDefinition CreateHeal()
        {
            return new SkillDefinition
            {
                Id = 1002,
                Name = "治疗术",
                Description = "恢复目标的生命值",
                ManaCost = 40f,
                CastTime = 2.0f,
                Cooldown = 8f,
                Range = 10f,
                BaseDamage = 0f,
                Effects = new List<SkillEffect>
                {
                    new SkillEffect
                    {
                        Type = EffectType.Heal,
                        Value = 150f
                    }
                }
            };
        }
    }
    
    /// <summary>
    /// 技能效果
    /// </summary>
    public class SkillEffect
    {
        public EffectType Type { get; set; }
        public string DamageType { get; set; }   // 伤害类型：Fire, Ice, Lightning, Physical
        public float Value { get; set; }        // 效果值
        public float Chance { get; set; } = 1.0f;  // 触发几率
        public string BuffId { get; set; }     // Buff ID
    }
    
    /// <summary>
    /// 效果类型
    /// </summary>
    public enum EffectType
    {
        Damage,
        Heal,
        Buff,
        Debuff,
        Shield,
        CrowdControl
    }
}
