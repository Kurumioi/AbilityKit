using System;
using ET;

namespace ET.AbilityKit.Demo.ET.Share
{
    /// <summary>
    /// 技能命中事件
    /// </summary>
    public struct SkillHitEvent : IEvent
    {
        public Type Type => typeof(SkillHitEvent);

        /// <summary>
        /// 目标 ActorId
        /// </summary>
        public int TargetActorId;

        /// <summary>
        /// 释放者 ActorId
        /// </summary>
        public int CasterActorId;

        /// <summary>
        /// 技能 ID
        /// </summary>
        public int SkillId;

        /// <summary>
        /// 伤害值
        /// </summary>
        public float Damage;
    }
}
