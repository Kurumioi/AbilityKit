using System;
using System.Collections.Generic;
using AbilityKit.Demo.Moba.Console.Platform;

namespace AbilityKit.Demo.Moba.Console.Services
{
    /// <summary>
    /// 角色信息（用于表现层显示）
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
        public int SkillLevel { get; set; } = 1;

        public ActorInfo() { }

        public ActorInfo(int actorId, string name)
        {
            ActorId = actorId;
            Name = name;
        }
    }

    /// <summary>
    /// 表现层角色数据仓库（纯表现层）
    ///
    /// 职责边界：
    /// - ✅ 角色数据存储（用于视图显示）
    /// - ✅ 角色查询
    /// - ✅ 位置更新
    /// - ✅ HP 更新（从事件中获取，由逻辑层计算）
    /// - ❌ 伤害计算（逻辑层）
    /// - ❌ 技能执行（逻辑层）
    /// - ❌ 死亡判定（逻辑层）
    /// - ❌ 目标查找（逻辑层）
    ///
    /// 数据来源：
    /// - 由逻辑层发布的事件（DamageEvent, MoveInputProcessedEvent 等）驱动更新
    /// - 不直接修改游戏状态，只存储用于渲染的副本
    /// </summary>
    public sealed class ViewActorRepository
    {
        private readonly Dictionary<int, ActorInfo> _actors = new();
        private readonly Dictionary<(int, int), int> _positionIndex = new();

        public ViewActorRepository()
        {
            Log.Trace("[TRACE] ViewActorRepository created");
        }

        /// <summary>
        /// 注册角色（由初始化阶段调用）
        /// </summary>
        public void RegisterActor(ActorInfo actor)
        {
            if (actor == null) return;
            _actors[actor.ActorId] = actor;
            UpdatePositionIndex(actor);
            Log.Entity($"[ViewRepo] Registered actor: #{actor.ActorId} {actor.Name}");
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
                Log.Entity($"[ViewRepo] Unregistered actor: #{actorId}");
            }
        }

        /// <summary>
        /// 更新角色位置（由移动事件驱动）
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
        /// 更新角色 HP（由伤害/治疗事件驱动）
        /// 注意：HP 值由逻辑层计算后通过事件传入，这里只是存储
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

        /// <summary>
        /// 清空所有角色
        /// </summary>
        public void Clear()
        {
            _actors.Clear();
            _positionIndex.Clear();
        }

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
