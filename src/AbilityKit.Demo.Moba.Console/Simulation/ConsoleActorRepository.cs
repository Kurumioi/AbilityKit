using System;
using System.Collections.Generic;

namespace AbilityKit.Demo.Moba.Console.Simulation
{
    /// <summary>
    /// Simulation 层的角色数据存储（权威数据源）
    ///
    /// 职责边界：
    /// - ✅ 角色数据存储（用于逻辑计算）
    /// - ✅ 角色查询
    /// - ✅ 位置更新
    /// - ✅ HP 更新
    /// - ✅ 目标查找（通过位置和队伍判断）
    /// - ❌ 不做渲染
    /// - ❌ 不持有 UI 引用
    ///
    /// 数据来源：
    /// - 由 ConsoleLogicHub 初始化和管理
    /// - 伤害计算后由 ConsoleLogicHub 更新
    /// - 移动处理后由 ConsoleLogicHub 更新
    /// </summary>
    public sealed class ConsoleActorRepository
    {
        private readonly Dictionary<int, ActorState> _actors = new();
        private readonly Dictionary<(int, int), int> _positionIndex = new();

        /// <summary>
        /// 注册角色
        /// </summary>
        public void RegisterActor(ActorState actor)
        {
            if (actor == null) return;
            _actors[actor.ActorId] = actor;
            UpdatePositionIndex(actor);
            Platform.Log.Entity($"[SimActorRepo] Registered actor: #{actor.ActorId} {actor.Name}");
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
                Platform.Log.Entity($"[SimActorRepo] Unregistered actor: #{actorId}");
            }
        }

        /// <summary>
        /// 尝试获取角色
        /// </summary>
        public bool TryGetActor(int actorId, out ActorState actor)
        {
            return _actors.TryGetValue(actorId, out actor);
        }

        /// <summary>
        /// 获取角色（不存在返回 null）
        /// </summary>
        public ActorState? GetActor(int actorId)
        {
            return _actors.TryGetValue(actorId, out var actor) ? actor : null;
        }

        /// <summary>
        /// 更新角色位置
        /// </summary>
        public void UpdatePosition(int actorId, float x, float y, float z)
        {
            if (!_actors.TryGetValue(actorId, out var actor)) return;
            RemovePositionIndex(actor);
            actor.X = x;
            actor.Y = y;
            actor.Z = z;
            UpdatePositionIndex(actor);
        }

        /// <summary>
        /// 更新角色 HP
        /// </summary>
        public void UpdateHp(int actorId, float hp, float maxHp)
        {
            if (!_actors.TryGetValue(actorId, out var actor)) return;
            actor.Hp = hp;
            actor.HpMax = maxHp;
        }

        /// <summary>
        /// 获取角色数量
        /// </summary>
        public int ActorCount => _actors.Count;

        /// <summary>
        /// 获取所有角色
        /// </summary>
        public IEnumerable<ActorState> GetAllActors()
        {
            return _actors.Values;
        }

        /// <summary>
        /// 查找范围内最近的敌方单位
        /// </summary>
        public int FindNearestEnemy(int casterId, float range)
        {
            var caster = GetActor(casterId);
            if (caster == null) return 0;

            var casterTeamId = caster.TeamId;
            int nearestEnemy = 0;
            float nearestDist = float.MaxValue;

            foreach (var actor in _actors.Values)
            {
                // 跳过自己
                if (actor.ActorId == casterId) continue;

                // 只找活着的单位
                if (actor.Hp <= 0) continue;

                // 只找敌方单位
                if (actor.TeamId == casterTeamId) continue;

                // 计算距离
                var dx = actor.X - caster.X;
                var dz = actor.Z - caster.Z;
                var dist = (float)Math.Sqrt(dx * dx + dz * dz);

                // 查找最近的
                if (dist < nearestDist && dist <= range)
                {
                    nearestDist = dist;
                    nearestEnemy = actor.ActorId;
                }
            }

            return nearestEnemy;
        }

        /// <summary>
        /// 清空所有角色
        /// </summary>
        public void Clear()
        {
            _actors.Clear();
            _positionIndex.Clear();
        }

        private void UpdatePositionIndex(ActorState actor)
        {
            var key = ((int)Math.Round(actor.X), (int)Math.Round(actor.Z));
            _positionIndex[key] = actor.ActorId;
        }

        private void RemovePositionIndex(ActorState actor)
        {
            var key = ((int)Math.Round(actor.X), (int)Math.Round(actor.Z));
            _positionIndex.Remove(key);
        }
    }

    /// <summary>
    /// Simulation 层角色状态
    /// </summary>
    public sealed class ActorState
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

        public ActorState() { }

        public ActorState(int actorId, string name)
        {
            ActorId = actorId;
            Name = name;
        }
    }
}
