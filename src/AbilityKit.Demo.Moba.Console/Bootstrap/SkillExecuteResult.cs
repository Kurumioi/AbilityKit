using System;
using AbilityKit.Core.Math;

namespace AbilityKit.Demo.Moba.Console.Bootstrap
{
    /// <summary>
    /// 技能执行结果
    /// 由 Simulation 层传递给 View 层使用
    /// </summary>
    public readonly struct SkillCastResult
    {
        /// <summary>
        /// 是否成功
        /// </summary>
        public bool Success { get; }

        /// <summary>
        /// 释放者 ID
        /// </summary>
        public int CasterId { get; }

        /// <summary>
        /// 技能槽位
        /// </summary>
        public int Slot { get; }

        /// <summary>
        /// 技能 ID
        /// </summary>
        public int SkillId { get; }

        /// <summary>
        /// 目标 ID
        /// </summary>
        public int TargetId { get; }

        /// <summary>
        /// 基础伤害值
        /// </summary>
        public float BaseDamage { get; }

        /// <summary>
        /// 失败原因
        /// </summary>
        public string FailReason { get; }

        private SkillCastResult(bool success, int casterId, int slot, int skillId,
            int targetId, float baseDamage, string failReason)
        {
            Success = success;
            CasterId = casterId;
            Slot = slot;
            SkillId = skillId;
            TargetId = targetId;
            BaseDamage = baseDamage;
            FailReason = failReason;
        }

        /// <summary>
        /// 创建成功结果
        /// </summary>
        public static SkillCastResult CreateSuccess(int casterId, int slot, int skillId,
            int targetId, float baseDamage)
        {
            return new SkillCastResult(true, casterId, slot, skillId, targetId, baseDamage, null);
        }

        /// <summary>
        /// 创建失败结果
        /// </summary>
        public static SkillCastResult CreateFailure(int casterId, int slot, int skillId,
            string failReason)
        {
            return new SkillCastResult(false, casterId, slot, skillId, 0, 0f, failReason);
        }
    }

    /// <summary>
    /// 伤害执行结果
    /// 由 Simulation 层传递给 View 层使用
    /// </summary>
    public readonly struct DamageExecuteResult
    {
        /// <summary>
        /// 攻击者 ID
        /// </summary>
        public int AttackerId { get; }

        /// <summary>
        /// 目标 ID
        /// </summary>
        public int TargetId { get; }

        /// <summary>
        /// 技能 ID
        /// </summary>
        public int SkillId { get; }

        /// <summary>
        /// 伤害值
        /// </summary>
        public float Damage { get; }

        /// <summary>
        /// 目标当前 HP
        /// </summary>
        public float CurrentHp { get; }

        /// <summary>
        /// 目标最大 HP
        /// </summary>
        public float MaxHp { get; }

        /// <summary>
        /// 是否死亡
        /// </summary>
        public bool IsDead { get; }

        /// <summary>
        /// 是否暴击
        /// </summary>
        public bool IsCritical { get; }

        public DamageExecuteResult(int attackerId, int targetId, int skillId,
            float damage, float currentHp, float maxHp, bool isDead, bool isCritical)
        {
            AttackerId = attackerId;
            TargetId = targetId;
            SkillId = skillId;
            Damage = damage;
            CurrentHp = currentHp;
            MaxHp = maxHp;
            IsDead = isDead;
            IsCritical = isCritical;
        }
    }
}
