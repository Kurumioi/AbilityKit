using System;
using System.Collections.Generic;
using AbilityKit.Demo.Moba.Console.Platform;

namespace AbilityKit.Demo.Moba.Console.Services
{
    /// <summary>
    /// 战斗服务接口
    /// 提供战斗系统需要的基础服务
    /// </summary>
    public interface IBattleServices
    {
        /// <summary>
        /// 获取角色信息
        /// </summary>
        ActorInfo? GetActor(int actorId);

        /// <summary>
        /// 查找指定位置的角色
        /// </summary>
        int FindActorAtPosition(float x, float z);

        /// <summary>
        /// 应用伤害
        /// </summary>
        void ApplyDamage(int targetActorId, float damage, int sourceActorId, int skillId);

        /// <summary>
        /// 技能施放事件
        /// </summary>
        void OnSkillCast(int casterActorId, int skillId, int slot);

        /// <summary>
        /// 移动输入
        /// </summary>
        void OnMoveInput(int actorId, float dx, float dz);
    }

    /// <summary>
    /// 角色信息
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
        public int AttributeTemplateId { get; set; }
        public float PhysicsAttack { get; set; }
        public float MagicAttack { get; set; }
        public float PhysicsDefense { get; set; }
        public float MagicDefense { get; set; }
        public float MoveSpeed { get; set; }
        public int TeamId { get; set; }

        public ActorInfo() { }

        public ActorInfo(int actorId, string name)
        {
            ActorId = actorId;
            Name = name;
        }
    }

    /// <summary>
    /// 战斗服务实现
    /// </summary>
    public sealed class BattleServices : IBattleServices
    {
        private readonly BattleViewServices _viewServices;
        private readonly Dictionary<int, ActorInfo> _actors = new();
        private readonly Dictionary<(int, int), int> _positionIndex = new();

        public BattleServices(BattleViewServices viewServices)
        {
            _viewServices = viewServices ?? throw new ArgumentNullException(nameof(viewServices));
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
            Log.Trace($"[TRACE] BattleServices.RegisterActor - Actor#{actor.ActorId} ({actor.Name}), HP:{actor.Hp:F0}/{actor.HpMax:F0}, ATK:{actor.PhysicsAttack}, DEF:{actor.PhysicsDefense}");
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
                Log.Trace($"[TRACE] BattleServices.UnregisterActor - Actor#{actorId}");
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
        /// 更新角色属性
        /// </summary>
        public void UpdateActorStats(int actorId, float? hp = null, float? hpMax = null)
        {
            if (!_actors.TryGetValue(actorId, out var actor)) return;

            if (hp.HasValue) actor.Hp = hp.Value;
            if (hpMax.HasValue) actor.HpMax = hpMax.Value;

            if (hp.HasValue)
            {
                _viewServices?.ShowDamage(actorId, actor.Hp, actor.HpMax);
            }
        }

        /// <summary>
        /// 获取角色信息
        /// </summary>
        public ActorInfo? GetActor(int actorId)
        {
            var actor = _actors.TryGetValue(actorId, out var a) ? a : null;
            Log.Trace($"[TRACE] BattleServices.GetActor({actorId}) -> {(actor != null ? $"{actor.Name}" : "null")}");
            return actor;
        }

        /// <summary>
        /// 查找指定位置的角色
        /// </summary>
        public int FindActorAtPosition(float x, float z)
        {
            return FindActorAtPosition(x, z, 1.5f);
        }

        /// <summary>
        /// 查找指定位置的角色（带范围）
        /// </summary>
        public int FindActorAtPosition(float x, float z, float range)
        {
            Log.Trace($"[TRACE] BattleServices.FindActorAtPosition({x:F1}, {z:F1}, range={range:F1})");
            foreach (var actor in _actors.Values)
            {
                var dx = actor.X - x;
                var dz = actor.Z - z;
                var dist = (float)Math.Sqrt(dx * dx + dz * dz);
                if (dist <= range)
                {
                    Log.Trace($"[TRACE] FindActorAtPosition - Found #{actor.ActorId} ({actor.Name}) at distance {dist:F2}");
                    return actor.ActorId;
                }
            }
            Log.Trace("[TRACE] FindActorAtPosition - No actor found");
            return 0;
        }

        /// <summary>
        /// 应用伤害
        /// </summary>
        public void ApplyDamage(int targetActorId, float damage, int sourceActorId, int skillId)
        {
            Log.Trace($"[TRACE] BattleServices.ApplyDamage - Target:{targetActorId}, Damage:{damage:F1}, Source:{sourceActorId}, Skill:{skillId}");
            if (!_actors.TryGetValue(targetActorId, out var target)) return;

            var actualDamage = Math.Max(1f, damage); // 至少造成1点伤害
            var oldHp = target.Hp;
            target.Hp = Math.Max(0f, target.Hp - actualDamage);

            Log.Damage($"[Damage] #{sourceActorId} dealt {actualDamage:F1} damage to #{targetActorId} (Skill:{skillId}). HP: {target.Hp:F0}/{target.HpMax:F0}");
            Log.Trace($"[TRACE] ApplyDamage - Actor#{targetActorId} HP: {oldHp:F0} -> {target.Hp:F0}");

            // 通知视图层
            _viewServices?.ShowDamage(targetActorId, target.Hp, target.HpMax);

            // 检查死亡
            if (target.Hp <= 0)
            {
                Log.Trace($"[TRACE] ApplyDamage - Actor#{targetActorId} died");
                OnActorDied(targetActorId, sourceActorId);
            }
        }

        /// <summary>
        /// 技能施放事件
        /// </summary>
        public void OnSkillCast(int casterActorId, int skillId, int slot)
        {
            Log.Trace($"[TRACE] BattleServices.OnSkillCast - Caster:{casterActorId}, Skill:{skillId}, Slot:{slot}");
            if (!_actors.TryGetValue(casterActorId, out var caster)) return;

            Log.Skill($"[Skill] Actor #{casterActorId} casted skill {skillId} (slot {slot})");
            _viewServices?.ShowSkillEffect(casterActorId, skillId);
        }

        /// <summary>
        /// 移动输入
        /// </summary>
        public void OnMoveInput(int actorId, float dx, float dz)
        {
            if (!_actors.TryGetValue(actorId, out var actor)) return;

            // 简单移动处理
            const float moveSpeed = 5f;
            actor.X += dx * moveSpeed * 0.033f; // 假设 30 FPS
            actor.Z += dz * moveSpeed * 0.033f;

            UpdatePositionIndex(actor);
            Log.Sync($"[Move] Actor #{actorId} -> ({actor.X:F1}, {actor.Z:F1})");
        }

        /// <summary>
        /// 角色死亡
        /// </summary>
        private void OnActorDied(int actorId, int killerActorId)
        {
            if (!_actors.TryGetValue(actorId, out var actor)) return;

            Log.Battle($"[Battle] Actor #{actorId} ({actor.Name}) was killed by #{killerActorId}");

            // 通知视图层
            _viewServices?.ShowDeath(actorId, killerActorId);

            // 移除角色
            UnregisterActor(actorId);
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

        /// <summary>
        /// 获取所有角色数量
        /// </summary>
        public int ActorCount => _actors.Count;
    }

    /// <summary>
    /// 战斗视图服务接口
    /// </summary>
    public interface BattleViewServices
    {
        void ShowDamage(int actorId, float hp, float hpMax);
        void ShowSkillEffect(int actorId, int skillId);
        void ShowDeath(int actorId, int killerActorId);
    }
}
