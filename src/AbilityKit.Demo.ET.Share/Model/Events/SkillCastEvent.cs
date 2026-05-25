using System;
using ET;

namespace ET.AbilityKit.Demo.ET.Share
{
    /// <summary>
    /// 技能释放事件
    /// </summary>
    public struct SkillCastEvent : IEvent
    {
        public Type Type => typeof(SkillCastEvent);

        /// <summary>
        /// 释放者 ActorId
        /// </summary>
        public int CasterActorId;

        /// <summary>
        /// 技能 ID
        /// </summary>
        public int SkillId;

        /// <summary>
        /// 目标位置 X
        /// </summary>
        public float TargetX;

        /// <summary>
        /// 目标位置 Y
        /// </summary>
        public float TargetY;
    }
}
