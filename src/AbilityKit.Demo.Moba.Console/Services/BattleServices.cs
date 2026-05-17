using System;
using System.Collections.Generic;
using AbilityKit.Demo.Moba.Console.Platform;

namespace AbilityKit.Demo.Moba.Console.Services
{
    /// <summary>
    /// 角色信息（用于表现层显示）
    /// 注意：这是表现层的数据副本，逻辑层的数据由 Core 项目管理
    /// </summary>
    public sealed class ActorInfo
    {
        public int ActorId { get; set; }
        public string Name { get; set; } = "";
        public int CharacterId { get; set; }
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
        public float Hp { get; set; }
        public float HpMax { get; set; }
        public float PhysicsAttack { get; set; }
        public float MagicAttack { get; set; }
        public float PhysicsDefense { get; set; }
        public float MagicDefense { get; set; }
        public float MoveSpeed { get; set; }
        public float CriticalR { get; set; }
        public int TeamId { get; set; }

        public ActorInfo() { }

        public ActorInfo(int actorId, string name)
        {
            ActorId = actorId;
            Name = name;
        }
    }

    /// <summary>
    /// 实体显示数据管理（表现层）
    ///
    /// 职责边界：
    /// - ✅ 实体数据存储（用于显示）
    /// - ✅ 实体查询
    /// - ✅ 位置更新
    /// - ❌ 伤害计算（逻辑层）
    /// - ❌ 技能执行（逻辑层）
    /// - ❌ 死亡判定（逻辑层）
    /// </summary>
    public sealed class BattleServices
    {
        private readonly Dictionary<int, ActorInfo> _actors = new();
        private readonly Dictionary<(int, int), int> _positionIndex = new();

        public BattleServices()
        {
            Log.Trace("[TRACE] BattleServices created");
        }

        /// <summary>
        /// 注册角色
        /// </summary>
        public void RegisterActor(ActorInfo actor)
        {
            if (actor == null) return;
            _actors[actor.ActorId] = actor;
            UpdatePositionIndex(actor);
            Log.Entity($"[BattleServices] Registered actor: #{actor.ActorId} {actor.Name}");
        }

        /// <summary>
        /// 移除角色
        /// </summary>
        public void UnregisterActor(int actorId)
        {
            if (_actors.TryGetValue(actorId, out var actor))
            {
                RemovePositionIndex(actor);
                _actors.Remove(actorId);
                Log.Entity($"[BattleServices] Unregistered actor: #{actorId}");
            }
        }

        /// <summary>
        /// 更新角色位置
        /// </summary>
        public void UpdateActorPosition(int actorId, float x, float y, float z)
        {
            if (!_actors.TryGetValue(actorId, out var actor)) return;
            RemovePositionIndex(actor);
            actor.X = x;
            actor.Y = y;
            actor.Z = z;
            UpdatePositionIndex(actor);
        }

        /// <summary>
        /// 更新角色属性（用于视图显示）
        /// </summary>
        public void UpdateActorStats(int actorId, float? hp = null, float? hpMax = null)
        {
            if (!_actors.TryGetValue(actorId, out var actor)) return;
            if (hp.HasValue) actor.Hp = hp.Value;
            if (hpMax.HasValue) actor.HpMax = hpMax.Value;
        }

        /// <summary>
        /// 获取角色信息
        /// </summary>
        public ActorInfo? GetActor(int actorId)
        {
            return _actors.TryGetValue(actorId, out var actor) ? actor : null;
        }

        /// <summary>
        /// 查找指定位置的角色
        /// </summary>
        public int FindActorAtPosition(float x, float z, float range = 1.5f)
        {
            foreach (var actor in _actors.Values)
            {
                var dx = actor.X - x;
                var dz = actor.Z - z;
                var dist = (float)Math.Sqrt(dx * dx + dz * dz);
                if (dist <= range)
                {
                    return actor.ActorId;
                }
            }
            return 0;
        }

        /// <summary>
        /// 获取所有角色数量
        /// </summary>
        public int ActorCount => _actors.Count;

        /// <summary>
        /// 获取所有角色
        /// </summary>
        public IEnumerable<ActorInfo> GetAllActors() => _actors.Values;

        private void UpdatePositionIndex(ActorInfo actor)
        {
            var key = ((int)Math.Round(actor.X), (int)Math.Round(actor.Z));
            _positionIndex[key] = actor.ActorId;
        }

        private void RemovePositionIndex(ActorInfo actor)
        {
            var key = ((int)Math.Round(actor.X), (int)Math.Round(actor.Z));
            _positionIndex.Remove(key);
        }
    }

    /// <summary>
    /// 战斗视图服务接口（用于兼容）
    /// </summary>
    public interface BattleViewServices
    {
        void ShowDamage(int actorId, float hp, float hpMax);
        void ShowSkillEffect(int actorId, int skillId);
        void ShowDeath(int actorId, int killerActorId);
    }
}
