using System;
using AbilityKit.Core.Math;

namespace AbilityKit.Demo.Moba.Console.Bootstrap
{
    /// <summary>
    /// 技能执行结果
    /// 由表现层传递给逻辑层，逻辑层负责计算伤害
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
        /// 失败原因
        /// </summary>
        public string FailReason { get; }

        private SkillCastResult(bool success, int casterId, int slot, int skillId,
            int targetId, string failReason)
        {
            Success = success;
            CasterId = casterId;
            Slot = slot;
            SkillId = skillId;
            TargetId = targetId;
            FailReason = failReason;
        }

        /// <summary>
        /// 创建成功结果
        /// </summary>
        public static SkillCastResult CreateSuccess(int casterId, int slot, int skillId,
            int targetId)
        {
            return new SkillCastResult(true, casterId, slot, skillId, targetId, null);
        }

        /// <summary>
        /// 创建失败结果
        /// </summary>
        public static SkillCastResult CreateFailure(int casterId, int slot, int skillId,
            string failReason)
        {
            return new SkillCastResult(false, casterId, slot, skillId, 0, failReason);
        }
    }
}
