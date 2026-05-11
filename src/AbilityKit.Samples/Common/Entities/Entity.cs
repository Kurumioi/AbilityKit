using System;
using System.Collections.Generic;

namespace AbilityKit.Samples.Common.Entities
{
    /// <summary>
    /// 实体接口
    /// </summary>
    public interface IEntity
    {
        int Id { get; }
        string Name { get; }
        bool IsAlive { get; }
        float Health { get; }
        float MaxHealth { get; }
    }

    /// <summary>
    /// 简单实体实现
    /// </summary>
    public sealed class Entity : IEntity
    {
        private static int _nextId;

        public int Id { get; }
        public string Name { get; set; }
        public float Health { get; set; }
        public float MaxHealth { get; set; }
        public float AttackPower { get; set; }
        public float Defense { get; set; }
        public float Position { get; set; }

        public bool IsAlive => Health > 0;

        public Entity(string name, float maxHealth = 100f, float attackPower = 10f, float defense = 0f)
        {
            Id = ++_nextId;
            Name = name;
            MaxHealth = maxHealth;
            Health = maxHealth;
            AttackPower = attackPower;
            Defense = defense;
        }

        public void TakeDamage(float damage)
        {
            var actualDamage = System.Math.Max(0, damage - Defense);
            Health = System.Math.Max(0, Health - actualDamage);
        }

        public void Heal(float amount)
        {
            Health = System.Math.Min(MaxHealth, Health + amount);
        }

        public override string ToString() => $"{Name}(HP={Health:F0}/{MaxHealth:F0})";
    }

    /// <summary>
    /// 实体管理器
    /// </summary>
    public sealed class EntityManager
    {
        private readonly Dictionary<int, Entity> _entities = new();

        public Entity Create(string name, float maxHealth = 100f, float attackPower = 10f)
        {
            var entity = new Entity(name, maxHealth, attackPower);
            _entities[entity.Id] = entity;
            return entity;
        }

        public Entity? Get(int id)
        {
            return _entities.TryGetValue(id, out var entity) ? entity : null;
        }

        public bool Destroy(int id)
        {
            return _entities.Remove(id);
        }

        public IReadOnlyCollection<Entity> GetAll()
        {
            return _entities.Values;
        }

        public int Count => _entities.Count;
    }
}
