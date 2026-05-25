using System;
using ET;

namespace ET.AbilityKit.Demo.ET.Share
{
    /// <summary>
    /// 单位受伤事件
    /// </summary>
    public struct ActorDamageEvent : IEvent
    {
        public Type Type => typeof(ActorDamageEvent);

        /// <summary>
        /// 逻辑层（MobaCore）的 ActorId
        /// </summary>
        public int ActorId;

        /// <summary>
        /// 伤害来源的 ActorId
        /// </summary>
        public int SourceActorId;

        /// <summary>
        /// 伤害值
        /// </summary>
        public float Damage;

        /// <summary>
        /// 当前 HP
        /// </summary>
        public float CurrentHp;

        /// <summary>
        /// 最大 HP
        /// </summary>
        public float MaxHp;
    }
}
