using System;

namespace AbilityKit.Samples.Logic.Samples.Demo.ProgressiveSkill.Phase0.Entities
{
    /// <summary>
    /// 技能实体 - 施放技能的角色
    /// </summary>
    public sealed class SkillEntity
    {
        public long Id { get; }
        public string Name { get; }
        public float Health { get; set; }
        public float MaxHealth { get; set; }
        public float Mana { get; set; }
        public float MaxMana { get; set; }
        public float AttackPower { get; set; }
        public float Defense { get; set; }
        public float MoveSpeed { get; set; }
        
        public SkillEntity(long id, string name, float maxHealth = 1000f, float maxMana = 500f)
        {
            Id = id;
            Name = name;
            MaxHealth = maxHealth;
            Health = maxHealth;
            MaxMana = maxMana;
            Mana = maxMana;
            AttackPower = 50f;
            Defense = 20f;
            MoveSpeed = 5f;
        }

        public bool IsAlive => Health > 0;
        
        public bool HasMana(float cost) => Mana >= cost;
        
        public void ConsumeMana(float amount)
        {
            Mana = Math.Max(0, Mana - amount);
        }

        public void RestoreMana(float amount)
        {
            Mana = Math.Min(MaxMana, Mana + amount);
        }

        public void TakeDamage(float damage)
        {
            var actualDamage = Math.Max(1, damage - Defense * 0.5f);
            Health = Math.Max(0, Health - actualDamage);
        }

        public void Heal(float amount)
        {
            Health = Math.Min(MaxHealth, Health + amount);
        }
        
        public override string ToString() => $"{Name}(HP={Health:F0}/{MaxHealth}, MP={Mana:F0}/{MaxMana})";
    }
}
