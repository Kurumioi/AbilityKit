using System;

namespace AbilityKit.Samples.Logic.Samples.Demo.ProgressiveSkill.Phase0.Entities
{
    /// <summary>
    /// 目标实体 - 技能的目标
    /// </summary>
    public sealed class TargetEntity
    {
        public long Id { get; }
        public string Name { get; }
        public float Health { get; set; }
        public float MaxHealth { get; set; }
        public float Defense { get; set; }
        public float PositionX { get; set; }
        public float PositionY { get; set; }
        public float PositionZ { get; set; }
        
        public TargetEntity(long id, string name, float maxHealth = 500f)
        {
            Id = id;
            Name = name;
            MaxHealth = maxHealth;
            Health = maxHealth;
            Defense = 15f;
            PositionX = 0;
            PositionY = 0;
            PositionZ = 0;
        }

        public bool IsAlive => Health > 0;
        
        public void TakeDamage(float damage)
        {
            var actualDamage = Math.Max(1, damage - Defense * 0.5f);
            Health = Math.Max(0, Health - actualDamage);
            Console.WriteLine($"  [伤害] {Name} 受到 {actualDamage:F0} 点伤害 (HP: {Health:F0}/{MaxHealth})");
        }
        
        public float DistanceTo(TargetEntity other)
        {
            var dx = other.PositionX - PositionX;
            var dy = other.PositionY - PositionY;
            var dz = other.PositionZ - PositionZ;
            return (float)Math.Sqrt(dx * dx + dy * dy + dz * dz);
        }
        
        public override string ToString() => $"{Name}(HP={Health:F0}/{MaxHealth})";
    }
}
